using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Migrations
{
    /// <inheritdoc />
    public partial class AddImmutableCapitalHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CapitalRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveDate = table.Column<DateTime>(type: "date", nullable: false),
                    RegulatoryCapital = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CoreCapital = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SupplementaryCapital = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActorSubject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapitalRevisions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapitalRevisions_EffectiveDate_Revision",
                table: "CapitalRevisions",
                columns: new[] { "EffectiveDate", "Revision" });

            migrationBuilder.CreateIndex(
                name: "IX_CapitalRevisions_Revision",
                table: "CapitalRevisions",
                column: "Revision",
                unique: true);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_CapitalRevisions_Immutable ON CapitalRevisions AFTER UPDATE, DELETE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM deleted)
                        THROW 51008, 'Capital revisions are append-only; record a new revision.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapitalRevisions");
        }
    }
}
