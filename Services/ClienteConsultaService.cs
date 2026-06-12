using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;

namespace ProjetoPontos.Services
{
    // Lógica de consulta (saldo, extrato, expiração) compartilhada entre
    // ControlePonto (acesso por funcionário, via CPF) e ClienteController (acesso pelo próprio cliente).
    public class ClienteConsultaService
    {
        private const string FmtData = "dd/MM/yyyy";

        private readonly LojaDbContext _dbContext;
        private readonly CashbackService _cashbackSvc;

        public ClienteConsultaService(LojaDbContext dbContext, CashbackService cashbackSvc)
        {
            _dbContext   = dbContext;
            _cashbackSvc = cashbackSvc;
        }

        public async Task<object> ObterSaldoAsync(Cliente cliente)
        {
            await _cashbackSvc.SincronizarSaldoClienteAsync(cliente);
            await _dbContext.SaveChangesAsync();

            var nivel         = ConfiguracoesFidelidade.ObterNivel(cliente.ValorTotalGasto);
            var multiplicador = ConfiguracoesFidelidade.ObterMultiplicador(cliente.ValorTotalGasto);
            var pontosEmDobro = ConfiguracoesFidelidade.EhDiaDePontosEmDobro();

            return new
            {
                cpf               = cliente.Cpf,
                nome              = cliente.Nome,
                numeroTelefone    = cliente.NumeroTelefone,
                pontos            = cliente.Pontos ?? 0,
                cashbackAcumulado = cliente.CashbackAcumulado,
                valorTotalGasto   = cliente.ValorTotalGasto,
                dataNascimento    = cliente.DataNascimento,
                dataCadastro      = cliente.DataCadastro,
                nivel,
                multiplicador,
                pontosEmDobro
            };
        }

        public async Task<object> ObterExtratoAsync(string cpf, int top = 5)
        {
            if (_dbContext.Historico is null) return Array.Empty<object>();

            try
            {
                var linhas = await _dbContext.Historico
                    .Where(h => h.CpfCliente == cpf)
                    .OrderByDescending(h => h.Data)
                    .Take(top)
                    .Select(h => new { h.Data, h.Operacao, h.Valor, h.PontosMovimentados })
                    .ToListAsync();

                return linhas;
            }
            catch
            {
                // tabela ainda não criada — rode a migration
                return Array.Empty<object>();
            }
        }

        public async Task<object> ObterExpiracaoAsync(Cliente cliente, int diasAlerta = 15)
        {
            DateTime dataRef = await ObterDataUltimaMovimentacaoAsync(cliente.Cpf!)
                               ?? cliente.DataCadastro
                               ?? DateTime.UtcNow;

            DateTime dataExpiracao = dataRef.AddDays(ConfiguracoesFidelidade.ValidadeCashbackDias);
            int diasRestantes      = (int)(dataExpiracao.Date - DateTime.UtcNow.Date).TotalDays;
            bool expiraEmBreve     = diasRestantes >= 0 && diasRestantes <= diasAlerta;

            return new
            {
                cpf                  = cliente.Cpf,
                cashbackAcumulado    = cliente.CashbackAcumulado,
                pontosAcumulados     = cliente.Pontos ?? 0,
                dataExpiracao        = dataExpiracao.ToString(FmtData),
                diasRestantes,
                expiraEmBreve,
                alerta = expiraEmBreve && cliente.CashbackAcumulado > 0
                    ? $"Atenção: R$ {cliente.CashbackAcumulado:F2} de cashback expiram em {diasRestantes} dia(s)!"
                    : null
            };
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
}
