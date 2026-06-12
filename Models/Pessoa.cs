using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjetoPontos.Models
{
    public class Pessoa
    {
        [Key]
        [Required(ErrorMessage = "O campo CPF é obrigatório.")]
        [RegularExpression(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$", ErrorMessage = "O CPF deve estar no formato 999.999.999-99.")]
        public string? Cpf { get; set; }

        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        public string? Nome { get; set; }
        
        [EmailAddress(ErrorMessage = "O campo Email deve estar em um formato de e-mail válido.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "O campo Data de Cadastro é obrigatório.")]
        public DateTime? DataCadastro { get; set; }

        public DateTime? DataUltimaMovimentacao { get; set; }

        public DateTime? DataNascimento { get; set; }
    private string? userName;
    [Required(ErrorMessage = "O campo UserName é obrigatório.")]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "O campo UserName deve conter apenas letras, números e underscores.")]
    [StringLength(20, MinimumLength = 4, ErrorMessage = "O campo UserName deve ter entre 4 e 20 caracteres.")]
    public string? UserName
    {
        get => userName;
        set
        {
            if (!IsUserNameValid(value))
            {
                throw new ArgumentException("Nome de usuário inválido.");
            }
            userName = value;
        }
    }

    private string? passwordHash;
    public string? PasswordHash
    {
        get => passwordHash;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Senha inválida.");

            // Se for hash Base64 do SHA-256 (44 chars terminando em =), atribui direto
            if (value.Length == 44 && value.EndsWith("="))
            {
                passwordHash = value;
                return;
            }

            // Caso contrário valida como senha em texto plano
            if (!IsPasswordValid(value))
                throw new ArgumentException("Senha inválida.");

            passwordHash = value;
        }
    }

    public void DefinirSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha) || !IsPasswordValid(senha))
            throw new ArgumentException("Senha deve ter mínimo 8 caracteres, uma maiúscula, uma minúscula e um número.");

        using var sha256 = SHA256.Create();
        byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(senha));
        passwordHash = Convert.ToBase64String(hashedBytes); // campo privado direto — evita re-validar o hash
    }

    // Senha temporária gerada pelo sistema (ex.: 6 dígitos numéricos no cadastro inline) — não passa
    // pela validação de senha "forte", pois o cliente é obrigado a trocá-la (ver PrecisaTrocarSenha).
    public void DefinirSenhaTemporaria(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
            throw new ArgumentException("Senha inválida.");

        using var sha256 = SHA256.Create();
        byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(senha));
        passwordHash = Convert.ToBase64String(hashedBytes);
    }

    // Método para verificar se a senha está correta
    public bool VerificarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
        {
            return false;
        }

        using (var sha256 = SHA256.Create())
        {
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(senha));
            string hashedPassword = Convert.ToBase64String(hashedBytes);
            return PasswordHash == hashedPassword;
        }
    }

    private bool IsUserNameValid(string? userName)
    {
        // Permitir apenas letras, números e underscore
        return !string.IsNullOrWhiteSpace(userName) &&
               Regex.IsMatch(userName, "^[a-zA-Z0-9_]+$") &&
               userName.Length >= 4 && userName.Length <= 20;
    }

   private bool IsPasswordValid(string? password)
{
    return !string.IsNullOrWhiteSpace(password) && 
           password.Length >= 8 &&
           password.Any(char.IsUpper) &&  // Pelo menos uma letra maiúscula
           password.Any(char.IsLower) &&  // Pelo menos uma letra minúscula
           password.Any(char.IsDigit);    // Pelo menos um dígito
}
}

}
