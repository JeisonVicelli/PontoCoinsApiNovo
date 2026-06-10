namespace ProjetoPontos.Services;

public static class ConfiguracoesFidelidade
{
    // ── Validade do cashback em dias ──
    public const int ValidadeCashbackDias = 60;

    // ── Multiplicadores por nível ──
    public const decimal MultiplicadorBronze = 1.0m;
    public const decimal MultiplicadorPrata  = 1.2m;
    public const decimal MultiplicadorOuro   = 1.5m;

    // ── Limiares de gasto total ──
    public const decimal LimitePrata = 1_000m;
    public const decimal LimiteOuro  = 3_000m;

    // ── Dia de pontos em dobro (null = inativo) ──
    public static DateTime? DataPontosEmDobro { get; set; } = null;

    public static string ObterNivel(decimal totalGasto) => totalGasto switch
    {
        >= 3_000m => "Ouro",
        >= 1_000m => "Prata",
        _         => "Bronze"
    };

    public static decimal ObterMultiplicador(decimal totalGasto) => totalGasto switch
    {
        >= 3_000m => MultiplicadorOuro,
        >= 1_000m => MultiplicadorPrata,
        _         => MultiplicadorBronze
    };

    public static bool EhDiaDePontosEmDobro()
        => DataPontosEmDobro.HasValue &&
           DateTime.UtcNow.Date == DataPontosEmDobro.Value.Date;
}
