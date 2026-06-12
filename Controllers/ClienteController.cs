using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;
using ProjetoPontos.Services;

namespace ProjetoPontos.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ClienteController : ControllerBase
{
    private readonly LojaDbContext _dbContext;
    private readonly WhatsAppService _whatsapp;

    public ClienteController(LojaDbContext dbContext, WhatsAppService whatsapp)
    {
        _dbContext = dbContext;
        _whatsapp  = whatsapp;
    }

    [HttpPost("cadastrar")]
    public async Task<IActionResult> Cadastrar([FromBody] Cliente cliente)
    {
        if (cliente is null) return BadRequest("Dados do cliente inválidos.");
        if (_dbContext.Clientes is null) return StatusCode(500, "Tabela de clientes não encontrada.");

        try
        {
            cliente.DefinirSenha(cliente.PasswordHash!);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        var existe = await _dbContext.Clientes!
            .AnyAsync(c => c.Cpf == cliente.Cpf);
        if (existe) return Conflict("CPF já cadastrado.");

        await _dbContext.AddAsync(cliente);
        await _dbContext.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(cliente.NumeroTelefone))
                {
                    await _whatsapp.EnviarBoasVindasAsync(
                        cliente.Nome ?? "Cliente",
                        cliente.NumeroTelefone);
                }
            }
            catch { }
        });

        return Created("", cliente);
    }

    [HttpGet("listar")]
    public async Task<ActionResult<IEnumerable<Cliente>>> Listar()
    {
        if (_dbContext.Clientes is null) return NotFound();
        return Ok(await _dbContext.Clientes.ToListAsync());
    }

    [HttpGet("buscar/{cpf}")]
    public async Task<ActionResult<Cliente>> Buscar(string cpf)
    {
        if (_dbContext.Clientes is null) return NotFound();
        var cliente = await _dbContext.Clientes.FindAsync(cpf);
        return cliente is null ? NotFound("Cliente não encontrado.") : Ok(cliente);
    }

    [HttpPut("alterar")]
    public async Task<ActionResult> Alterar([FromBody] Cliente cliente)
    {
        if (_dbContext.Clientes is null) return NotFound();
        _dbContext.Clientes.Update(cliente);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPatch("mudarDescricao/{cpf}")]
    public async Task<ActionResult> MudarDescricao(string cpf, [FromForm] string Nome)
    {
        if (_dbContext.Clientes is null) return NotFound();
        var cliente = await _dbContext.Clientes.FindAsync(cpf);
        if (cliente is null) return NotFound();
        cliente.Nome = Nome;
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("excluir/{cpf}")]
    public async Task<ActionResult> Excluir(string cpf)
    {
        if (_dbContext.Clientes is null) return NotFound();
        var cliente = await _dbContext.Clientes.FindAsync(cpf);
        if (cliente is null) return NotFound("Cliente não encontrado.");
        _dbContext.Clientes.Remove(cliente);
        await _dbContext.SaveChangesAsync();
        return Ok("Cliente excluído com sucesso.");
    }
}
