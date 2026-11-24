using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261124101000_EnsureUniqueUserIdentityAndUpdateRoleRequestCopy")]
public sealed class EnsureUniqueUserIdentityAndUpdateRoleRequestCopy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        EnsureUniqueIdentityIndex(migrationBuilder, "Username", 51026);
        EnsureUniqueIdentityIndex(migrationBuilder, "Email", 51027);

        migrationBuilder.Sql("""
            UPDATE resources
               SET ResourceLabel = changes.NewLabel
            FROM dbo.CodeListLocalization AS resources
            INNER JOIN (VALUES
                (N'BS', N'roles.change-help', N'Trenutne role ostaju aktivne dok administrator ne odobri zahtjev. Nakon odobrenja zamjenjuju ih odabrane role.'),
                (N'EN', N'roles.change-help', N'Your current roles stay active until an administrator approves the request. The selected roles replace them after approval.'),
                (N'DE', N'roles.change-help', N'Ihre aktuellen Rollen bleiben aktiv, bis die Administration den Antrag genehmigt. Danach werden sie durch die ausgewählten Rollen ersetzt.'),
                (N'BS', N'roles.note', N'Napomena (opcionalno)')
            ) AS changes(LanguageCode, ResourceKey, NewLabel)
                ON changes.LanguageCode = resources.LanguageCode AND changes.ResourceKey = resources.ResourceKey
            WHERE resources.Category = 'LanguagePacks';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Both unique indexes are part of the preceding schema and remain required.
        migrationBuilder.Sql("""
            UPDATE resources
               SET ResourceLabel = changes.OldLabel
            FROM dbo.CodeListLocalization AS resources
            INNER JOIN (VALUES
                (N'BS', N'roles.change-help', N'Odabrane role zamijenit će vaše trenutne role. Možete dodati napomenu za administratore.'),
                (N'EN', N'roles.change-help', N'The roles you choose will replace your current roles. You can add a note for the administrators.'),
                (N'DE', N'roles.change-help', N'Die ausgewählten Rollen ersetzen Ihre bisherigen Rollen. Sie können eine Bemerkung für die Administratoren hinzufügen.'),
                (N'BS', N'roles.note', N'Napomena (neobavezno)')
            ) AS changes(LanguageCode, ResourceKey, OldLabel)
                ON changes.LanguageCode = resources.LanguageCode AND changes.ResourceKey = resources.ResourceKey
            WHERE resources.Category = 'LanguagePacks';
            """);
    }

    private static void EnsureUniqueIdentityIndex(MigrationBuilder migrationBuilder, string column, int error)
    {
        migrationBuilder.Sql($"""
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes AS i
                INNER JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                WHERE i.object_id = OBJECT_ID(N'dbo.AppUsers')
                  AND i.name = N'IX_AppUsers_{column}' AND i.is_unique = 1 AND i.has_filter = 0
                  AND i.is_disabled = 0 AND i.ignore_dup_key = 0 AND ic.key_ordinal = 1
                  AND COL_NAME(ic.object_id, ic.column_id) = N'{column}'
                  AND NOT EXISTS (
                      SELECT 1 FROM sys.index_columns AS extra
                      WHERE extra.object_id = i.object_id AND extra.index_id = i.index_id AND extra.key_ordinal > 1
                  )
            )
            BEGIN
                IF EXISTS (SELECT 1 FROM dbo.AppUsers GROUP BY [{column}] HAVING COUNT_BIG(*) > 1)
                    THROW {error}, 'AppUsers contains duplicate {column} values. Reconcile them before applying uniqueness.', 1;
                DROP INDEX IF EXISTS [IX_AppUsers_{column}] ON [dbo].[AppUsers];
                CREATE UNIQUE INDEX [IX_AppUsers_{column}] ON [dbo].[AppUsers] ([{column}]);
            END;
            """);
    }
}
