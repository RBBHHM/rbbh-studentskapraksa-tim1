using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261128140000_SeedPeriodLockPermissions")]
public sealed class SeedPeriodLockPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        IF NOT EXISTS (SELECT 1 FROM dbo.PERMISSIONS WHERE CODE = 'PERIOD_LOCK_VIEW')
            INSERT dbo.PERMISSIONS (ID, IS_ACTIVE, MODULE, CODE)
            VALUES (NEXT VALUE FOR dbo.PermissionsIdSequence, 1, 'PERIOD_LOCK', 'PERIOD_LOCK_VIEW');

        IF NOT EXISTS (SELECT 1 FROM dbo.PERMISSIONS WHERE CODE = 'PERIOD_LOCK_MANAGE')
            INSERT dbo.PERMISSIONS (ID, IS_ACTIVE, MODULE, CODE)
            VALUES (NEXT VALUE FOR dbo.PermissionsIdSequence, 1, 'PERIOD_LOCK', 'PERIOD_LOCK_MANAGE');

        INSERT dbo.ROLE_PERMISSIONS (ROLE_ID, PERMISSION_ID)
        SELECT roleRow.ID, permissionRow.ID
        FROM dbo.ROLES AS roleRow
        CROSS JOIN dbo.PERMISSIONS AS permissionRow
        WHERE roleRow.NAME IN ('admin', 'kapital')
          AND permissionRow.CODE IN ('PERIOD_LOCK_VIEW', 'PERIOD_LOCK_MANAGE')
          AND NOT EXISTS (SELECT 1 FROM dbo.ROLE_PERMISSIONS AS grantRow
                          WHERE grantRow.ROLE_ID = roleRow.ID AND grantRow.PERMISSION_ID = permissionRow.ID);
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DELETE FROM dbo.PERMISSIONS WHERE CODE IN ('PERIOD_LOCK_VIEW', 'PERIOD_LOCK_MANAGE');
        """);
}
