using System.ComponentModel.DataAnnotations;

namespace ProjetoPontos.Models;

public class Brinde{
    [Key]
    public int? Id {get; set;}
    public string? Nome {get; set;}
    public int? ValorPontos{get; set;} 
}