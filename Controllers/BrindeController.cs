using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;

namespace ProjetoPontos.Controllers;
[ApiController]
[Route("[controller]")]
public class BrindeController : ControllerBase
{
    private LojaDbContext _dbContext;
    public BrindeController(LojaDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    [HttpPost]
    [Route("cadastrar")]   
    public IActionResult Criar(Brinde brinde)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Brindes is null) return NotFound();
        _dbContext.AddAsync(brinde);
        _dbContext.SaveChangesAsync();
        return Created("",brinde);
    }
    [HttpGet]
    [Route("listar")]
    public async Task<ActionResult<IEnumerable<Brinde>>> Listar()
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Brindes is null) return NotFound();
        return await _dbContext.Brindes.ToListAsync();
    }
    [HttpGet]
    [Route("buscar/{id}")]
    public async Task<ActionResult<Brinde>> Buscar(int id)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Brindes is null) return NotFound();
        var brindeTemp = await _dbContext.Brindes.FindAsync(id);
        if(brindeTemp is null) return NotFound();
    return brindeTemp;
    }
    [HttpPut]
    [Route("alterar")]
    public async Task<ActionResult> Alterar(Brinde brinde)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Brindes is null) return NotFound();
        //var clienteTemp = await _dbContext.Cliente.FindAsync(cliente.Cpf);
        //if(clienteTemp is null) return NotFound();
        _dbContext.Brindes.Update(brinde);    
        await _dbContext.SaveChangesAsync();
        return Ok();  
    }

    [HttpPatch]
    [Route("mudarDescricao/{id}")]
    public async Task<ActionResult>MudarDescricao(int id,[FromForm] string nome)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Brindes is null) return NotFound();
        var brindeTemp = await _dbContext.Brindes.FindAsync(id);
        if(brindeTemp is null) return NotFound();
        brindeTemp.Nome = nome;                   
        await _dbContext.SaveChangesAsync();
        return Ok();  
    }
    [HttpDelete]
    [Route("excluir/{id}")]
    public async Task<ActionResult>Excluir(int id,[FromForm] string nome)
    {
        if(_dbContext is null) return NotFound();
        if(_dbContext.Brindes is null) return NotFound();
        var brindeTemp = await _dbContext.Brindes.FindAsync(id);
        if(brindeTemp is null) return NotFound();    
        _dbContext.Brindes.Remove(brindeTemp);                 
        await _dbContext.SaveChangesAsync();
        return Ok("Brinde excluido com Sucesso!");  
    }
}