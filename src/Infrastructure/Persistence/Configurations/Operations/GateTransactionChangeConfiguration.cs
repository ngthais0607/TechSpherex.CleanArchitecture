using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnpDepotManagement.Domain.Entities.Operations;

namespace SnpDepotManagement.Infrastructure.Persistence.Configurations.Operations;

public sealed class GateTransactionChangeConfiguration : IEntityTypeConfiguration<GateTransactionChange>
{
    public void Configure(EntityTypeBuilder<GateTransactionChange> builder)
    {
        builder.ToTable("gate_transaction_changes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FieldName).HasMaxLength(50).IsRequired();
        builder.Property(c => c.OldValue).HasMaxLength(200);
        builder.Property(c => c.NewValue).HasMaxLength(200);
        builder.Property(c => c.ChangedBy).HasMaxLength(100);

        builder.HasIndex(c => c.TransactionId);
    }
}
