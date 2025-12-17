using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsTransacoes.Domain.Entities;

namespace MsTransacoes.Infra.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log", "transacoes");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id");
        
        builder.Property(x => x.EntityName)
            .HasMaxLength(100)
            .HasColumnName("entity_name")
            .IsRequired();
        
        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id");
        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id");
        
        builder.Property(x => x.Operation)
            .HasMaxLength(10)
            .HasColumnName("operation")
            .IsRequired();
        
        builder.Property(x => x.OldValues)
            .HasColumnType("jsonb")
            .HasColumnName("old_values");
        
        builder.Property(x => x.NewValues)
            .HasColumnType("jsonb")
            .HasColumnName("new_values");
        
        builder.Property(x => x.UserId)
            .HasMaxLength(100)
            .HasColumnName("user_id");
        
        builder.Property(x => x.CorrelationId)
            .HasColumnName("correlation_id");
        
        builder.Property(x => x.SourceService)
            .HasMaxLength(50)
            .HasColumnName("source_service");
        
        builder.Property(x => x.PublishedToQueue)
            .HasColumnName("published_to_queue");
        
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
        
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
