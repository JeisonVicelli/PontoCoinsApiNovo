using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Filters;
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
    private readonly ClienteConsultaService _consultaSvc;
    private readonly ILogger<ClienteController> _logger;

    public ClienteController(LojaDbContext dbContext, WhatsAppService whatsapp, ClienteConsultaService consultaSvc, ILogger<ClienteController> logger)
    {
        _dbContext   = dbContext;
        _whatsapp    = whatsapp;
        _consultaSvc = consultaSvc;
        _logger      = logger;
    }

    [HttpPost("cadastrar")]
    [Authorize(Policy = "Funcionario")]
    public async Task<IActionResult> Cadastrar([FromBody] Cliente cliente)
    {
        if (cliente is null) return BadRequest("Dados do cliente inválidos.");
        if (_dbContext.Clientes is null) return StatusCode(500, "Tabela de clientes não encontrada.");

        var senhaTemporaria = GerarSenhaTemporaria();

        try
        {
            cliente.DefinirSenhaTemporaria(senhaTemporaria);
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

        var loja = await _dbContext.Lojas!.FindAsync(cliente.LojaId);

        _ = Task.Run(async () =>
        {
            try
            {
                if (loja is not null && !string.IsNullOrWhiteSpace(cliente.NumeroTelefone))
                {
                    var enviado = await _whatsapp.EnviarBoasVindasAsync(loja, cliente, senhaTemporaria);
                    if (!enviado)
                        _logger.LogWarning("Não foi possível enviar a senha temporária por WhatsApp para o cliente {Cpf}.", cliente.Cpf);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao enviar a senha temporária por WhatsApp para o cliente {Cpf}.", cliente.Cpf);
            }
        });

        return Created("", cliente);
    }

    // Gera uma senha temporária de 6 dígitos numéricos para o cadastro inline no balcão.
    // O cliente é obrigado a trocá-la no primeiro acesso (PrecisaTrocarSenha).
    private static string GerarSenhaTemporaria()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    [HttpGet("listar")]
    [Authorize(Policy = "Funcionario")]
    public async Task<ActionResult<IEnumerable<Cliente>>> Listar()
    {
        if (_dbContext.Clientes is null) return NotFound();
        return Ok(await _dbContext.Clientes.ToListAsync());
    }

    [HttpGet("buscar/{cpf}")]
    [Authorize(Policy = "Funcionario")]
    public async Task<ActionResult<Cliente>> Buscar(string cpf)
    {
        if (_dbContext.Clientes is null) return NotFound();
        var cliente = await _dbContext.Clientes.FindAsync(cpf);
        return cliente is null ? NotFound("Cliente não encontrado.") : Ok(cliente);
    }

    [HttpPut("alterar")]
    [Authorize(Policy = "Funcionario")]
    public async Task<ActionResult> Alterar([FromBody] Cliente cliente)
    {
        if (_dbContext.Clientes is null) return NotFound();
        _dbContext.Clientes.Update(cliente);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPatch("mudarDescricao/{cpf}")]
    [Authorize(Policy = "Funcionario")]
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
    [Authorize(Policy = "Funcionario")]
    public async Task<ActionResult> Excluir(string cpf)
    {
        if (_dbContext.Clientes is null) return NotFound();
        var cliente = await _dbContext.Clientes.FindAsync(cpf);
        if (cliente is null) return NotFound("Cliente não encontrado.");
        _dbContext.Clientes.Remove(cliente);
        await _dbContext.SaveChangesAsync();
        return Ok("Cliente excluído com sucesso.");
    }

    // ─────────────────────────────────────────────
    //  GET meu-saldo
    //  Saldo/cashback do próprio cliente autenticado
    // ─────────────────────────────────────────────
    [HttpGet("meu-saldo")]
    [Authorize(Policy = "Cliente")]
    [ExigeSenhaDefinida]
    public async Task<IActionResult> MeuSaldo()
    {
        if (_dbContext.Clientes is null) return NotFound();

        var cpf = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(cpf)) return Unauthorized();

        var cliente = await _dbContext.Clientes.FindAsync(cpf);
        if (cliente is null) return NotFound("Cliente não encontrado.");

        return Ok(await _consultaSvc.ObterSaldoAsync(cliente));
    }

    // ─────────────────────────────────────────────
    //  GET meu-extrato
    //  Últimas movimentações do próprio cliente autenticado
    // ─────────────────────────────────────────────
    [HttpGet("meu-extrato")]
    [Authorize(Policy = "Cliente")]
    [ExigeSenhaDefinida]
    public async Task<IActionResult> MeuExtrato([FromQuery] int top = 5)
    {
        var cpf = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(cpf)) return Unauthorized();

        return Ok(await _consultaSvc.ObterExtratoAsync(cpf, top));
    }

    // ─────────────────────────────────────────────
    //  GET meus-alertas
    //  Alerta de expiração de cashback do próprio cliente autenticado
    // ─────────────────────────────────────────────
    [HttpGet("meus-alertas")]
    [Authorize(Policy = "Cliente")]
    [ExigeSenhaDefinida]
    public async Task<IActionResult> MeusAlertas([FromQuery] int diasAlerta = 15)
    {
        if (_dbContext.Clientes is null) return NotFound();

        var cpf = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(cpf)) return Unauthorized();

        var cliente = await _dbContext.Clientes.FindAsync(cpf);
        if (cliente is null) return NotFound("Cliente não encontrado.");

        return Ok(await _consultaSvc.ObterExpiracaoAsync(cliente, diasAlerta));
    }

    // ─────────────────────────────────────────────
    //  POST trocar-senha
    //  Troca a senha do próprio cliente autenticado e limpa PrecisaTrocarSenha
    // ─────────────────────────────────────────────
    [HttpPost("trocar-senha")]
    [Authorize(Policy = "Cliente")]
    public async Task<IActionResult> TrocarSenha([FromBody] TrocarSenhaModel model)
    {
        if (_dbContext.Clientes is null) return NotFound();
        if (model is null || string.IsNullOrWhiteSpace(model.SenhaAtual) || string.IsNullOrWhiteSpace(model.NovaSenha))
            return BadRequest("Informe a senha atual e a nova senha.");

        var cpf = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(cpf)) return Unauthorized();

        var cliente = await _dbContext.Clientes.FindAsync(cpf);
        if (cliente is null) return NotFound("Cliente não encontrado.");

        if (!cliente.VerificarSenha(model.SenhaAtual))
            return BadRequest("Senha atual incorreta.");

        try
        {
            cliente.DefinirSenha(model.NovaSenha);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }

        cliente.PrecisaTrocarSenha = false;
        await _dbContext.SaveChangesAsync();

        return Ok("Senha alterada com sucesso.");
    }
}

public class TrocarSenhaModel
{
    public string SenhaAtual { get; set; } = string.Empty;
    public string NovaSenha { get; set; } = string.Empty;
}
