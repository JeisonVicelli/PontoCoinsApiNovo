using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace ProjetoPontos.Models;

[Keyless]
public class Login
{
    [Required(ErrorMessage = "O campo Username é obrigatório.")]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "O campo Username deve conter apenas letras, números e underscores.")]
    [StringLength(20, MinimumLength = 4, ErrorMessage = "O campo Username deve ter entre 4 e 20 caracteres.")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "O campo Password é obrigatório.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "O campo Password deve ter pelo menos 8 caracteres.")]
    public string? Password { get; set; }
}
