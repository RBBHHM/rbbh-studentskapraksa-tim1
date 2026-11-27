using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261127093000_IndexRegistryReadPaths")]
public sealed class IndexRegistryReadPaths : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(name: "IX_APP_USERS_LastNameFirstNameId", table: "APP_USERS",
            columns: ["LAST_NAME", "FIRST_NAME", "ID"]);
        migrationBuilder.CreateIndex(name: "IX_AUDIT_LOGS_TimestampId", table: "AUDIT_LOGS",
            columns: ["TIMESTAMP", "ID"], descending: [true, true]);
        migrationBuilder.CreateIndex(name: "IX_LEGAL_ENTITIES_IsActiveCreatedAtId", table: "LEGAL_ENTITIES",
            columns: ["IS_ACTIVE", "CREATED_AT", "ID"], descending: [false, true, false]);
        migrationBuilder.CreateIndex(name: "IX_RELATED_PERSONS_IsActiveLastNameFirstNameId", table: "RELATED_PERSONS",
            columns: ["IS_ACTIVE", "LAST_NAME", "FIRST_NAME", "ID"]);
        migrationBuilder.CreateIndex(name: "IX_REPORTS_ReportTypeIsActiveReportDateCreatedAtId", table: "REPORTS",
            columns: ["REPORT_TYPE", "IS_ACTIVE", "REPORT_DATE", "CREATED_AT", "ID"], descending: [false, false, true, true, false]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_APP_USERS_LastNameFirstNameId", table: "APP_USERS");
        migrationBuilder.DropIndex(name: "IX_AUDIT_LOGS_TimestampId", table: "AUDIT_LOGS");
        migrationBuilder.DropIndex(name: "IX_LEGAL_ENTITIES_IsActiveCreatedAtId", table: "LEGAL_ENTITIES");
        migrationBuilder.DropIndex(name: "IX_RELATED_PERSONS_IsActiveLastNameFirstNameId", table: "RELATED_PERSONS");
        migrationBuilder.DropIndex(name: "IX_REPORTS_ReportTypeIsActiveReportDateCreatedAtId", table: "REPORTS");
    }
}
