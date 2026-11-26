using Microsoft.EntityFrameworkCore.Migrations;

namespace RBBH.ConnectedParties.Api.Migrations;

public partial class UseNumericRegistryKeys : Migration
{
    private static readonly (string Table, string AuditName)[] Roots =
    [
        ("AppUsers", "AppUser"), ("Roles", "Role"), ("Permissions", "Permission"),
        ("UserRoles", "UserRole"), ("RoleRequests", "RoleRequest"), ("Notifications", "Notification"),
        ("UnlockRequests", "PeriodLock"), ("LegalEntities", "LegalEntity"), ("RelatedPersons", "RelatedPerson"),
        ("FamilyMembers", "FamilyMember"), ("ClientLimits", "ClientLimit"), ("Reports", "Report")
    ];

    private sealed record Link(string Table, string Column, string Principal, bool Nullable = false,
        ReferentialAction? Delete = ReferentialAction.Restrict);

    private static readonly Link[] Links =
    [
        new("UserRoles", "UserId", "AppUsers"),
        new("UserRoles", "RoleId", "Roles"),
        new("RequestedRole", "RequestId", "RoleRequests"),
        new("RequestedRole", "RoleId", "Roles"),
        new("RolePermissions", "RoleId", "Roles", Delete: ReferentialAction.Cascade),
        new("RolePermissions", "PermissionId", "Permissions", Delete: ReferentialAction.Cascade),
        new("NotificationDeliveries", "NotificationId", "Notifications"),
        new("UnlockRequests", "ParentRequestId", "UnlockRequests", Nullable: true, Delete: null),
        new("LegalEntityVerificationOrigins", "LegalEntityId", "LegalEntities", Delete: ReferentialAction.Cascade),
        new("Limits", "LegalEntityId", "LegalEntities"),
        new("ClientLimits", "LegalEntityId", "LegalEntities"),
        new("RelatedPersons", "RelatedToPersonId", "RelatedPersons", Nullable: true),
        new("FamilyMembers", "ParentFamilyMemberId", "FamilyMembers", Nullable: true),
        new("FamilyMembers", "RelatedPersonId", "RelatedPersons")
    ];

    private sealed record Index(string Table, string[] Columns, bool Unique = false, string? Filter = null)
    {
        internal string Name => $"IX_{Table}_{string.Join('_', Columns)}";
    }

    private static readonly Index[] Indexes =
    [
        new("UserRoles", ["RoleId"]), new("UserRoles", ["UserId", "RoleId"], true),
        new("RequestedRole", ["RoleId"]), new("RequestedRole", ["RequestId", "RoleId"], true),
        new("RolePermissions", ["PermissionId"]), new("RolePermissions", ["RoleId", "PermissionId"], true),
        new("NotificationDeliveries", ["NotificationId"], true),
        new("UnlockRequests", ["ParentRequestId"], true, "[ParentRequestId] IS NOT NULL"),
        new("Limits", ["LegalEntityId"]), new("ClientLimits", ["LegalEntityId"], true),
        new("RelatedPersons", ["RelatedToPersonId"]), new("FamilyMembers", ["ParentFamilyMemberId"]),
        new("FamilyMembers", ["RelatedPersonId"])
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var (table, auditName) in Roots)
        {
            migrationBuilder.AddColumn<long>("NewId", table, type: "bigint", nullable: true);
            Execute(migrationBuilder, $"""
                WITH numbered AS (SELECT NewId, ROW_NUMBER() OVER (ORDER BY Id) AS Number FROM dbo.[{table}])
                UPDATE numbered SET NewId = Number;

                UPDATE audit SET RecordId = CONVERT(nvarchar(20), record.NewId)
                FROM dbo.AuditLogs AS audit
                INNER JOIN dbo.[{table}] AS record ON record.Id = TRY_CONVERT(uniqueidentifier, audit.RecordId)
                WHERE audit.TableName = N'{auditName}' AND LEN(audit.RecordId) = 36;

                DECLARE @next bigint = (SELECT ISNULL(MAX(NewId), 0) + 1 FROM dbo.[{table}]);
                DECLARE @sequence nvarchar(200) = N'CREATE SEQUENCE dbo.[{table}IdSequence] AS bigint START WITH '
                    + CONVERT(nvarchar(20), @next) + N' INCREMENT BY 1';
                EXEC sys.sp_executesql @sequence;
                """);
        }

        Execute(migrationBuilder, """
            UPDATE notice
            SET ParametersJson = JSON_MODIFY(notice.ParametersJson, '$.requestId', request.NewId),
                Route = CASE WHEN CHARINDEX('?requestId=', notice.Route) > 0
                             THEN LEFT(notice.Route, CHARINDEX('?requestId=', notice.Route) + 10)
                                  + CONVERT(nvarchar(20), request.NewId)
                             ELSE notice.Route END
            FROM dbo.Notifications AS notice
            INNER JOIN dbo.RoleRequests AS request ON request.Id = TRY_CONVERT(uniqueidentifier,
                JSON_VALUE(CASE WHEN ISJSON(notice.ParametersJson) = 1 THEN notice.ParametersJson ELSE N'{}' END, '$.requestId'))
            WHERE notice.EventKey LIKE N'role-request:%';
            """);

        foreach (var link in Links)
        {
            migrationBuilder.AddColumn<long>("New" + link.Column, link.Table, type: "bigint", nullable: true);
            Execute(migrationBuilder, $"""
                UPDATE child SET [New{link.Column}] = parent.NewId
                FROM dbo.[{link.Table}] AS child
                INNER JOIN dbo.[{link.Principal}] AS parent ON parent.Id = child.[{link.Column}];

                IF EXISTS (SELECT 1 FROM dbo.[{link.Table}] WHERE [{link.Column}] IS NOT NULL AND [New{link.Column}] IS NULL)
                    THROW 51000, 'Numeric key migration found a missing parent in {link.Table}.{link.Column}.', 1;
                """);
        }

        foreach (var link in Links.Where(link => link.Delete.HasValue))
            migrationBuilder.DropForeignKey(ForeignKeyName(link), link.Table);
        foreach (var index in Indexes)
            migrationBuilder.DropIndex(index.Name, index.Table);
        foreach (var (table, _) in Roots)
            migrationBuilder.DropPrimaryKey($"PK_{table}", table);
        migrationBuilder.DropPrimaryKey("PK_LegalEntityVerificationOrigins", "LegalEntityVerificationOrigins");

        foreach (var (table, _) in Roots)
        {
            ReplaceColumn(migrationBuilder, table, "Id", nullable: false);
            migrationBuilder.AddPrimaryKey($"PK_{table}", table, "Id");
        }
        foreach (var link in Links)
            ReplaceColumn(migrationBuilder, link.Table, link.Column, link.Nullable);
        migrationBuilder.AddPrimaryKey("PK_LegalEntityVerificationOrigins", "LegalEntityVerificationOrigins", "LegalEntityId");

        foreach (var index in Indexes)
            migrationBuilder.CreateIndex(index.Name, index.Table, index.Columns, unique: index.Unique, filter: index.Filter);
        foreach (var link in Links.Where(link => link.Delete.HasValue))
            migrationBuilder.AddForeignKey(ForeignKeyName(link), link.Table, link.Column, link.Principal, principalColumn: "Id",
                onDelete: link.Delete!.Value);

        migrationBuilder.Sql("""
            EXEC sys.sp_refreshview N'dbo.RequestedRoleDetails';
            EXEC sys.sp_refreshview N'dbo.RolePermissionDetails';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Restore a database backup to recover the original GUID keys.");

    private static string ForeignKeyName(Link link) => $"FK_{link.Table}_{link.Principal}_{link.Column}";

    private static void ReplaceColumn(MigrationBuilder migrationBuilder, string table, string column, bool nullable)
    {
        migrationBuilder.DropColumn(column, table);
        migrationBuilder.RenameColumn("New" + column, table, column);
        if (!nullable)
            Execute(migrationBuilder, $"ALTER TABLE dbo.[{table}] ALTER COLUMN [{column}] bigint NOT NULL;");
    }

    // Resolve schema changes per batch, including when EF generates an idempotent script.
    private static void Execute(MigrationBuilder migrationBuilder, string sql) =>
        migrationBuilder.Sql("EXEC(N'" + sql.Replace("'", "''", StringComparison.Ordinal) + "')");
}
