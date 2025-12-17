using Microsoft.EntityFrameworkCore;
using MsTransacoes.Domain.Entities;

namespace MsTransacoes.Infra.Persistence;

public sealed class TransacoesDbContext : DbContext
{
    public TransacoesDbContext(DbContextOptions<TransacoesDbContext> options) : base(options)
    {
    }

    public DbSet<Transacao> Transacoes => Set<Transacao>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransacoesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
