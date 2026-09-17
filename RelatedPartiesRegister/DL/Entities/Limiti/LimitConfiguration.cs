using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RBBH.ConnectedParties.DL.Entities.Limiti;

public class LimitConfiguration : IEntityTypeConfiguration<Limit>
{
    public void Configure(EntityTypeBuilder<Limit> builder)
    {
        builder.ToTable("Limiti");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Naziv)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.TipLimita)
               .HasMaxLength(100)
               .IsRequired();

        builder.HasOne(e => e.LegalEntity).WithMany().HasForeignKey(e => e.LegalEntityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.LegalEntityId);

        builder.Property(e => e.CreatedBy)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(e => e.ModifiedBy)
               .HasMaxLength(100);

        builder.HasIndex(e => e.TipLimita);
        builder.HasIndex(e => e.Naziv);
    }
}
