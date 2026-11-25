using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Api.Migrations;

[DbContext(typeof(ConnectedPartiesDbContext))]
[Migration("20261125100000_RemovePermissionLabel")]
public sealed class RemovePermissionLabel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "Label", table: "Permissions");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>(name: "Label", table: "Permissions",
            type: "nvarchar(250)", maxLength: 250, nullable: false, defaultValue: "");
}
