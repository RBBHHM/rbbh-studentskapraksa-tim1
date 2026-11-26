using Microsoft.EntityFrameworkCore.Migrations;

namespace RBBH.ConnectedParties.Api.Migrations;

public partial class RefineRegistryColumnsAndNumericKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ReplaceGuidKey(migrationBuilder, "PeriodLocks");
        ReplaceGuidKey(migrationBuilder, "NotificationDeliveries");
        ReplaceGuidKey(migrationBuilder, "CodeListDefinitions");

        AddNumericKey(migrationBuilder, "RequestedRole", "RequestId", "RoleId");
        AddNumericKey(migrationBuilder, "RolePermissions", "RoleId", "PermissionId");

        migrationBuilder.AddColumn<string>("VerificationOriginFingerprint", "RelatedPersons",
            type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>("Fingerprint", "LegalEntityVerificationOrigins",
            type: "nvarchar(64)", maxLength: 64, nullable: true);

        // HASHBYTES over nvarchar uses the same UTF-16LE encoding as Encoding.Unicode.
        migrationBuilder.Sql("""
            EXEC(N'UPDATE dbo.RelatedPersons
                SET VerificationOriginFingerprint = CONVERT(varchar(64), HASHBYTES(''SHA2_256'', CreatorSubject), 2)
                WHERE CreatorSubject IS NOT NULL AND DATALENGTH(CreatorSubject) > 0');

            EXEC(N'UPDATE dbo.LegalEntityVerificationOrigins
                SET Fingerprint = CONVERT(varchar(64), HASHBYTES(''SHA2_256'', Subject), 2)');
            """);
        migrationBuilder.AlterColumn<string>("Fingerprint", "LegalEntityVerificationOrigins",
            type: "nvarchar(64)", maxLength: 64, nullable: false,
            oldClrType: typeof(string), oldType: "nvarchar(64)", oldMaxLength: 64, oldNullable: true);
        migrationBuilder.DropColumn("Subject", "LegalEntityVerificationOrigins");
        migrationBuilder.DropColumn("CreatorSubject", "RelatedPersons");
        migrationBuilder.DropColumn("RelationDescription", "RelatedPersons");

        // The views resolve current names without duplicating data across tables.
        migrationBuilder.Sql("""
            EXEC(N'CREATE VIEW dbo.RequestedRoleDetails AS
                SELECT item.Id, item.RequestId, item.RoleId, request.Username AS RequestedBy,
                       request.FirstName, request.LastName, role.Name AS RoleName,
                       request.CreatedAt AS RequestedAt, request.Status, request.Note,
                       request.ProcessedBy, request.ProcessedAt, request.DecisionNote
                FROM dbo.RequestedRole AS item
                INNER JOIN dbo.RoleRequests AS request ON request.Id = item.RequestId
                INNER JOIN dbo.Roles AS role ON role.Id = item.RoleId');

            EXEC(N'CREATE VIEW dbo.RolePermissionDetails AS
                SELECT item.Id, item.RoleId, item.PermissionId, role.Name AS RoleName,
                       permission.Code AS PermissionCode, permission.Module
                FROM dbo.RolePermissions AS item
                INNER JOIN dbo.Roles AS role ON role.Id = item.RoleId
                INNER JOIN dbo.Permissions AS permission ON permission.Id = item.PermissionId');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Restore a database backup to recover the removed values and original GUID keys.");

    private static void ReplaceGuidKey(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.AddColumn<long>("NumericId", table, type: "bigint", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1");
        migrationBuilder.DropPrimaryKey($"PK_{table}", table);
        migrationBuilder.DropColumn("Id", table);
        migrationBuilder.RenameColumn("NumericId", table, "Id");
        migrationBuilder.AddPrimaryKey($"PK_{table}", table, "Id");
    }

    private static void AddNumericKey(MigrationBuilder migrationBuilder, string table, string firstKey, string secondKey)
    {
        migrationBuilder.AddColumn<long>("Id", table, type: "bigint", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1");
        migrationBuilder.DropPrimaryKey($"PK_{table}", table);
        migrationBuilder.AddPrimaryKey($"PK_{table}", table, "Id");
        migrationBuilder.CreateIndex($"IX_{table}_{firstKey}_{secondKey}", table, new[] { firstKey, secondKey }, unique: true);
    }
}
