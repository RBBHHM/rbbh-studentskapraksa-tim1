using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261125113000_UseNumericAuditIdsAndRemoveIpAddress")]
public sealed class UseNumericAuditIdsAndRemoveIpAddress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>("NumericId", "AuditLogs", type: "bigint", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1");
        migrationBuilder.DropPrimaryKey("PK_AuditLogs", "AuditLogs");
        migrationBuilder.DropColumn("Id", "AuditLogs");
        migrationBuilder.RenameColumn("NumericId", "AuditLogs", "Id");
        migrationBuilder.AddPrimaryKey("PK_AuditLogs", "AuditLogs", "Id");
        migrationBuilder.DropColumn("IpAddress", "AuditLogs");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>("LegacyId", "AuditLogs", type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()");
        migrationBuilder.DropPrimaryKey("PK_AuditLogs", "AuditLogs");
        migrationBuilder.DropColumn("Id", "AuditLogs");
        migrationBuilder.RenameColumn("LegacyId", "AuditLogs", "Id");
        migrationBuilder.AddPrimaryKey("PK_AuditLogs", "AuditLogs", "Id");
        migrationBuilder.AddColumn<string>("IpAddress", "AuditLogs", type: "nvarchar(50)", maxLength: 50, nullable: true);
    }
}
