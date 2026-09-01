// File: src/RBBH.ConnectedParties/DL/Entities/LegalEntity/LegalEntityConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RBBH.ConnectedParties.DL.Entities.LegalEntity;

public class LegalEntityConfiguration : IEntityTypeConfiguration<LegalEntity>
{
    public void Configure(EntityTypeBuilder<LegalEntity> builder)
    {
        builder.HasKey(e => e.Id);

        // Porezni broj mora biti jedinstven kada je postavljen (rezidenti)
        builder.HasIndex(e => e.TaxNumber)
               .IsUnique()
               .HasFilter("\"TaxNumber\" IS NOT NULL");

        // FBA ID mora biti jedinstven kada je postavljen (nerezidenti)
        builder.HasIndex(e => e.FbaId)
               .IsUnique()
               .HasFilter("\"FbaId\" IS NOT NULL");

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.IsResident);

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.TaxNumber).HasMaxLength(13);
        builder.Property(e => e.FbaId).HasMaxLength(50);
        builder.Property(e => e.GccNumber).HasMaxLength(100);
        builder.Property(e => e.GccName).HasMaxLength(200);
        builder.Property(e => e.BasisOfConnection).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ConnectionDescription).HasMaxLength(500);
        builder.Property(e => e.Status).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(e => e.ModifiedBy).HasMaxLength(100);

        // Soft delete filter — nikad fizički DELETE
        builder.HasQueryFilter(e => e.IsActive);
    }
}