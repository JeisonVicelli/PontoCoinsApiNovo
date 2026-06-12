using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjetoPontos.Models;

public class Usuario : ITenantEntity
{
    [Key]
    public int Id { get; set; }

    public int LojaId { get; set; }

    [ForeignKey("LojaId")]
    public Loja? Loja { get; set; }

    private string _userName = string.Empty;

    [Required]
    public string UserName
    {
        get => _userName;
        set
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !Regex.IsMatch(value, "^[a-zA-Z0-9_]+$") ||
                value.Length < 4 || value.Length > 20)
                throw new ArgumentException("UserName deve ter 4-20 caracteres (letras, números ou _).");
            _userName = value;
        }
    }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string Cargo { get; set; } = string.Empty;

    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    public void DefinirSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
            throw new ArgumentException("Senha não pode ser vazia.");
        using var sha256 = SHA256.Create();
        PasswordHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(senha)));
    }

    public bool VerificarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha)) return false;
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(senha))) == PasswordHash;
    }
}
