using System.ComponentModel.DataAnnotations;

namespace ProjetoPontos.Models;

public class Loja
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Nome { get; set; } = string.Empty;

    [Required]
    public string NumeroWhatsApp { get; set; } = string.Empty;

    [Required]
    public string ZApiInstanceId { get; set; } = string.Empty;

    [Required]
    public string ZApiToken { get; set; } = string.Empty;

    [Required]
    public string ZApiClientToken { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
}
