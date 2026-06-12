using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;
using ProjetoPontos.Services;

namespace ProjetoPontos.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly LojaDbContext _dbContext;
        private readonly TokenService _tokenService;

        public LoginController(LojaDbContext dbContext, TokenService tokenService)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
        }

        [HttpPost]
        [Route("autenticar")]
        public IActionResult Autenticar([FromBody] Login login)
        {
            if (login == null || string.IsNullOrEmpty(login.Username) || string.IsNullOrEmpty(login.Password))
            {
                return BadRequest("Login inválido.");
            }

            var usuario = _dbContext.Usuarios.IgnoreQueryFilters().FirstOrDefault(u => u.UserName == login.Username);
            if (usuario != null && usuario.VerificarSenha(login.Password))
            {
                var token = _tokenService.GerarToken(usuario.Id.ToString(), usuario.UserName, usuario.Cargo, usuario.LojaId);
                return Ok(new
                {
                    token,
                    tipoConta = "Usuario",
                    id = usuario.Id,
                    userName = usuario.UserName,
                    cargo = usuario.Cargo,
                    lojaId = usuario.LojaId
                });
            }

            var cliente = _dbContext.Clientes.IgnoreQueryFilters().FirstOrDefault(c => c.UserName == login.Username);
            if (cliente != null && cliente.VerificarSenha(login.Password))
            {
                var token = _tokenService.GerarToken(cliente.Cpf!, cliente.UserName!, "Cliente", cliente.LojaId);
                return Ok(new
                {
                    token,
                    tipoConta = "Cliente",
                    cpf = cliente.Cpf,
                    userName = cliente.UserName,
                    nome = cliente.Nome,
                    cargo = "Cliente",
                    lojaId = cliente.LojaId
                });
            }

            return Unauthorized("Login inválido.");
        }
    }
}
