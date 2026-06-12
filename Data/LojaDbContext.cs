using ProjetoPontos.Models;
using ProjetoPontos.Services;
using Microsoft.EntityFrameworkCore;

namespace ProjetoPontos.Data
{
  public class LojaDbContext : DbContext
  {
    private readonly ITenantProvider _tenant;

    public LojaDbContext(DbContextOptions<LojaDbContext> options, ITenantProvider tenant) : base(options)
    {
      _tenant = tenant;
    }
    public DbSet<Loja>? Lojas { get; set; }
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

      // Filtro global de multi-tenant: cada loja só vê os próprios dados.
      modelBuilder.Entity<Cliente>().HasQueryFilter(e => e.LojaId == _tenant.LojaId);
      modelBuilder.Entity<Usuario>().HasQueryFilter(e => e.LojaId == _tenant.LojaId);
      modelBuilder.Entity<Brinde>().HasQueryFilter(e => e.LojaId == _tenant.LojaId);
      modelBuilder.Entity<Pedido>().HasQueryFilter(e => e.LojaId == _tenant.LojaId);
      modelBuilder.Entity<HistoricoMovimentacao>().HasQueryFilter(e => e.LojaId == _tenant.LojaId);
      modelBuilder.Entity<CashbackLote>().HasQueryFilter(e => e.LojaId == _tenant.LojaId);

      // Impede excluir uma Loja enquanto houver dados vinculados a ela.
      modelBuilder.Entity<Cliente>().HasOne(e => e.Loja).WithMany().HasForeignKey(e => e.LojaId).OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Usuario>().HasOne(e => e.Loja).WithMany().HasForeignKey(e => e.LojaId).OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Brinde>().HasOne(e => e.Loja).WithMany().HasForeignKey(e => e.LojaId).OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<Pedido>().HasOne(e => e.Loja).WithMany().HasForeignKey(e => e.LojaId).OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<HistoricoMovimentacao>().HasOne(e => e.Loja).WithMany().HasForeignKey(e => e.LojaId).OnDelete(DeleteBehavior.Restrict);
      modelBuilder.Entity<CashbackLote>().HasOne(e => e.Loja).WithMany().HasForeignKey(e => e.LojaId).OnDelete(DeleteBehavior.Restrict);
    }

    public override int SaveChanges()
    {
      AplicarLojaId();
      return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
      AplicarLojaId();
      return base.SaveChangesAsync(cancellationToken);
    }

    private void AplicarLojaId()
    {
      foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
      {
        if (entry.State == EntityState.Added)
        {
          entry.Entity.LojaId = _tenant.LojaId;
        }
      }
    }
  }
}
