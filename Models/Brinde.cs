using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetoPontos.Models;

public class Brinde : ITenantEntity {
    [Key]
    public int? Id {get; set;}
    public string? Nome {get; set;}
    public int? ValorPontos{get; set;}

    public int LojaId { get; set; }

    [ForeignKey("LojaId")]
    public Loja? Loja { get; set; }
}