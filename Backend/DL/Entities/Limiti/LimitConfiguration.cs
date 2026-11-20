using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RBBH.ConnectedParties.DL.Entities.Limiti;

public class LimitConfiguration : IEntityTypeConfiguration<Limit>
{
    public void Configure(EntityTypeBuilder<Limit> builder)
    {
        builder.ToTable("Limits");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TipLimita)
               .HasColumnName("LimitType")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.OdobreniLimit).HasColumnName("ApprovedLimit");
        builder.Property(e => e.RokOcekivaneUtilizacije).HasColumnName("ExpectedUtilizationDate");
        builder.Property(e => e.MaksimalnaOcekivanaUtilizacija).HasColumnName("MaximumExpectedUtilization");
        builder.Property(e => e.Komentar).HasColumnName("Comment");

        builder.Property(e => e.CreatedBy)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.ModifiedBy)
               .HasMaxLength(100);

        builder.HasOne(e => e.LegalEntity)
               .WithMany()
               .HasForeignKey(e => e.LegalEntityId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.LegalEntityId);
        builder.HasIndex(e => e.TipLimita);
    }
}
