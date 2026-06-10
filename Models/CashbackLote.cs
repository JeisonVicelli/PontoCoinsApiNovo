using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetoPontos.Models;

[Table("cashbacklote")]
public class CashbackLote
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(14)]
    public string CpfCliente { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Valor { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Restante { get; set; }

    public DateTime DataGerado    { get; set; } = DateTime.UtcNow;

    public DateTime DataExpiracao { get; set; }

    public bool Ativo { get; set; } = true;

    public int? HistoricoOrigemId { get; set; }

    // ── Helpers ──
    public bool EstaAtivo(DateTime agora) => Ativo && DataExpiracao > agora;
}
