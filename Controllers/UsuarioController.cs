using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;

namespace ProjetoPontos.Controllers;
[ApiController]
[Route("[controller]")]
public class UsuarioController : ControllerBase
{
    private LojaDbContext _dbContext;
    public UsuarioController(LojaDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    [HttpPost]
    [Route("cadastrar")]   
    public async Task<IActionResult> CadastrarUsuario([FromBody]Usuario usuario)
    {
        if(_dbContext is null) return NotFound("DbContext não encontrado.");
        if(usuario is null) return BadRequest("Usuario Inválido.");
        if(_dbContext.Usuarios is null) return NotFound("Tabela de Usuario não encontrado.");
        usuario.DefinirSenha(usuario.PasswordHash);
        await _dbContext.AddAsync(usuario);
        await _dbContext.SaveChangesAsync();
        return Created("",usuario);
    }
    [HttpGet]
    [Route("listar")]
    public async Task<ActionResult<IEnumerable<Usuario>>> Listar()
    {
         if(_dbContext is null) return NotFound("DbContext não encontrado.");
        if(_dbContext.Usuarios is null) return NotFound();

        var usuario = await _dbContext.Usuarios.ToListAsync();

        // Se deseja incluir clientes com atributos nulos
        return Ok(usuario);
    }
    [HttpGet]
    [Route("buscar/{cpf}")]
    public async Task<ActionResult<Usuario>> Buscar(string cpf)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Usuarios is null) return NotFound();
        var usuarioTemp = await _dbContext.Usuarios.FindAsync(cpf);
        if(usuarioTemp is null) return NotFound();
    return usuarioTemp;
    }
    [HttpPut]
    [Route("alterar")]
    public async Task<ActionResult> Alterar(Usuario usuario)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Usuarios is null) return NotFound();
        //var clienteTemp = await _dbContext.Cliente.FindAsync(cliente.Cpf);
        //if(clienteTemp is null) return NotFound();
        _dbContext.Usuarios.Update(usuario);    
        await _dbContext.SaveChangesAsync();
        return Ok();  
    }

    [HttpPatch]
    [Route("mudarDescricao/{cpf}")]
    public async Task<ActionResult>MudarDescricao(string cpf,[FromForm] string Nome)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Usuarios is null) return NotFound();
        var usuarioTemp = await _dbContext.Usuarios.FindAsync(cpf);
        if(usuarioTemp is null) return NotFound();
        usuarioTemp.Nome = Nome;                   
        await _dbContext.SaveChangesAsync();
        return Ok();  
    }
    [HttpDelete]
    [Route("excluir/{cpf}")]
    public async Task<ActionResult>Excluir(string cpf,[FromForm] string descricao)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Usuarios is null) return NotFound();
        var usuarioTemp = await _dbContext.Usuarios.FindAsync(cpf);
        if(usuarioTemp is null) return NotFound();    
        _dbContext.Usuarios.Remove(usuarioTemp);                 
        await _dbContext.SaveChangesAsync();
        return Ok("Usuario excluido com Sucesso!");  
    }
     internal static void Buscar()
  {
    throw new NotImplementedException();
  }
}