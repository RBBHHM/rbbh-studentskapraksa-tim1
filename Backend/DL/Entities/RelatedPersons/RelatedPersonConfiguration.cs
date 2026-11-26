using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RBBH.ConnectedParties.DL.Entities.RelatedPersons;

public class RelatedPersonConfiguration : IEntityTypeConfiguration<RelatedPerson>
{
    public void Configure(EntityTypeBuilder<RelatedPerson> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);

        builder.Property(e => e.Residency)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(e => e.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(e => e.JMBG).HasMaxLength(13);
        builder.Property(e => e.PassportNumber).HasMaxLength(50).UseCollation("Latin1_General_100_CI_AS");
        builder.Property(e => e.FBAId).HasMaxLength(50).UseCollation("Latin1_General_100_CI_AS");

        builder.Property(e => e.RelationBasis).HasMaxLength(500);
        builder.Property(e => e.SpecialRelationBasis).HasMaxLength(500);

        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ModifiedBy).HasMaxLength(100);

        // Identifikatori su jedinstveni među aktivnim zapisima. Soft-delete omogućava
        // ponovni unos tek nakon deaktivacije starog zapisa.
        builder.HasIndex(e => e.JMBG).IsUnique().HasFilter("[IS_ACTIVE] = 1 AND [JMBG] IS NOT NULL");
        builder.HasIndex(e => e.FBAId).IsUnique().HasFilter("[IS_ACTIVE] = 1 AND [FBA_ID] IS NOT NULL");
        builder.HasIndex(e => e.PassportNumber).IsUnique().HasFilter("[IS_ACTIVE] = 1 AND [PASSPORT_NUMBER] IS NOT NULL");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_RelatedPersons_PassportNumber_Trimmed",
                "DATALENGTH([PASSPORT_NUMBER]) = DATALENGTH(LTRIM(RTRIM([PASSPORT_NUMBER])))");
            table.HasCheckConstraint("CK_RelatedPersons_FBAId_Trimmed",
                "DATALENGTH([FBA_ID]) = DATALENGTH(LTRIM(RTRIM([FBA_ID])))");
        });
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.RelatedToPersonId);

        // Soft delete filter — nikad fizički DELETE
        builder.HasQueryFilter(e => e.IsActive);

        builder.HasMany(e => e.FamilyMembers)
               .WithOne(fm => fm.RelatedPerson)
               .HasForeignKey(fm => fm.RelatedPersonId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RelatedToPerson)
               .WithMany(e => e.RelatedFamilyMembers)
               .HasForeignKey(e => e.RelatedToPersonId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
