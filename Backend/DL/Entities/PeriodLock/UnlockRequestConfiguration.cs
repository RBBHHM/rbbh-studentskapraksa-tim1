using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RBBH.ConnectedParties.DL.Entities.PeriodLock;

public class UnlockRequestConfiguration : IEntityTypeConfiguration<UnlockRequest>
{
    public void Configure(EntityTypeBuilder<UnlockRequest> builder)
    {
        builder.HasKey(e => e.Id);

        // Pretraga po statusu i periodu
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => new { e.Year, e.Month });

        builder.Property(e => e.RequestedBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.RequestedByEmail).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ProcessedBy).HasMaxLength(100);
        builder.Property(e => e.AdminNote).HasMaxLength(1000);
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ModifiedBy).HasMaxLength(100);

        // Soft delete filter
        builder.HasQueryFilter(e => e.IsActive);
    }
}
