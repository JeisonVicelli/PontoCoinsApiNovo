using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjetoPontos.Data;

namespace ProjetoPontos.Services;

public class AlertaExpiracaoService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertaExpiracaoService> _logger;

    private const int DiasAlertaCashback = 15;
    private const int DiasAlertaPontos   = 15;
    private const int ValidadePontos     = 365;

    private static readonly TimeZoneInfo TzBrasilia =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");

    public AlertaExpiracaoService(IServiceScopeFactory scopeFactory,
                                   ILogger<AlertaExpiracaoService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("AlertaExpiracaoService iniciado.");

        while (!ct.IsCancellationRequested)
        {
            var delay = CalcularDelayAte09h();
            _logger.LogInformation("Próximo disparo de alertas em {Minutos} min ({Hora} BRT).",
                (int)delay.TotalMinutes,
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Add(delay), TzBrasilia).ToString("dd/MM HH:mm"));

            try { await Task.Delay(delay, ct); }
            catch (TaskCanceledException) { break; }

            if (ct.IsCancellationRequested) break;

            await EnviarAlertasAsync(ct);
        }

        _logger.LogInformation("AlertaExpiracaoService encerrado.");
    }

    private static TimeSpan CalcularDelayAte09h()
    {
        var agora       = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TzBrasilia);
        var proximo09h  = agora.Date.AddHours(9);

        // se já passou das 09:00 hoje, agenda para amanhã
        if (agora >= proximo09h)
            proximo09h = proximo09h.AddDays(1);

        var proximoUtc = TimeZoneInfo.ConvertTimeToUtc(proximo09h, TzBrasilia);
        return proximoUtc - DateTime.UtcNow;
    }

    private async Task EnviarAlertasAsync(CancellationToken ct)
    {
        _logger.LogInformation("Iniciando envio de alertas de expiração...");
        int alertasCashback = 0, alertasPontos = 0;

        using var scope    = _scopeFactory.CreateScope();
        var db             = scope.ServiceProvider.GetRequiredService<LojaDbContext>();
        var whatsapp       = scope.ServiceProvider.GetRequiredService<WhatsAppService>();

        var agora                = DateTime.UtcNow;
        var limiteAlertaCashback = agora.AddDays(DiasAlertaCashback);
        var corteMovimentacao    = agora.AddDays(-(ValidadePontos - DiasAlertaPontos));

        // ── Cashback expirando em 15 dias ──
        try
        {
            var lotesPorCliente = await db.CashbackLotes!
                .Where(l => l.Ativo && l.DataExpiracao > agora && l.DataExpiracao <= limiteAlertaCashback)
                .GroupBy(l => l.CpfCliente)
                .Select(g => new
                {
                    Cpf          = g.Key,
                    ValorTotal   = g.Sum(l => l.Restante),
                    ExpiracaoMin = g.Min(l => l.DataExpiracao)
                })
                .ToListAsync(ct);

            foreach (var item in lotesPorCliente)
            {
                if (ct.IsCancellationRequested) break;
                var cliente = await db.Clientes!.FindAsync(new object[] { item.Cpf }, ct);
                if (cliente is null || string.IsNullOrWhiteSpace(cliente.NumeroTelefone)) continue;

                int dias = Math.Max(1, (int)(item.ExpiracaoMin - agora).TotalDays);
                try
                {
                    await whatsapp.EnviarAlertaExpiracaoAsync(
                        cliente.Nome ?? "Cliente", cliente.NumeroTelefone, item.ValorTotal, dias);
                    alertasCashback++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Falha ao enviar alerta cashback para {Cpf}: {Msg}", item.Cpf, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar alertas de cashback.");
        }

        // ── Pontos expirando em 15 dias ──
        try
        {
            var clientesPontos = await db.Clientes!
                .Where(c => c.Pontos > 0
                         && c.DataUltimaMovimentacao != null
                         && c.DataUltimaMovimentacao <= corteMovimentacao)
                .ToListAsync(ct);

            foreach (var cliente in clientesPontos)
            {
                if (ct.IsCancellationRequested) break;
                if (string.IsNullOrWhiteSpace(cliente.NumeroTelefone)) continue;

                int dias = Math.Max(1,
                    ValidadePontos - (int)(agora - cliente.DataUltimaMovimentacao!.Value).TotalDays);
                try
                {
                    await whatsapp.EnviarAlertaPontosAsync(
                        cliente.Nome ?? "Cliente", cliente.NumeroTelefone, cliente.Pontos ?? 0, dias);
                    alertasPontos++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Falha ao enviar alerta pontos para {Cpf}: {Msg}", cliente.Cpf, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar alertas de pontos.");
        }

        _logger.LogInformation(
            "Alertas enviados: {Cashback} cashback, {Pontos} pontos.",
            alertasCashback, alertasPontos);
    }
}
