using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;

namespace ProjetoPontos.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class UsuarioController : ControllerBase
{
    private readonly LojaDbContext _dbContext;

    public UsuarioController(LojaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    [Route("cadastrar")]
    public async Task<IActionResult> CadastrarUsuario([FromBody] Usuario usuario)
    {
        if (_dbContext.Usuarios is null) return NotFound("Tabela de Usuario não encontrada.");
        if (usuario is null) return BadRequest("Usuario inválido.");
        usuario.DefinirSenha(usuario.PasswordHash);
        usuario.DataCadastro = DateTime.UtcNow;
        await _dbContext.AddAsync(usuario);
        await _dbContext.SaveChangesAsync();
        return Created("", usuario);
    }

    [HttpGet]
    [Route("listar")]
    public async Task<ActionResult<IEnumerable<Usuario>>> Listar()
    {
        if (_dbContext.Usuarios is null) return NotFound();
        return Ok(await _dbContext.Usuarios.ToListAsync());
    }

    [HttpGet]
    [Route("buscar/{id}")]
    public async Task<ActionResult<Usuario>> Buscar(int id)
    {
        if (_dbContext.Usuarios is null) return NotFound();
        var usuario = await _dbContext.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();
        return usuario;
    }

    [HttpPut]
    [Route("alterar")]
    public async Task<ActionResult> Alterar([FromBody] Usuario usuario)
    {
        if (_dbContext.Usuarios is null) return NotFound();
        _dbContext.Usuarios.Update(usuario);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPatch]
    [Route("mudarCargo/{id}")]
    public async Task<ActionResult> MudarCargo(int id, [FromForm] string cargo)
    {
        if (_dbContext.Usuarios is null) return NotFound();
        var usuario = await _dbContext.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();
        usuario.Cargo = cargo;
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete]
    [Route("excluir/{id}")]
    public async Task<ActionResult> Excluir(int id)
    {
        if (_dbContext.Usuarios is null) return NotFound();
        var usuario = await _dbContext.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();
        _dbContext.Usuarios.Remove(usuario);
        await _dbContext.SaveChangesAsync();
        return Ok("Usuario excluido com Sucesso!");
    }
}
