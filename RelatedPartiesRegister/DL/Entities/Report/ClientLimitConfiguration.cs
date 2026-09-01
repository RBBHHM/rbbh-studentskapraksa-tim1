// File: src/RBBH.ConnectedParties/DL/Entities/Report/ClientLimitConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RBBH.ConnectedParties.DL.Entities.Report;

public class ClientLimitConfiguration : IEntityTypeConfiguration<ClientLimit>
{
    public void Configure(EntityTypeBuilder<ClientLimit> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.LegalEntityId).IsUnique();
        builder.HasIndex(e => e.IsLimitBreached);

        builder.Property(e => e.RegulatoryCapital).HasPrecision(18, 2);
        builder.Property(e => e.CoreCapital).HasPrecision(18, 2);
        builder.Property(e => e.ExposureLimit).HasPrecision(18, 2);
        builder.Property(e => e.CurrentExposure).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ModifiedBy).HasMaxLength(100);

        builder.HasOne(e => e.LegalEntity)
               .WithMany()
               .HasForeignKey(e => e.LegalEntityId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => e.IsActive);
    }
}