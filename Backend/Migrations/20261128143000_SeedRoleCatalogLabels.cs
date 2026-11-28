using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261128143000_SeedRoleCatalogLabels")]
public sealed class SeedRoleCatalogLabels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DECLARE @packs bigint = (SELECT ID FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'LANGUAGE_PACKS');
        IF @packs IS NULL RETURN;

        DECLARE @labels TABLE (
            LanguageCode nvarchar(10) COLLATE Latin1_General_100_CI_AS,
            ResourceKey nvarchar(200) COLLATE Latin1_General_100_CI_AS,
            PreviousLabel nvarchar(4000), Label nvarchar(4000));
        INSERT @labels VALUES
            (N'BS', N'users.role.fl', N'FL', N'Fizička lica'),
            (N'BS', N'users.role.pl', N'PL', N'Pravna lica'),
            (N'BS', N'users.role.gost', N'Gost', N'Gost'),
            (N'BS', N'rolecatalog.delete-blocked', NULL, N'Brisanje je nedostupno jer postoje dodjele korisnicima ili zahtjevi za ovu rolu.'),
            (N'BS', N'rolecatalog.inactive-retained', NULL, N'Deaktivirana rola ostaje u katalogu zbog historije dodjela ili zahtjeva.'),
            (N'EN', N'users.role.fl', N'FL', N'Natural persons'),
            (N'EN', N'users.role.pl', N'PL', N'Legal entities'),
            (N'EN', N'users.role.gost', N'Gost', N'Guest'),
            (N'EN', N'rolecatalog.delete-blocked', NULL, N'This role cannot be deleted because it has user assignments or role requests.'),
            (N'EN', N'rolecatalog.inactive-retained', NULL, N'This inactive role is kept in the catalog for assignment or request history.'),
            (N'DE', N'users.role.fl', N'FL', N'Natürliche Personen'),
            (N'DE', N'users.role.pl', N'PL', N'Juristische Personen'),
            (N'DE', N'users.role.gost', N'Gost', N'Gast'),
            (N'DE', N'rolecatalog.delete-blocked', NULL, N'Diese Rolle kann wegen vorhandener Zuweisungen oder Rollenanträge nicht gelöscht werden.'),
            (N'DE', N'rolecatalog.inactive-retained', NULL, N'Die deaktivierte Rolle bleibt für die Historie der Zuweisungen oder Anträge im Katalog.');

        UPDATE entry SET ID16 = seed.Label, MODIFIED_AT = SYSUTCDATETIME(), MODIFIED_BY = 'migration'
        FROM dbo.CODELIST_VALUES AS entry
        INNER JOIN @labels AS seed ON entry.ID12 = seed.LanguageCode AND entry.ID13 = seed.ResourceKey
        WHERE entry.DEFINITION_ID = @packs AND entry.ID16 = seed.PreviousLabel AND entry.ID16 <> seed.Label;

        INSERT dbo.CODELIST_VALUES (DEFINITION_ID, ID1, ID12, ID13, ID14, ID15, ID16, ID17, CREATED_BY, CREATED_AT)
        SELECT @packs, 1, seed.LanguageCode, seed.ResourceKey, N'Roles', N'', seed.Label, N'', 'migration', SYSUTCDATETIME()
        FROM @labels AS seed
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.CODELIST_VALUES AS entry
            WHERE entry.DEFINITION_ID = @packs AND entry.ID12 = seed.LanguageCode AND entry.ID13 = seed.ResourceKey);

        UPDATE dbo.CODELIST_DEFINITIONS SET MODIFIED_AT = SYSUTCDATETIME() WHERE ID = @packs;
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve labels that administrators may have edited after the upgrade.
    }
}
