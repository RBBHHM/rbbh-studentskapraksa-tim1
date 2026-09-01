using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RBBH.ConnectedParties.DL.Entities.RelatedPersons;

public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);

        builder.Property(e => e.Residency)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(e => e.RelationshipType)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(30);

        builder.Property(e => e.JMBG).HasMaxLength(13);
        builder.Property(e => e.PassportNumber).HasMaxLength(50);
        builder.Property(e => e.FBAId).HasMaxLength(50);

        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ModifiedBy).HasMaxLength(100);

        builder.HasIndex(e => e.RelatedPersonId);
        builder.HasIndex(e => e.JMBG);
        builder.HasIndex(e => e.ParentFamilyMemberId);

        // Veza prema matičnom licu
        builder.HasOne(e => e.RelatedPerson)
               .WithMany(rp => rp.FamilyMembers)
               .HasForeignKey(e => e.RelatedPersonId)
               .OnDelete(DeleteBehavior.Restrict);

        // Samoreferentna veza za hijerarhijski prikaz porodičnog stabla.
        // Restrict da se izbjegnu višestruke kaskadne putanje (RelatedPerson + self).
        builder.HasOne(e => e.ParentFamilyMember)
               .WithMany(e => e.ChildFamilyMembers)
               .HasForeignKey(e => e.ParentFamilyMemberId)
               .OnDelete(DeleteBehavior.Restrict);

        // Soft delete filter — nikad fizički DELETE
        builder.HasQueryFilter(e => e.IsActive);
    }
}
