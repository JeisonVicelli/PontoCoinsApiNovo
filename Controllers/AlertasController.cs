using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Services;

namespace ProjetoPontos.Controllers;

[ApiController]
[Route("[controller]")]
public class AlertasController : ControllerBase
{
    private readonly LojaDbContext    _db;
    private readonly WhatsAppService  _whatsapp;

    private const int DiasAlertaCashback = 15;   // alertar 15 dias antes de expirar (dia 45)
    private const int DiasAlertaPontos   = 15;   // alertar 15 dias antes de expirar (dia 350)
    private const int ValidadeCashback   = 60;
    private const int ValidadePontos     = 365;

    public AlertasController(LojaDbContext db, WhatsAppService whatsapp)
    {
        _db       = db;
        _whatsapp = whatsapp;
    }

    // ─────────────────────────────────────────────
    //  GET /Alertas/preview
    //  Lista quem vai receber alerta (para debug / n8n)
    // ─────────────────────────────────────────────
    [HttpGet("preview")]
    public async Task<IActionResult> Preview()
    {
        var agora              = DateTime.UtcNow;
        var limiteAlertaCashback = agora.AddDays(DiasAlertaCashback);
        var corteMovimentacao  = agora.AddDays(-(ValidadePontos - DiasAlertaPontos)); // 350 dias atrás

        // Cashback: lotes ativos que expiram nos próximos 15 dias
        var cashback = await _db.CashbackLotes!
            .Where(l => l.Ativo && l.DataExpiracao > agora && l.DataExpiracao <= limiteAlertaCashback)
            .GroupBy(l => l.CpfCliente)
            .Select(g => new
            {
                Cpf          = g.Key,
                ValorTotal   = g.Sum(l => l.Restante),
                ExpiracaoMin = g.Min(l => l.DataExpiracao)
            })
            .ToListAsync();

        // Pontos: clientes com pontos cuja última movimentação foi há 350+ dias
        var pontos = await _db.Clientes!
            .Where(c => c.Pontos > 0
                     && c.DataUltimaMovimentacao != null
                     && c.DataUltimaMovimentacao <= corteMovimentacao)
            .Select(c => new
            {
                c.Cpf,
                c.Nome,
                c.Pontos,
                DiasRestantes = ValidadePontos - (int)((agora - c.DataUltimaMovimentacao!.Value).TotalDays)
            })
            .ToListAsync();

        return Ok(new { cashbackExpirando = cashback, pontosExpirando = pontos });
    }

    // ─────────────────────────────────────────────
    //  POST /Alertas/enviar
    //  Chame este endpoint via n8n diariamente (sugestão: 09h00)
    //  Retorna quantos alertas foram disparados
    // ─────────────────────────────────────────────
    [HttpPost("enviar")]
    public async Task<IActionResult> EnviarAlertas()
    {
        var agora              = DateTime.UtcNow;
        var limiteAlertaCashback = agora.AddDays(DiasAlertaCashback);
        var corteMovimentacao  = agora.AddDays(-(ValidadePontos - DiasAlertaPontos));

        int alertasCashback = 0;
        int alertasPontos   = 0;

        // ── Alertas de cashback ──
        var lotesPorCliente = await _db.CashbackLotes!
            .Where(l => l.Ativo && l.DataExpiracao > agora && l.DataExpiracao <= limiteAlertaCashback)
            .GroupBy(l => l.CpfCliente)
            .Select(g => new
            {
                Cpf          = g.Key,
                ValorTotal   = g.Sum(l => l.Restante),
                ExpiracaoMin = g.Min(l => l.DataExpiracao)
            })
            .ToListAsync();

        foreach (var item in lotesPorCliente)
        {
            var cliente = await _db.Clientes!.FindAsync(item.Cpf);
            if (cliente is null || string.IsNullOrWhiteSpace(cliente.NumeroTelefone)) continue;

            int diasRestantes = Math.Max(1, (int)(item.ExpiracaoMin - agora).TotalDays);
            try
            {
                await _whatsapp.EnviarAlertaExpiracaoAsync(
                    cliente.Nome ?? "Cliente",
                    cliente.NumeroTelefone,
                    item.ValorTotal,
                    diasRestantes);
                alertasCashback++;
            }
            catch { /* falha silenciosa por cliente */ }
        }

        // ── Alertas de pontos ──
        var clientesPontos = await _db.Clientes!
            .Where(c => c.Pontos > 0
                     && c.DataUltimaMovimentacao != null
                     && c.DataUltimaMovimentacao <= corteMovimentacao)
            .ToListAsync();

        foreach (var cliente in clientesPontos)
        {
            if (string.IsNullOrWhiteSpace(cliente.NumeroTelefone)) continue;

            int diasRestantes = Math.Max(1,
                ValidadePontos - (int)(agora - cliente.DataUltimaMovimentacao!.Value).TotalDays);
            try
            {
                await _whatsapp.EnviarAlertaPontosAsync(
                    cliente.Nome ?? "Cliente",
                    cliente.NumeroTelefone,
                    cliente.Pontos ?? 0,
                    diasRestantes);
                alertasPontos++;
            }
            catch { /* falha silenciosa por cliente */ }
        }

        return Ok(new
        {
            mensagem        = $"{alertasCashback + alertasPontos} alertas enviados.",
            alertasCashback,
            alertasPontos
        });
    }
}
