using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;

namespace Controles
{
    [Route("[controller]")]
    [ApiController]
    public class ControlePonto : ControllerBase
    {
        private readonly LojaDbContext _dbContext;

        public ControlePonto(LojaDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("buscar/{cpf}")]
        public async Task<ActionResult<Cliente>> Buscar(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return BadRequest("CPF inválido.");

            var clienteTemp = await _dbContext.Clientes.FindAsync(cpf);

            return clienteTemp != null ? Ok(clienteTemp) : NotFound("Cliente não encontrado.");
        }

        [HttpPost("converterParaPontos")]
        public async Task<IActionResult> ConverterParaPontos([FromBody] ConversaoPontoModel conversao)
        {
            if (conversao == null || string.IsNullOrWhiteSpace(conversao.CpfCliente) || conversao.ValorEmReais <= 0)
                return BadRequest("Dados inválidos para a conversão de pontos.");

            var cliente = await _dbContext.Clientes.FindAsync(conversao.CpfCliente);

            if (cliente is null)
                return NotFound("Cliente não encontrado.");

            decimal valorDaCompra = conversao.ValorEmReais; // Valor da compra recebido do Swagger
            cliente.AtualizarValorTotalGasto(valorDaCompra); // Atualiza o valor total gasto pelo cliente 

            int pontosConvertidos = PontosHelper.ConverteValorParaPontos(conversao.ValorEmReais);
            cliente.Pontos += pontosConvertidos;

            await _dbContext.SaveChangesAsync();

            return Ok($"Pontos convertidos com sucesso. Novo saldo de pontos: {cliente.Pontos}");
        }

        [HttpPost("realizarTroca")]
        public async Task<IActionResult> RealizarTroca([FromBody] TrocaPontoModel troca)
        {
            if (troca == null || string.IsNullOrWhiteSpace(troca.CpfCliente) || troca.IdBrinde <= 0)
                return BadRequest("Dados inválidos para a troca de pontos.");

            var cliente = await _dbContext.Clientes.FindAsync(troca.CpfCliente);
            var brinde = await _dbContext.Brindes.FindAsync(troca.IdBrinde);

            if (cliente is null || brinde is null)
                return BadRequest("Cliente ou Brinde não encontrado.");

            int pontosCliente = cliente.Pontos ?? 0;
            int pontosBrinde = brinde.ValorPontos ?? 0;

            if (pontosCliente >= pontosBrinde)
            {
                cliente.Pontos = pontosCliente - pontosBrinde;
                await _dbContext.SaveChangesAsync();
                return Ok("Troca Realizada com Sucesso!");
            }
            else
            {
                return BadRequest("Pontos insuficientes para trocar pelo brinde selecionado.");
            }
        }
    }

    public static class PontosHelper
    {
        public static int ConverteValorParaPontos(decimal valorEmReais)
        {
            const decimal valorPor100Reais = 100.00m;
            const int pontosPor100Reais = 8;

            int pontos = (int)(valorEmReais / valorPor100Reais) * pontosPor100Reais;
            return pontos;
        }
    }

    public class TrocaPontoModel
    {
        public string CpfCliente { get; set; }
        public int IdBrinde { get; set; }
    }

    public class ConversaoPontoModel
    {
        public string CpfCliente { get; set; }
        public decimal ValorEmReais { get; set; }
    }
}
