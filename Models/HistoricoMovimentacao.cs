using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetoPontos.Models;

[Table("HistoricoMovimentacao")]
public class HistoricoMovimentacao : ITenantEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public string CpfCliente { get; set; } = string.Empty;

    public DateTime Data { get; set; } = DateTime.UtcNow;

    [Required]
    public string Operacao { get; set; } = string.Empty; // "Compra", "Resgate Cashback", "Resgate Pontos"

    public decimal Valor { get; set; }

    public int PontosMovimentados { get; set; }

    public int LojaId { get; set; }

    [ForeignKey("LojaId")]
    public Loja? Loja { get; set; }
}
