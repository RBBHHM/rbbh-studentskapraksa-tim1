using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RBBH.ConnectedParties.DL.Entities.PeriodLock;

public class PeriodLockConfiguration : IEntityTypeConfiguration<PeriodLock>
{
    public void Configure(EntityTypeBuilder<PeriodLock> builder)
    {
        builder.HasKey(e => e.Id);

        // Jedan zapis po periodu
        builder.HasIndex(e => new { e.Year, e.Month }).IsUnique();

        builder.Property(e => e.LockedBy).HasMaxLength(100);
        builder.Property(e => e.UnlockedBy).HasMaxLength(100);
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ModifiedBy).HasMaxLength(100);

        // Soft delete filter — nikad fizički DELETE
        builder.HasQueryFilter(e => e.IsActive);
    }
}
