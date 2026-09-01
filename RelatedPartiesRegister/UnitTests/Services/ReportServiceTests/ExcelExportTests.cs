using System.Diagnostics;
using ClosedXML.Excel;
using FluentAssertions;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using LegalEntityEntity = RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity;
using Microsoft.Extensions.Logging;
using Moq;
using UnitTests.Mocks.DB;

namespace UnitTests.Services.ReportServiceTests
{
    /// <summary>
    /// Testiranje Excel izvoza.
    /// Ovaj fajl:
    ///   - Export po matičnom broju / poreznom broju / FBA ID-u (ExportClientByIdAsync)
    ///   - Export svih klijenata (ExportAllClientsWithLimitsAsync)
    ///   - Tačnost podataka u Excel fajlu (čitanje generisanog .xlsx preko ClosedXML)
    ///   - Ispravnost filtera (identifikator, case-insensitive, samo aktivni, match po nazivu)
    ///   - Performanse exporta sa većim brojem zapisa
    ///
    /// Kolone u Excelu (header red = 3, podaci od reda 4):
    ///   1 Naziv | 2 Tip limita | 3 Iznos limita | 4 Utilizacija | 5 Korigovani limit |
    ///   6 Raspoloživi limit | 7 Regulatorni kapital | 8 Osnovni kapital | 9 Kreirao
    /// </summary>
    public class ExcelExportTests
    {
        private const int HeaderRow = 3;
        private const int FirstDataRow = 4;

        private static ReportService NewService(out ConnectedPartiesDbContext ctx)
        {
            ctx = InMemoryContextFactory.Create();
            var logger = new Mock<ILogger<ReportService>>();
            return new ReportService(ctx, logger.Object);
        }

        private static LegalEntityEntity NewEntity(string name, bool resident = true,
            string? tax = null, string? fba = null, string? matbroj = null,
            string? maticni = null, bool active = true) => new()
        {
            Name = name,
            IsResident = resident,
            TaxNumber = tax,
            FbaId = fba,
            Matbroj = matbroj,
            MaticniBroj = maticni,
            BasisOfConnection = "Vlasništvo",
            Status = "Draft",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            IsActive = active
        };

        private static Limit NewLimit(string naziv, decimal iznos = 1000m, decimal util = 200m,
            decimal raspolozivi = 100m, string tip = "Izloženost") => new()
        {
            Naziv = naziv,
            TipLimita = tip,
            IznosLimita = iznos,
            Utilizacija = util,
            RaspoloziviLimit = raspolozivi,
            RegulatorniKapital = 5000m,
            OsnovniKapital = 4000m,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        };

        private static IXLWorksheet OpenSheet(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var wb = new XLWorkbook(ms);
            return wb.Worksheet(1);
        }

        private static int DataRowCount(IXLWorksheet ws)
        {
            var last = ws.LastRowUsed();
            return last is null ? 0 : Math.Max(0, last.RowNumber() - HeaderRow);
        }

        // ── Export po identifikatoru — validacija / filteri ─────────────────────

        // Ispravnost filtera - prazan identifikator
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ExportClientByIdAsync_WithEmptyIdentifier_ThrowsValidationException(string id)
        {
            var service = NewService(out _);

            var act = async () => await service.ExportClientByIdAsync(id);

            (await act.Should().ThrowAsync<ValidationException>())
                .Which.Field.Should().Be("identifier");
        }

        // Ispravnost filtera - klijent nije pronađen
        [Fact]
        public async Task ExportClientByIdAsync_WhenEntityNotFound_ThrowsValidationException()
        {
            var service = NewService(out _);

            var act = async () => await service.ExportClientByIdAsync("0000000000000");

            await act.Should().ThrowAsync<ValidationException>();
        }

        // Ispravnost filtera - klijent postoji ali nema limita
        [Fact]
        public async Task ExportClientByIdAsync_WhenEntityHasNoLimits_ThrowsValidationException()
        {
            var service = NewService(out var ctx);
            ctx.LegalEntities.Add(NewEntity("ACME", tax: "1111111111111"));
            await ctx.SaveChangesAsync();

            var act = async () => await service.ExportClientByIdAsync("1111111111111");

            await act.Should().ThrowAsync<ValidationException>();
        }

        // Export po poreznom broju
        [Fact]
        public async Task ExportClientByIdAsync_ByTaxNumber_ReturnsNonEmptyExcel()
        {
            var service = NewService(out var ctx);
            ctx.LegalEntities.Add(NewEntity("ACME", tax: "1111111111111"));
            ctx.Limiti.Add(NewLimit("ACME"));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("1111111111111");

            bytes.Should().NotBeNullOrEmpty();
            DataRowCount(OpenSheet(bytes)).Should().Be(1);
        }

        // Export po FBA ID-u
        [Fact]
        public async Task ExportClientByIdAsync_ByFbaId_ReturnsExcel()
        {
            var service = NewService(out var ctx);
            ctx.LegalEntities.Add(NewEntity("NEREZ", resident: false, fba: "1234567890"));
            ctx.Limiti.Add(NewLimit("NEREZ"));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("1234567890");

            DataRowCount(OpenSheet(bytes)).Should().Be(1);
        }

        // Export po matičnom broju (Matbroj)
        [Fact]
        public async Task ExportClientByIdAsync_ByMatbroj_ReturnsExcel()
        {
            var service = NewService(out var ctx);
            ctx.LegalEntities.Add(NewEntity("MATB", matbroj: "MB12345"));
            ctx.Limiti.Add(NewLimit("MATB"));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("MB12345");

            DataRowCount(OpenSheet(bytes)).Should().Be(1);
        }

        // Export po matičnom broju (MaticniBroj)
        [Fact]
        public async Task ExportClientByIdAsync_ByMaticniBroj_ReturnsExcel()
        {
            var service = NewService(out var ctx);
            ctx.LegalEntities.Add(NewEntity("MATICNI", maticni: "4400001"));
            ctx.Limiti.Add(NewLimit("MATICNI"));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("4400001");

            DataRowCount(OpenSheet(bytes)).Should().Be(1);
        }

        // Ispravnost filtera - case-insensitive identifikator
        [Fact]
        public async Task ExportClientByIdAsync_IsCaseInsensitive()
        {
            var service = NewService(out var ctx);
            ctx.LegalEntities.Add(NewEntity("CI", matbroj: "AbC123"));
            ctx.Limiti.Add(NewLimit("CI"));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("abc123");

            DataRowCount(OpenSheet(bytes)).Should().Be(1);
        }

        // Ispravnost filtera - neaktivna pravna lica se preskaču
        [Fact]
        public async Task ExportClientByIdAsync_IgnoresInactiveEntity_ThrowsNotFound()
        {
            var service = NewService(out var ctx);
            ctx.LegalEntities.Add(NewEntity("STARI", tax: "9999999999999", active: false));
            ctx.Limiti.Add(NewLimit("STARI"));
            await ctx.SaveChangesAsync();

            var act = async () => await service.ExportClientByIdAsync("9999999999999");

            await act.Should().ThrowAsync<ValidationException>();
        }

        // Ispravnost filtera - export sadrži samo limite koji pripadaju traženom klijentu
        [Fact]
        public async Task ExportClientByIdAsync_IncludesOnlyMatchingClientLimits()
        {
            var service = NewService(out var ctx);
            ctx.LegalEntities.Add(NewEntity("ACME", tax: "1111111111111"));
            ctx.Limiti.AddRange(
                NewLimit("ACME", tip: "Izloženost"),
                NewLimit("ACME", tip: "Interni"),
                NewLimit("BETA") // drugi klijent — ne smije biti u exportu
            );
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("1111111111111");

            var ws = OpenSheet(bytes);
            DataRowCount(ws).Should().Be(2, "samo ACME limiti");
            // Provjeri da nijedan red nije BETA
            for (int r = FirstDataRow; r < FirstDataRow + 2; r++)
                ws.Cell(r, 1).GetString().Should().Be("ACME");
        }

        // ── Export svih klijenata ────────────────────────────────────────────────

        // Export svih klijenata - broj redova odgovara broju limita
        [Fact]
        public async Task ExportAllClientsWithLimitsAsync_ReturnsRowPerLimit()
        {
            var service = NewService(out var ctx);
            ctx.Limiti.AddRange(NewLimit("A"), NewLimit("B"), NewLimit("C"));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportAllClientsWithLimitsAsync();

            DataRowCount(OpenSheet(bytes)).Should().Be(3);
        }

        // Export svih klijenata - prazna baza daje Excel samo sa headerom
        [Fact]
        public async Task ExportAllClientsWithLimitsAsync_WithNoLimits_ReturnsHeaderOnly()
        {
            var service = NewService(out _);

            var bytes = await service.ExportAllClientsWithLimitsAsync();

            bytes.Should().NotBeNullOrEmpty();
            DataRowCount(OpenSheet(bytes)).Should().Be(0);
        }

        // Export svih klijenata - sortirano po nazivu
        [Fact]
        public async Task ExportAllClientsWithLimitsAsync_OrdersByNaziv()
        {
            var service = NewService(out var ctx);
            ctx.Limiti.AddRange(NewLimit("Zeta"), NewLimit("Alpha"), NewLimit("Mid"));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportAllClientsWithLimitsAsync();

            var ws = OpenSheet(bytes);
            ws.Cell(FirstDataRow, 1).GetString().Should().Be("Alpha");
            ws.Cell(FirstDataRow + 2, 1).GetString().Should().Be("Zeta");
        }

        // ── Tačnost podataka u Excel fajlu ──────────────────────────────────────

        // Tačnost podataka - ispravni nazivi kolona (header)
        [Fact]
        public async Task ExportAllClients_ExcelHasExpectedHeaders()
        {
            var service = NewService(out var ctx);
            ctx.Limiti.Add(NewLimit("A"));
            await ctx.SaveChangesAsync();

            var ws = OpenSheet(await service.ExportAllClientsWithLimitsAsync());

            ws.Cell(HeaderRow, 1).GetString().Should().Be("Naziv");
            ws.Cell(HeaderRow, 2).GetString().Should().Be("Tip limita");
            ws.Cell(HeaderRow, 3).GetString().Should().Be("Iznos limita");
            ws.Cell(HeaderRow, 4).GetString().Should().Be("Utilizacija");
            ws.Cell(HeaderRow, 6).GetString().Should().Be("Raspoloživi limit");
            ws.Cell(HeaderRow, 7).GetString().Should().Be("Regulatorni kapital");
            ws.Cell(HeaderRow, 9).GetString().Should().Be("Kreirao");
        }

        // Tačnost podataka - vrijednosti u ćelijama odgovaraju podacima iz baze
        [Fact]
        public async Task ExportAllClients_ExcelCellsMatchSourceData()
        {
            var service = NewService(out var ctx);
            ctx.Limiti.Add(NewLimit("ACME", iznos: 1500.50m, util: 750.25m, raspolozivi: 749.75m, tip: "Izloženost"));
            await ctx.SaveChangesAsync();

            var ws = OpenSheet(await service.ExportAllClientsWithLimitsAsync());

            ws.Cell(FirstDataRow, 1).GetString().Should().Be("ACME");
            ws.Cell(FirstDataRow, 2).GetString().Should().Be("Izloženost");
            ws.Cell(FirstDataRow, 3).GetValue<decimal>().Should().Be(1500.50m);
            ws.Cell(FirstDataRow, 4).GetValue<decimal>().Should().Be(750.25m);
            ws.Cell(FirstDataRow, 6).GetValue<decimal>().Should().Be(749.75m);
            ws.Cell(FirstDataRow, 7).GetValue<decimal>().Should().Be(5000m);
            ws.Cell(FirstDataRow, 9).GetString().Should().Be("seed");
        }

        // Tačnost podataka - produkovani fajl je validan .xlsx (može se otvoriti)
        [Fact]
        public async Task ExportAllClients_ProducesValidXlsxWorkbook()
        {
            var service = NewService(out var ctx);
            ctx.Limiti.Add(NewLimit("A"));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportAllClientsWithLimitsAsync();

            var open = () => { using var ms = new MemoryStream(bytes); _ = new XLWorkbook(ms); };
            open.Should().NotThrow("izlaz mora biti validan Excel dokument");
        }

        // ── Performanse exporta sa većim brojem zapisa ──────────────────────────

        // Performanse - export velikog broja zapisa se izvrši i sadrži sve redove
        [Fact]
        public async Task ExportAllClientsWithLimitsAsync_WithLargeDataset_ExportsAllRows()
        {
            // Arrange — 2000 limita
            var service = NewService(out var ctx);
            const int count = 2000;
            for (int i = 0; i < count; i++)
                ctx.Limiti.Add(NewLimit($"Klijent {i:D5}", util: i, raspolozivi: i % 7 == 0 ? -1m : 10m));
            await ctx.SaveChangesAsync();

            // Act
            var sw = Stopwatch.StartNew();
            var bytes = await service.ExportAllClientsWithLimitsAsync();
            sw.Stop();

            // Assert
            DataRowCount(OpenSheet(bytes)).Should().Be(count, "svi zapisi moraju biti u exportu");
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
                "export ~2000 zapisa ne smije trajati nerazumno dugo");
        }
    }
}
