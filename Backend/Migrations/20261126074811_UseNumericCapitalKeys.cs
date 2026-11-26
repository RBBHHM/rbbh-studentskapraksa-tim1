using Microsoft.EntityFrameworkCore.Migrations;

namespace RBBH.ConnectedParties.Api.Migrations;

public partial class UseNumericCapitalKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>("NumericId", "Capitals", type: "bigint", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1");
        migrationBuilder.DropPrimaryKey("PK_Capitals", "Capitals");
        migrationBuilder.DropColumn("Id", "Capitals");
        migrationBuilder.RenameColumn("NumericId", "Capitals", "Id");
        migrationBuilder.AddPrimaryKey("PK_Capitals", "Capitals", "Id");

        // Keep the original identity values and ordering for all existing capital revisions.
        migrationBuilder.DropIndex("IX_CapitalRevisions_EffectiveDate_Revision", "CapitalRevisions");
        migrationBuilder.DropIndex("IX_CapitalRevisions_Revision", "CapitalRevisions");
        migrationBuilder.DropPrimaryKey("PK_CapitalRevisions", "CapitalRevisions");
        migrationBuilder.DropColumn("Id", "CapitalRevisions");
        migrationBuilder.RenameColumn("Revision", "CapitalRevisions", "Id");
        migrationBuilder.AddPrimaryKey("PK_CapitalRevisions", "CapitalRevisions", "Id");
        migrationBuilder.CreateIndex("IX_CapitalRevisions_EffectiveDate_Id", "CapitalRevisions", new[] { "EffectiveDate", "Id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Restore a database backup to recover the original GUID keys.");
}
