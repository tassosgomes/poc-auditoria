using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsTransacoes.Domain.Entities;

namespace MsTransacoes.Infra.Persistence.Configurations;

public sealed class TransacaoConfiguration : IEntityTypeConfiguration<Transacao>
{
    public void Configure(EntityTypeBuilder<Transacao> builder)
    {
        builder.ToTable("transacoes", schema: "transacoes");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.ContaOrigemId)
            .HasColumnName("conta_origem_id")
            .IsRequired();

        builder.Property(t => t.ContaDestinoId)
            .HasColumnName("conta_destino_id");

        builder.Property(t => t.Tipo)
            .HasColumnName("tipo")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Valor)
            .HasColumnName("valor")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(255);

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(t => t.ProcessadoEm)
            .HasColumnName("processado_em");

        builder.HasIndex(t => t.ContaOrigemId)
            .HasDatabaseName("idx_transacoes_conta_origem");

        builder.HasIndex(t => t.CriadoEm)
            .HasDatabaseName("idx_transacoes_data");
    }
}
