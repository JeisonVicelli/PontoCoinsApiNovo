using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;
using ProjetoPontos.Services;

namespace Controles
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class ControlePonto : ControllerBase
    {
        private const string ClienteNaoEncontrado = "Cliente não encontrado.";
        private const string FmtData = "dd/MM/yyyy";
        private readonly LojaDbContext _dbContext;
        private readonly CashbackService _cashbackSvc;
        private readonly WhatsAppService _whatsapp;

        public ControlePonto(LojaDbContext dbContext, CashbackService cashbackSvc, WhatsAppService whatsapp)
        {
            _dbContext   = dbContext;
            _cashbackSvc = cashbackSvc;
            _whatsapp    = whatsapp;
        }

        // ─────────────────────────────────────────────
        //  GET buscar/{cpf}
        // ─────────────────────────────────────────────
        [HttpGet("buscar/{cpf}")]
        public async Task<ActionResult<Cliente>> Buscar(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return BadRequest("CPF inválido.");

            var cliente = await _dbContext.Clientes.FindAsync(cpf);
            if (cliente is null) return NotFound(ClienteNaoEncontrado);

            await _cashbackSvc.SincronizarSaldoClienteAsync(cliente);
            await _dbContext.SaveChangesAsync();

            var nivel         = ConfiguracoesFidelidade.ObterNivel(cliente.ValorTotalGasto);
            var multiplicador = ConfiguracoesFidelidade.ObterMultiplicador(cliente.ValorTotalGasto);
            var pontosEmDobro = ConfiguracoesFidelidade.EhDiaDePontosEmDobro();

            return Ok(new {
                cpf                = cliente.Cpf,
                nome               = cliente.Nome,
                numeroTelefone     = cliente.NumeroTelefone,
                pontos             = cliente.Pontos ?? 0,
                cashbackAcumulado  = cliente.CashbackAcumulado,
                valorTotalGasto    = cliente.ValorTotalGasto,
                dataNascimento     = cliente.DataNascimento,
                dataCadastro       = cliente.DataCadastro,
                nivel,
                multiplicador,
                pontosEmDobro
            });
        }

        // ─────────────────────────────────────────────
        //  POST converterParaPontos
        // ─────────────────────────────────────────────
        [HttpPost("converterParaPontos")]
        public async Task<IActionResult> ConverterParaPontos([FromBody] ConversaoPontoModel conversao)
        {
            if (conversao == null || string.IsNullOrWhiteSpace(conversao.CpfCliente) || conversao.ValorEmReais <= 0)
                return BadRequest("Dados inválidos para a conversão de pontos.");

            var cliente = await _dbContext.Clientes.FindAsync(conversao.CpfCliente);
            if (cliente is null) return NotFound(ClienteNaoEncontrado);

            cliente.AtualizarValorTotalGasto(conversao.ValorEmReais);
            cliente.DataUltimaMovimentacao = DateTime.UtcNow;

            bool    ambos         = conversao.AcumularPontos && conversao.GerarCashback;
            string  nivel         = ConfiguracoesFidelidade.ObterNivel(cliente.ValorTotalGasto);
            decimal multiplicador = ConfiguracoesFidelidade.ObterMultiplicador(cliente.ValorTotalGasto);
            if (ConfiguracoesFidelidade.EhDiaDePontosEmDobro()) multiplicador *= 2;

            if (conversao.AcumularPontos)
            {
                if (cliente.Pontos == null) cliente.Pontos = 0;
                int fator        = ambos ? 1 : 2; // só pontos = 2× por R$12,50
                int pontosGanhos = (int)((conversao.ValorEmReais / 12.5m) * fator * multiplicador);
                cliente.Pontos   += pontosGanhos;
            }

            decimal cashbackValor = 0;
            if (conversao.GerarCashback)
            {
                decimal taxa  = ambos ? 0.025m : 0.05m;
                cashbackValor = Math.Round(conversao.ValorEmReais * taxa * multiplicador, 2);
                cliente.CreditarCashback(cashbackValor);
            }

            await _dbContext.SaveChangesAsync();

            // grava lote com o valor real calculado (2,5% ou 5% × multiplicador)
            if (conversao.GerarCashback && cashbackValor > 0)
                await _cashbackSvc.CriarLoteAsync(conversao.CpfCliente, cashbackValor);

            // grava histórico
            await RegistrarHistoricoAsync(conversao.CpfCliente, "Compra",
                conversao.ValorEmReais, conversao.AcumularPontos ? (cliente.Pontos ?? 0) : 0);

            var expiracao = DateTime.UtcNow.AddDays(ConfiguracoesFidelidade.ValidadeCashbackDias).ToString(FmtData);

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(cliente.NumeroTelefone))
                    {
                        await _whatsapp.EnviarAtualizacaoPontosAsync(
                            cliente.Nome ?? "Cliente",
                            cliente.NumeroTelefone,
                            cliente.Pontos ?? 0,
                            cliente.CashbackAcumulado,
                            nivel);
                    }
                }
                catch { /* fire-and-forget — falha silenciosa não bloqueia a resposta */ }
            });

            return Ok($"Venda registrada | Nível: {nivel} (x{multiplicador}) | Pontos: {cliente.Pontos} | Cashback: R$ {cliente.CashbackAcumulado:F2} | Cashback expira: {expiracao}");
        }

        // ─────────────────────────────────────────────
        //  POST resgatarPontos
        // ─────────────────────────────────────────────
        [HttpPost("resgatarPontos")]
        public async Task<IActionResult> ResgatarPontos([FromBody] ResgatePontosModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.CpfCliente) || model.Pontos <= 0)
                return BadRequest("Dados inválidos.");

            var cliente = await _dbContext.Clientes.FindAsync(model.CpfCliente);
            if (cliente is null) return NotFound(ClienteNaoEncontrado);

            int saldo = cliente.Pontos ?? 0;
            if (model.Pontos > saldo)
                return BadRequest($"Pontos insuficientes. Saldo: {saldo} pts");

            cliente.Pontos = saldo - model.Pontos;
            cliente.DataUltimaMovimentacao = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await RegistrarHistoricoAsync(model.CpfCliente, "Resgate Pontos", 0, -model.Pontos);

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(cliente.NumeroTelefone))
                    {
                        await _whatsapp.EnviarResgatePontosAsync(
                            cliente.Nome ?? "Cliente",
                            cliente.NumeroTelefone,
                            model.Pontos,
                            cliente.Pontos ?? 0);
                    }
                }
                catch { /* fire-and-forget — falha silenciosa não bloqueia a resposta */ }
            });

            return Ok($"Resgate de {model.Pontos} pts. Saldo restante: {cliente.Pontos} pts");
        }

        // ─────────────────────────────────────────────
        //  POST aplicarCashback
        // ─────────────────────────────────────────────
        [HttpPost("aplicarCashback")]
        public async Task<IActionResult> AplicarCashback([FromBody] AplicarCashbackModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.CpfCliente) || model.ValorCashback <= 0)
                return BadRequest("Dados inválidos.");

            var cliente = await _dbContext.Clientes.FindAsync(model.CpfCliente);
            if (cliente is null) return NotFound(ClienteNaoEncontrado);

            decimal saldo     = cliente.CashbackAcumulado;
            decimal solicitado = Math.Round(model.ValorCashback, 2);

            if (solicitado > saldo)
                return BadRequest($"Cashback insuficiente. Disponível: R$ {saldo:F2}");

            cliente.CashbackAcumulado = Math.Round(saldo - solicitado, 2);
            cliente.DataUltimaMovimentacao = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            // consome lotes FIFO (expira primeiro → usa primeiro)
            await _cashbackSvc.AplicarCashbackAsync(model.CpfCliente, solicitado);

            await RegistrarHistoricoAsync(model.CpfCliente, "Uso Cashback", -solicitado, 0);

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(cliente.NumeroTelefone))
                    {
                        var saldoRestante = cliente.CashbackAcumulado;
                        var msg = $"✅ *Gaia Skate & Surf*\n\n" +
                                  $"Oi {cliente.Nome ?? "Cliente"}! Cashback usado:\n\n" +
                                  $"💰 Desconto: *R$ {solicitado:F2}*\n" +
                                  $"💰 Saldo restante: *R$ {saldoRestante:F2}*\n\n" +
                                  $"Valeu pelo rolê! 🛹";
                        await _whatsapp.EnviarMensagemAsync(cliente.NumeroTelefone, msg);
                    }
                }
                catch { /* fire-and-forget — falha silenciosa não bloqueia a resposta */ }
            });

            return Ok($"Desconto de R$ {solicitado:F2} aplicado. Cashback restante: R$ {cliente.CashbackAcumulado:F2}");
        }

        // ─────────────────────────────────────────────
        //  POST realizarTroca
        // ─────────────────────────────────────────────
        [HttpPost("realizarTroca")]
        public async Task<IActionResult> RealizarTroca([FromBody] TrocaPontoModel troca)
        {
            if (troca == null || string.IsNullOrWhiteSpace(troca.CpfCliente) || troca.IdBrinde <= 0)
                return BadRequest("Dados inválidos para a troca de pontos.");

            var cliente = await _dbContext.Clientes.FindAsync(troca.CpfCliente);
            var brinde  = await _dbContext.Brindes.FindAsync(troca.IdBrinde);

            if (cliente is null || brinde is null)
                return BadRequest("Cliente ou Brinde não encontrado.");

            int pontosCliente = cliente.Pontos ?? 0;
            int pontosBrinde  = brinde.ValorPontos ?? 0;

            if (pontosCliente < pontosBrinde)
                return BadRequest("Pontos insuficientes para trocar pelo brinde selecionado.");

            cliente.Pontos = pontosCliente - pontosBrinde;
            await _dbContext.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(cliente.NumeroTelefone))
                    {
                        await _whatsapp.EnviarResgatePontosAsync(
                            cliente.Nome ?? "Cliente",
                            cliente.NumeroTelefone,
                            pontosBrinde,
                            cliente.Pontos ?? 0);
                    }
                }
                catch { /* fire-and-forget — falha silenciosa não bloqueia a resposta */ }
            });

            return Ok("Troca Realizada com Sucesso!");
        }

        // ─────────────────────────────────────────────
        //  GET expirando/{cpf}  ← NOVO
        //  Retorna quanto cashback expira nos próximos 15 dias
        // ─────────────────────────────────────────────
        [HttpGet("expirando/{cpf}")]
        public async Task<IActionResult> Expirando(string cpf, [FromQuery] int diasAlerta = 15)
        {
            var cliente = await _dbContext.Clientes.FindAsync(cpf);
            if (cliente is null) return NotFound(ClienteNaoEncontrado);

            // Data de referência: última entrada no histórico, ou DataCadastro como fallback
            DateTime dataRef = await ObterDataUltimaMovimentacaoAsync(cpf)
                               ?? cliente.DataCadastro
                               ?? DateTime.UtcNow;

            DateTime dataExpiracao = dataRef.AddDays(ConfiguracoesFidelidade.ValidadeCashbackDias);
            int diasRestantes      = (int)(dataExpiracao.Date - DateTime.UtcNow.Date).TotalDays;
            bool expiraEmBreve     = diasRestantes >= 0 && diasRestantes <= diasAlerta;

            return Ok(new
            {
                cpf,
                cashbackAcumulado    = cliente.CashbackAcumulado,
                pontosAcumulados     = cliente.Pontos ?? 0,
                dataExpiracao        = dataExpiracao.ToString(FmtData),
                diasRestantes,
                expiraEmBreve,
                alerta = expiraEmBreve && cliente.CashbackAcumulado > 0
                    ? $"Atenção: R$ {cliente.CashbackAcumulado:F2} de cashback expiram em {diasRestantes} dia(s)!"
                    : null
            });
        }

        // ─────────────────────────────────────────────
        //  GET extrato/{cpf}  ← NOVO
        //  Últimas 5 movimentações do histórico
        // ─────────────────────────────────────────────
        [HttpGet("extrato/{cpf}")]
        public async Task<IActionResult> Extrato(string cpf, [FromQuery] int top = 5)
        {
            if (_dbContext.Historico is null)
                return Ok(new object[] { });

            try
            {
                var linhas = await _dbContext.Historico
                    .Where(h => h.CpfCliente == cpf)
                    .OrderByDescending(h => h.Data)
                    .Take(top)
                    .Select(h => new { h.Data, h.Operacao, h.Valor, h.PontosMovimentados })
                    .ToListAsync();

                return Ok(linhas);
            }
            catch
            {
                // tabela ainda não criada — rode a migration
                return Ok(new object[] { });
            }
        }

        // ─────────────────────────────────────────────
        //  GET reativacao
        //  Clientes inativos há +60 dias com saldo > 0
        // ─────────────────────────────────────────────
        [HttpGet("reativacao")]
        public async Task<IActionResult> Reativacao([FromQuery] int diasCorte = 60)
        {
            if (_dbContext.Clientes is null) return StatusCode(500);

            var corte    = DateTime.UtcNow.AddDays(-diasCorte);
            var clientes = await _dbContext.Clientes.ToListAsync();

            // lookup de TotalGastoHistorico por CPF (fallback para ValorTotalGasto se tabela não existir)
            var totalPorCpf = await ObterTotalGastoHistoricoAsync();

            var lista = clientes
                .Where(c => (c.Pontos ?? 0) > 0 || c.CashbackAcumulado > 0)
                .Select(c =>
                {
                    var dataRef = c.DataUltimaMovimentacao ?? c.DataCadastro;
                    return new { c, dataRef };
                })
                .Where(x => x.dataRef.HasValue && x.dataRef.Value < corte)
                .OrderByDescending(x => (DateTime.UtcNow - x.dataRef!.Value).TotalDays)
                .Select(x => new
                {
                    x.c.Nome,
                    x.c.Cpf,
                    x.c.NumeroTelefone,
                    SaldoPontos          = x.c.Pontos ?? 0,
                    SaldoCashback        = x.c.CashbackAcumulado,
                    DiasInativo          = (int)(DateTime.UtcNow - x.dataRef!.Value).TotalDays,
                    UltimaAtividade      = x.dataRef.Value.ToString(FmtData),
                    TotalGastoHistorico  = totalPorCpf.TryGetValue(x.c.Cpf ?? "", out var t) ? t : x.c.ValorTotalGasto
                })
                .ToList();

            return Ok(lista);
        }

        // ─────────────────────────────────────────────
        //  GET aniversariantes-hoje
        // ─────────────────────────────────────────────
        [HttpGet("aniversariantes-hoje")]
        public async Task<IActionResult> AniversariantesHoje()
        {
            if (_dbContext.Clientes is null) return StatusCode(500);

            var hoje     = DateTime.UtcNow;
            var clientes = await _dbContext.Clientes.ToListAsync();

            var aniversariantes = clientes
                .Where(c => c.DataNascimento.HasValue &&
                            c.DataNascimento.Value.Month == hoje.Month &&
                            c.DataNascimento.Value.Day   == hoje.Day)
                .Select(c => new
                {
                    c.Nome,
                    c.Cpf,
                    c.NumeroTelefone,
                    DataNascimento = c.DataNascimento!.Value.ToString("dd/MM"),
                    Idade          = hoje.Year - c.DataNascimento!.Value.Year,
                    SaldoPontos    = c.Pontos ?? 0,
                    SaldoCashback  = c.CashbackAcumulado,
                    Nivel          = ProjetoPontos.Services.ConfiguracoesFidelidade.ObterNivel(c.ValorTotalGasto)
                })
                .OrderBy(c => c.Nome)
                .ToList();

            return Ok(new
            {
                data          = hoje.ToString(FmtData),
                total         = aniversariantes.Count,
                sugestao      = "Ativar pontos em dobro para os aniversariantes via DataPontosEmDobro.",
                aniversariantes
            });
        }

        // ─────────────────────────────────────────────
        //  Helpers privados
        // ─────────────────────────────────────────────
        // Retorna dicionário CPF → soma de "Compra" no histórico
        private async Task<Dictionary<string, decimal>> ObterTotalGastoHistoricoAsync()
        {
            if (_dbContext.Historico is null) return new Dictionary<string, decimal>();
            try
            {
                return await _dbContext.Historico
                    .Where(h => h.Operacao == "Compra")
                    .GroupBy(h => h.CpfCliente)
                    .Select(g => new { Cpf = g.Key, Total = g.Sum(h => h.Valor) })
                    .ToDictionaryAsync(x => x.Cpf, x => x.Total);
            }
            catch { return new Dictionary<string, decimal>(); }
        }

        private async Task RegistrarHistoricoAsync(string cpf, string operacao, decimal valor, int pontos)
        {
            if (_dbContext.Historico is null) return;
            try
            {
                await _dbContext.Historico.AddAsync(new HistoricoMovimentacao
                {
                    CpfCliente         = cpf,
                    Data               = DateTime.UtcNow,
                    Operacao           = operacao,
                    Valor              = valor,
                    PontosMovimentados = pontos
                });
                await _dbContext.SaveChangesAsync();
            }
            catch { /* tabela não criada ainda — silencioso */ }
        }

        private async Task<DateTime?> ObterDataUltimaMovimentacaoAsync(string cpf)
        {
            if (_dbContext.Historico is null) return null;
            try
            {
                return await _dbContext.Historico
                    .Where(h => h.CpfCliente == cpf)
                    .OrderByDescending(h => h.Data)
                    .Select(h => (DateTime?)h.Data)
                    .FirstOrDefaultAsync();
            }
            catch { return null; }
        }
    }

    // ─────────────────────────────────────────────
    //  DTOs e Helpers
    // ─────────────────────────────────────────────
    public static class PontosHelper
    {
        public static int ConverteValorParaPontos(decimal valorEmReais)
        {
            const decimal valorPor100Reais = 100.00m;
            const int pontosPor100Reais = 8;
            return (int)(valorEmReais / valorPor100Reais) * pontosPor100Reais;
        }

        public static decimal CalculaCashback(decimal valorEmReais)
        {
            const decimal porcento = 0.05m;
            return Math.Round(valorEmReais * porcento, 2);
        }
    }

    public class ResgatePontosModel
    {
        public string CpfCliente { get; set; } = string.Empty;
        public int Pontos { get; set; }
    }

    public class AplicarCashbackModel
    {
        public string CpfCliente { get; set; } = string.Empty;
        public decimal ValorCashback { get; set; }
    }

    public class TrocaPontoModel
    {
        public string CpfCliente { get; set; } = string.Empty;
        public int IdBrinde { get; set; }
    }

    public class ConversaoPontoModel
    {
        public string CpfCliente { get; set; } = string.Empty;
        public decimal ValorEmReais { get; set; }
        public bool AcumularPontos { get; set; } = true;
        public bool GerarCashback  { get; set; } = true;
    }
}
