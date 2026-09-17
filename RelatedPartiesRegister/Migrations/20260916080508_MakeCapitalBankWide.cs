using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RBBH.ConnectedParties.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeCapitalBankWide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Capital",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OsnovniKapital = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RegulatorniKapital = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DopunskiKapital = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DatumKapitala = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capital", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Capital_DatumKapitala",
                table: "Capital",
                column: "DatumKapitala",
                unique: true);

            // Legacy capital was repeated per limit. Retain one most-recently modified
            // value for each effective date; the source limit rows remain untouched.
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM dbo.Limiti WHERE DatumKapitala IS NULL
                    AND (OsnovniKapital <> 0 OR RegulatorniKapital <> 0 OR DopunskiKapital <> 0))
                    THROW 51002, 'Historijski kapital bez datuma; unesite potvrđeni datum prije migracije.', 1;
                IF EXISTS (
                    SELECT 1 FROM dbo.Limiti WHERE DatumKapitala IS NOT NULL
                    GROUP BY CAST(DatumKapitala AS date)
                    HAVING COUNT(DISTINCT CONCAT(OsnovniKapital, '|', RegulatorniKapital, '|', DopunskiKapital)) > 1
                ) THROW 51001, 'Različiti historijski iznosi kapitala za isti datum; uskladite ih prije migracije.', 1;
                ;WITH Ranked AS (
                    SELECT OsnovniKapital, RegulatorniKapital, DopunskiKapital,
                           CAST(DatumKapitala AS date) AS EffectiveDate,
                           CreatedAt, CreatedBy, ModifiedAt, ModifiedBy,
                           ROW_NUMBER() OVER (PARTITION BY CAST(DatumKapitala AS date)
                               ORDER BY COALESCE(ModifiedAt, CreatedAt) DESC, Id DESC) AS rn
                    FROM dbo.Limiti WHERE DatumKapitala IS NOT NULL
                )
                INSERT INTO dbo.Capital (OsnovniKapital, RegulatorniKapital, DopunskiKapital,
                    DatumKapitala, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
                SELECT OsnovniKapital, RegulatorniKapital, DopunskiKapital,
                    EffectiveDate, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                FROM Ranked WHERE rn = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Capital");
        }
    }
}
