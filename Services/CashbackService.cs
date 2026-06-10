using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;

namespace ProjetoPontos.Services;

public class CashbackService
{
    private readonly LojaDbContext _db;
    private const int ValidadeDias = 60;

    public CashbackService(LojaDbContext db) => _db = db;

    // ── Cria um lote ao registrar uma compra ──
    // valorCashback = valor já calculado pelo controller (2,5% ou 5% com multiplicador)
    public async Task CriarLoteAsync(string cpf, decimal valorCashback, int? historicoOrigemId = null)
    {
        if (_db.CashbackLotes is null || valorCashback <= 0) return;

        decimal cashback = Math.Round(valorCashback, 2);
        if (cashback <= 0) return;

        await _db.CashbackLotes.AddAsync(new CashbackLote
        {
            CpfCliente       = cpf,
            Valor            = cashback,
            Restante         = cashback,
            DataGerado       = DateTime.UtcNow,
            DataExpiracao    = DateTime.UtcNow.AddDays(ValidadeDias),
            Ativo            = true,
            HistoricoOrigemId = historicoOrigemId
        });
        await _db.SaveChangesAsync();
    }

    // ── Saldo ativo (não expirado, não utilizado) ──
    public async Task<decimal> GetSaldoAtivoAsync(string cpf)
    {
        if (_db.CashbackLotes is null) return 0;
        try
        {
            var agora = DateTime.UtcNow;
            return await _db.CashbackLotes
                .Where(l => l.CpfCliente == cpf && l.Ativo && l.DataExpiracao > agora)
                .SumAsync(l => l.Restante);
        }
        catch { return 0; }
    }

    // ── Consome cashback (FIFO — expira primeiro) ──
    public async Task<decimal> AplicarCashbackAsync(string cpf, decimal valorSolicitado)
    {
        if (_db.CashbackLotes is null || valorSolicitado <= 0) return 0;

        var agora = DateTime.UtcNow;
        var lotes = await _db.CashbackLotes
            .Where(l => l.CpfCliente == cpf && l.Ativo && l.DataExpiracao > agora)
            .OrderBy(l => l.DataExpiracao)
            .ToListAsync();

        decimal restante = valorSolicitado;
        decimal totalConsumido = 0;

        foreach (var lote in lotes)
        {
            if (restante <= 0) break;

            if (lote.Restante <= restante)
            {
                lote.Ativo  = false;
                restante   -= lote.Restante;
                totalConsumido += lote.Restante;
            }
            else
            {
                decimal sobra = lote.Restante - restante;
                lote.Ativo    = false;
                totalConsumido += restante;

                await _db.CashbackLotes.AddAsync(new CashbackLote
                {
                    CpfCliente       = cpf,
                    Valor            = sobra,
                    Restante         = sobra,
                    DataGerado       = lote.DataGerado,
                    DataExpiracao    = lote.DataExpiracao,
                    Ativo            = true,
                    HistoricoOrigemId = lote.HistoricoOrigemId
                });
                restante = 0;
            }
        }

        if (totalConsumido > 0) await _db.SaveChangesAsync();
        return Math.Round(totalConsumido, 2);
    }

    // ── Expira lotes vencidos e desconta do CashbackAcumulado do cliente ──
    public async Task ExpirarLotesAsync(string cpf, Cliente cliente)
    {
        if (_db.CashbackLotes is null) return;
        try
        {
            var agora = DateTime.UtcNow;
            var vencidos = await _db.CashbackLotes
                .Where(l => l.CpfCliente == cpf && l.Ativo && l.DataExpiracao <= agora)
                .ToListAsync();

            if (!vencidos.Any()) return;

            decimal totalExpirado = vencidos.Sum(l => l.Restante);
            vencidos.ForEach(l => l.Ativo = false);
            cliente.CashbackAcumulado = Math.Max(0, cliente.CashbackAcumulado - totalExpirado);
            await _db.SaveChangesAsync();
        }
        catch { /* silencioso — não bloqueia a operação principal */ }
    }

    // ── Extrato de lotes do cliente ──
    public async Task<List<CashbackLote>> GetExtratoPorCpfAsync(string cpf, int top = 20)
    {
        if (_db.CashbackLotes is null) return new();
        try
        {
            return await _db.CashbackLotes
                .Where(l => l.CpfCliente == cpf)
                .OrderByDescending(l => l.DataGerado)
                .Take(top)
                .ToListAsync();
        }
        catch { return new(); }
    }

    public async Task SincronizarSaldoClienteAsync(Cliente cliente)
    {
        await ExpirarLotesAsync(cliente.Cpf!, cliente);
        cliente.CashbackAcumulado = await GetSaldoAtivoAsync(cliente.Cpf!);
    }
}
