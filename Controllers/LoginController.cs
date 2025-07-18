using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;
using System.Security.Cryptography;
using System.Text;

namespace ProjetoPontos.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly LojaDbContext _dbContext;

        public LoginController(LojaDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        [Route("autenticar")]
        public IActionResult Autenticar([FromBody] Login login)
    {
      if (login == null || string.IsNullOrEmpty(login.Username) || string.IsNullOrEmpty(login.Password))
      {
        return BadRequest("Login inválido.");
      }

      var usuarioAutenticado = AutenticarUsuario(login.Username, login.Password);
      if (usuarioAutenticado != null)
      {
        return Ok(usuarioAutenticado);
      }      
      var clienteAutenticado = AutenticarCliente(login.Username, login.Password);
      if (clienteAutenticado == null)
      {
        return Unauthorized("Login inválido.");
      }

      return Ok(clienteAutenticado);
    }

    private Usuario AutenticarUsuario(string username, string password)
        {
            // Consultar o banco de dados para encontrar o usuário com o username informado
            var usuario = _dbContext.Usuarios.FirstOrDefault(u => u.UserName == username);

            // Verificar se o usuário foi encontrado e se a senha corresponde
            if (usuario != null && usuario.VerificarSenha(password))
            {
                return usuario; // Autenticação bem-sucedida
            }

            return null; // Usuário não encontrado ou senha incorreta
        }
        private Cliente AutenticarCliente(string username, string password)
        {
            // Consultar o banco de dados para encontrar o cliente com o username informado
            var cliente = _dbContext.Clientes.FirstOrDefault(u => u.UserName == username);

            // Verificar se o usuário foi encontrado e se a senha corresponde
            if (cliente != null && cliente.VerificarSenha(password))
            {
                return cliente; // Autenticação bem-sucedida
            }

            return null; // Usuário não encontrado ou senha incorreta
        }
         private bool VerificarSenha(string senha, string hashedPassword)
        {
            using (SHA256 sha256 = new SHA256Managed())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(senha));
                string hashedPasswordInput = Convert.ToBase64String(hashedBytes);
                return hashedPasswordInput == hashedPassword;
            }
        }
    }
}
