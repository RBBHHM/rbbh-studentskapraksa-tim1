using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261127071000_SeedCodebookDeletionConfirmation")]
public sealed class SeedCodebookDeletionConfirmation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @packs bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LANGUAGE_PACKS');

            INSERT dbo.CODELIST_VALUES (DEFINITION_ID, ID1, ID12, ID13, ID14, ID15, ID16, ID17, CREATED_BY, CREATED_AT)
            SELECT @packs, 1, seed.LanguageCode, seed.ResourceKey, N'Codebooks', N'', seed.Label, N'', 'migration', SYSUTCDATETIME()
            FROM (VALUES
                (N'BS', N'codes.delete-category-permanently', N'Šifrarnik i sve njegove vrijednosti bit će trajno obrisani.'),
                (N'EN', N'codes.delete-category-permanently', N'The codebook and all its values will be permanently deleted.'),
                (N'DE', N'codes.delete-category-permanently', N'Die Schlüsselliste und alle ihre Werte werden dauerhaft gelöscht.')
            ) AS seed(LanguageCode, ResourceKey, Label)
            WHERE @packs IS NOT NULL AND NOT EXISTS (
                SELECT 1 FROM dbo.CODELIST_VALUES existing
                WHERE existing.DEFINITION_ID = @packs AND existing.ID12 = seed.LanguageCode
                    AND existing.ID13 = seed.ResourceKey
            );

            IF @@ROWCOUNT > 0
                UPDATE dbo.CODELIST_DEFINITIONS SET MODIFIED_AT = SYSUTCDATETIME() WHERE ID = @packs;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve translations because administrators may have edited them after the upgrade.
    }
}
