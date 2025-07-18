using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;

namespace ProjetoPontos.Controllers;
[ApiController]
[Route("[controller]")]
public class ClienteController : ControllerBase
{
    private LojaDbContext _dbContext;
    public ClienteController(LojaDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    [HttpPost]
    [Route("cadastrar")]   
    public async Task<IActionResult> Cadastrar([FromBody]Cliente cliente)
    {
        if(_dbContext is null) return NotFound("DbContext não encontrado.");
        if(cliente is null) return BadRequest("Cliente Inválido.");
        if(_dbContext.Clientes is null) return NotFound("Tabela de Cliente não encontrado.");
        cliente.DefinirSenha(cliente.PasswordHash);
        await _dbContext.AddAsync(cliente);
        await _dbContext.SaveChangesAsync();
        return Created("",cliente);
    }
    
    [HttpGet]
    [Route("listar")]
    public async Task<ActionResult<IEnumerable<Cliente>>> Listar()
    {
        if(_dbContext is null) return NotFound("DbContext não encontrado.");
        if(_dbContext.Clientes is null) return NotFound();

        var clientes = await _dbContext.Clientes.ToListAsync();

        // Se deseja incluir clientes com atributos nulos
        return Ok(clientes);
    }
    [HttpGet]
    [Route("buscar/{cpf}")]
    public async Task<ActionResult<Cliente>> Buscar(string cpf)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Clientes is null) return NotFound();
        var clienteTemp = await _dbContext.Clientes.FindAsync(cpf);
        if(clienteTemp is null) return NotFound();
    return clienteTemp;
    }
    [HttpPut]
    [Route("alterar")]
    public async Task<ActionResult> Alterar(Cliente cliente)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Clientes is null) return NotFound();
        //var clienteTemp = await _dbContext.Cliente.FindAsync(cliente.Cpf);
        //if(clienteTemp is null) return NotFound();
        _dbContext.Clientes.Update(cliente);    
        await _dbContext.SaveChangesAsync();
        return Ok();  
    }

    [HttpPatch]
    [Route("mudarDescricao/{cpf}")]
    public async Task<ActionResult>MudarDescricao(string cpf,[FromForm] string Nome)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Clientes is null) return NotFound();
        var clienteTemp = await _dbContext.Clientes.FindAsync(cpf);
        if(clienteTemp is null) return NotFound();
        clienteTemp.Nome = Nome;                   
        await _dbContext.SaveChangesAsync();
        return Ok();  
    }
    [HttpDelete]
    [Route("excluir/{cpf}")]
    public async Task<ActionResult>Excluir(string cpf,[FromForm] string descricao)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Clientes is null) return NotFound();
        var clienteTemp = await _dbContext.Clientes.FindAsync(cpf);
        if(clienteTemp is null) return NotFound();    
        _dbContext.Clientes.Remove(clienteTemp);                 
        await _dbContext.SaveChangesAsync();
        return Ok("Cliente excluido com Sucesso");  
    }

  internal static void Buscar()
  {
    throw new NotImplementedException();
  }
}
