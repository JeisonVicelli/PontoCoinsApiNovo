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
    public DbSet<HistoricoMovimentacao>? Historico { get; set; }
    public DbSet<CashbackLote>? CashbackLotes { get; set; }
    public DbSet<Pedido>? Pedidos { get; set; }
    public DbSet<Login>? Logins { get; set; }
    public DbSet<Usuario>? Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<CashbackLote>(entity =>
      {
        entity.ToTable("cashbacklote");
      });

      modelBuilder.Entity<Usuario>(entity =>
      {
        entity.HasIndex(u => u.UserName).IsUnique();
      });
    }
  }
}
