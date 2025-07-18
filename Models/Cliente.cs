using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ProjetoPontos.Models
{
    public class Cliente : Pessoa
    {
        [Required(ErrorMessage = "O campo Número de Telefone é obrigatório.")]
        public string? NumeroTelefone { get; set; }  
        public int? Pontos { get; set; }
        public decimal ValorTotalGasto { get; set; }      
        public bool IsTelefoneValido()
        {
            // Validação de telefone: Aceita números, parênteses, espaços e hífens
            if (string.IsNullOrWhiteSpace(NumeroTelefone))
                return false;

            string cleanPhoneNumber = Regex.Replace(NumeroTelefone, @"[^\d]", ""); // Remove caracteres não numéricos

            // O número de telefone deve ter no mínimo 10 dígitos
            return cleanPhoneNumber.Length >= 10;
        }
        public void AtualizarValorTotalGasto(decimal valorGasto)
    {
        if (valorGasto < 0)
        {
            throw new ArgumentException("O valor gasto não pode ser negativo.");
        }

        ValorTotalGasto += valorGasto;
    }
    }
}

