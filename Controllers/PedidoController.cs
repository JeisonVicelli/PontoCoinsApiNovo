using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoPontos.Data;
using ProjetoPontos.Models;

namespace Controles
{
    [Route("[controller]")]
    [ApiController]
    public class PedidoController : ControllerBase
    {
        private readonly LojaDbContext _dbContext;

        public PedidoController(LojaDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        [Route("adicionarPedido")]
        public IActionResult AdicionarPedido(Pedido novoPedido)
        {
            try
            {
                // Adicionar o novo pedido ao contexto do banco de dados
                _dbContext.Pedidos.Add(novoPedido);
                _dbContext.SaveChanges();
                return CreatedAtAction(nameof(BuscarPedidoPorId), new { id = novoPedido.Id }, novoPedido);
            }
            catch (Exception e)
            {
                return BadRequest("Erro ao adicionar o pedido: " + e.Message);
            }
        }

        [HttpPost]
        [Route("adicionarBrinde")]
        public IActionResult AdicionarBrinde(Brinde brinde)
        {
            if (brinde == null)
            {
                return BadRequest("Não há pedido em andamento.");
            }

            if (PedidoController.pedido != null)
            {
                PedidoController.pedido.AdicionarBrinde(brinde);
                return Ok("Brinde Adicionado com sucesso");
            }
             return BadRequest("Não há pedido em andamento.");
        }


        [HttpGet]
        [Route("buscarPorId/{id}")]
        public IActionResult BuscarPedidoPorId(int id)
        {
            var pedido = _dbContext.Pedidos.FirstOrDefault(p => p.Id == id);
            if (pedido == null)
            {
                return NotFound("Pedido não encontrado.");
            }
            return Ok(pedido);
        }

        [HttpDelete]
        [Route("cancelarPedido/{id}")]
        public IActionResult CancelarPedido(int Id)
        {
            var pedidoCancelar = _dbContext.Pedidos.FirstOrDefault(p => p.Id == Id);
            if (pedidoCancelar != null)
            {
                _dbContext.Pedidos.Remove(pedidoCancelar);
                _dbContext.SaveChanges();
                return Ok("Pedido cancelado com sucesso.");
            }

            return NotFound("Pedido não encontrado.");
        }

        [HttpGet]
        [Route("getPedidos")]
        public IActionResult GetPedidos()
        {
            var pedidos = _dbContext.Pedidos.ToList();
            return Ok(pedidos);
        }
        
        private static Pedido pedido;

        public static void IniciarNovoPedido()
        {
            pedido = new Pedido();
        }
    }
}
