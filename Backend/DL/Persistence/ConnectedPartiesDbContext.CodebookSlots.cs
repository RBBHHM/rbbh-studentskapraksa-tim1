using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;

namespace RBBH.ConnectedParties.DL.Persistence;

public partial class ConnectedPartiesDbContext
{
    private static void ConfigureDefinitionSlots(EntityTypeBuilder<CodeListDefinition> entity)
    {
        entity.Property<string>("Owner").HasColumnName("OWNER").HasMaxLength(100).IsRequired().HasDefaultValue("registry");
        for (var number = 1; number <= 20; number++)
        {
            entity.Property<string>($"ColumnName{number}").HasColumnName($"COLUMN_NAME_{number}").HasMaxLength(128);
            entity.Property<string>($"ColumnDescription{number}").HasColumnName($"COLUMN_DESCRIPTION_{number}").HasMaxLength(4000);
            entity.Property<int?>($"ColumnPosition{number}").HasColumnName($"COLUMN_POSITION_{number}");
        }
    }

    private static void ConfigureCodeListSlots(EntityTypeBuilder<CodeList> entity)
    {
        entity.Property<bool?>("Slot2").HasColumnName("ID2");
        for (var number = 3; number <= 5; number++)
            entity.Property<int?>($"Slot{number}").HasColumnName($"ID{number}");
        for (var number = 6; number <= 8; number++)
            entity.Property<decimal?>($"Slot{number}").HasColumnName($"ID{number}").HasPrecision(number == 8 ? 19 : 18, number == 8 ? 6 : 2);
        for (var number = 9; number <= 11; number++)
            entity.Property<DateTime?>($"Slot{number}").HasColumnName($"ID{number}").HasColumnType(number == 11 ? "datetime2" : "date");
        entity.Property<string>("Slot15").HasColumnName("ID15").HasMaxLength(100);
        for (var number = 17; number <= 18; number++)
            entity.Property<string>($"Slot{number}").HasColumnName($"ID{number}").HasMaxLength(4000);

        entity.OwnsOne(row => row.Localization, slots =>
        {
            slots.WithOwner().HasForeignKey("ID");
            slots.Property<int>("ID").HasColumnName("ID");
            slots.Property(item => item.LanguageCode).HasColumnName("ID12").UseCollation("Latin1_General_100_CI_AS");
            slots.Property(item => item.ResourceKey).HasColumnName("ID13").UseCollation("Latin1_General_100_CI_AS");
            slots.Property(item => item.ContextInfo).HasColumnName("ID14");
            slots.Property(item => item.ResourceLabel).HasColumnName("ID16");
            slots.Property(item => item.ResourceHelper).HasColumnName("ID19");
            slots.Property(item => item.ResourcePlaceholder).HasColumnName("ID20");
            slots.HasIndex(item => new { item.LanguageCode, item.ResourceKey }).IsUnique()
                .HasDatabaseName("IX_CODELIST_VALUES_LANGUAGE_RESOURCE")
                .HasFilter("[CATEGORY] = 'LANGUAGE_PACKS' AND [ID12] IS NOT NULL AND [ID13] IS NOT NULL");
        });
    }
}
