using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjetoPontos.Data;
using ProjetoPontos.Models;

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertaExpiracaoService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalcularDelayAte09h();
            _logger.LogInformation("Próximo disparo de alertas em {Minutos} min ({Hora} BRT).",
                (int)delay.TotalMinutes,
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.Add(delay), TzBrasilia).ToString("dd/MM HH:mm"));

            try { await Task.Delay(delay, stoppingToken); }
            catch (TaskCanceledException) { break; }

            if (stoppingToken.IsCancellationRequested) break;

            await EnviarAlertasAsync(stoppingToken);
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
        using var scope  = _scopeFactory.CreateScope();
        var options      = scope.ServiceProvider.GetRequiredService<DbContextOptions<LojaDbContext>>();
        var whatsapp     = scope.ServiceProvider.GetRequiredService<WhatsAppService>();

        List<Loja> lojas;
        using (var db = new LojaDbContext(options, new StaticTenantProvider(0)))
        {
            lojas = await db.Lojas!.Where(l => l.Ativo).ToListAsync(ct);
        }

        _logger.LogInformation("Processando alertas de {Quantidade} loja(s) ativa(s)...", lojas.Count);

        foreach (var loja in lojas)
        {
            if (ct.IsCancellationRequested) break;

            if (string.IsNullOrWhiteSpace(loja.ZApiInstanceId) ||
                string.IsNullOrWhiteSpace(loja.ZApiToken) ||
                string.IsNullOrWhiteSpace(loja.ZApiClientToken))
            {
                _logger.LogWarning("Loja {LojaId} ({Nome}) sem credenciais Z-API configuradas — alertas ignorados.", loja.Id, loja.Nome);
                continue;
            }

            using var db = new LojaDbContext(options, new StaticTenantProvider(loja.Id));
            await EnviarAlertasDaLojaAsync(db, whatsapp, loja, ct);
        }
    }

    private async Task EnviarAlertasDaLojaAsync(LojaDbContext db, WhatsAppService whatsapp, Loja loja, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;

        int alertasCashback = await ProcessarAlertasCashbackAsync(db, whatsapp, loja, agora, ct);
        int alertasPontos   = await ProcessarAlertasPontosAsync(db, whatsapp, loja, agora, ct);

        _logger.LogInformation(
            "Loja {LojaId}: {Cashback} alertas de cashback, {Pontos} alertas de pontos.",
            loja.Id, alertasCashback, alertasPontos);
    }

    private async Task<int> ProcessarAlertasCashbackAsync(LojaDbContext db, WhatsAppService whatsapp, Loja loja, DateTime agora, CancellationToken ct)
    {
        int alertasCashback = 0;
        var limiteAlertaCashback = agora.AddDays(DiasAlertaCashback);

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
                        loja, cliente.Nome ?? "Cliente", cliente.NumeroTelefone, item.ValorTotal, dias);
                    alertasCashback++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar alerta cashback para {Cpf} (Loja {LojaId}).", item.Cpf, loja.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar alertas de cashback da loja {LojaId}.", loja.Id);
        }

        return alertasCashback;
    }

    private async Task<int> ProcessarAlertasPontosAsync(LojaDbContext db, WhatsAppService whatsapp, Loja loja, DateTime agora, CancellationToken ct)
    {
        int alertasPontos = 0;
        var corteMovimentacao = agora.AddDays(-(ValidadePontos - DiasAlertaPontos));

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
                        loja, cliente.Nome ?? "Cliente", cliente.NumeroTelefone, cliente.Pontos ?? 0, dias);
                    alertasPontos++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao enviar alerta pontos para {Cpf} (Loja {LojaId}).", cliente.Cpf, loja.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar alertas de pontos da loja {LojaId}.", loja.Id);
        }

        return alertasPontos;
    }
}
