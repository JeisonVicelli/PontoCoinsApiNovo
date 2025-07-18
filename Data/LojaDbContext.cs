using ProjetoPontos.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjetoPontos.Data
{
  public class LojaDbContext : DbContext
  {
    public LojaDbContext(DbContextOptions<LojaDbContext> options) : base(options)
    {
    }
    public DbSet<Cliente>? Clientes { get; set; }
    public DbSet<Brinde>? Brindes { get; set; }
    public DbSet<Pedido>? Pedidos { get; set; }
    public DbSet<Login>? Logins { get; set; }
    public DbSet<Usuario>? Usuarios { get; set; }
  }
}
