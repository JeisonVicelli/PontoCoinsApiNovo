using System.ComponentModel.DataAnnotations;

namespace ProjetoPontos.Models
{
    public class Usuario : Pessoa
    {
        [Required(ErrorMessage = "O campo Cargo é obrigatório.")]
        public string Cargo { get; set; } = string.Empty;
        
    }
}
