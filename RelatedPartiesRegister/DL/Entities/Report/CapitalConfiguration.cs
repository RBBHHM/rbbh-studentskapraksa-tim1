// File: src/RBBH.ConnectedParties/DL/Entities/Report/CapitalConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RBBH.ConnectedParties.DL.Entities.Report;

public class CapitalConfiguration : IEntityTypeConfiguration<Capital>
{
    public void Configure(EntityTypeBuilder<Capital> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RegulatoryCapital).HasPrecision(18, 2);
        builder.Property(e => e.CoreCapital).HasPrecision(18, 2);
        builder.Property(e => e.SupplementaryCapital).HasPrecision(18, 2);
        builder.Property(e => e.CapitalDate).HasColumnType("date");
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ModifiedBy).HasMaxLength(100);

        builder.HasQueryFilter(e => e.IsActive);
    }
}
