// File: src/RBBH.ConnectedParties/DL/Entities/Report/ReportConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RBBH.ConnectedParties.DL.Entities.Report;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.ReportType);
        builder.HasIndex(e => e.ReportDate);
        builder.HasIndex(e => new { e.ReportType, e.ReportDate });

        builder.Property(e => e.ReportType).HasMaxLength(10).IsRequired();
        builder.Property(e => e.TotalExposure).HasPrecision(18, 2);
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();

        builder.HasQueryFilter(e => e.IsActive);
    }
}