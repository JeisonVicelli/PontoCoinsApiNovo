using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;

namespace ProjetoPontos.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class RelatoriosController : ControllerBase
{
    private readonly LojaDbContext _db;
    public RelatoriosController(LojaDbContext db) { _db = db; }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        if (_db.Clientes is null || _db.Brindes is null)
            return StatusCode(500, "Tabelas indisponíveis.");

        var clientes = await _db.Clientes.ToListAsync();
        var brindes  = await _db.Brindes.ToListAsync();

        // ── KPIs ──
        int     totalClientes         = clientes.Count;
        decimal totalCashbackPendente = clientes.Sum(c => c.CashbackAcumulado);

        var comCompras = clientes.Where(c => c.ValorTotalGasto > 0).ToList();
        decimal ticketMedio = comCompras.Count > 0
            ? Math.Round(comCompras.Average(c => c.ValorTotalGasto), 2)
            : 0m;

        // ── Top 10 clientes por valor total gasto ──
        var topClientes = clientes
            .OrderByDescending(c => c.ValorTotalGasto)
            .Take(10)
            .Select(c => new
            {
                c.Nome,
                c.Cpf,
                c.NumeroTelefone,
                TotalCompras      = c.ValorTotalGasto,
                Pontos            = c.Pontos ?? 0,
                CashbackAcumulado = c.CashbackAcumulado
            })
            .ToList();

        // ── Catálogo de brindes (ordenado por custo — histórico de resgates não rastreado) ──
        var catalogoBrindes = brindes
            .OrderBy(b => b.ValorPontos)
            .Select(b => new { b.Id, b.Nome, b.ValorPontos })
            .ToList();

        // ── Total de cashback já resgatado (original - pendente) ──
        // ValorTotalGasto * 5% = cashback gerado; diferença = resgatado
        decimal totalCashbackGerado   = clientes.Sum(c => Math.Round(c.ValorTotalGasto * 0.05m, 2));
        decimal totalCashbackResgatado = Math.Max(0m, totalCashbackGerado - totalCashbackPendente);

        // ── Clientes inativos (cadastro há mais de 60 dias sem compras) ──
        var corte = DateTime.UtcNow.AddDays(-60);
        var inativos = clientes
            .Where(c => c.ValorTotalGasto == 0 &&
                        c.DataCadastro.HasValue &&
                        c.DataCadastro.Value < corte)
            .OrderBy(c => c.DataCadastro)
            .Select(c => new
            {
                c.Nome,
                c.Cpf,
                c.NumeroTelefone,
                DiasInativo = (int)(DateTime.UtcNow - c.DataCadastro!.Value).TotalDays
            })
            .ToList();

        return Ok(new
        {
            totalClientes,
            totalCashbackPendente,
            ticketMedio,
            totalCashbackResgatado,
            topClientes,
            catalogoBrindes,
            clientesInativos = inativos
        });
    }
}
