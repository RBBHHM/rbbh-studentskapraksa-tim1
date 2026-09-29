using System.Diagnostics;
using ClosedXML.Excel;
using FluentAssertions;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.DL.Entities.Report;
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
    ///   - Ispravnost filtera (identifikator, case-insensitive, samo aktivni, match po pravnom licu)
    ///   - Performanse exporta sa većim brojem zapisa
    ///
    /// Kolone u Excelu (RegistryExcelExporter — header red = 1, podaci od reda 2):
    ///   1 Naziv | 2 Rezidentnost | 3 FBA ID | 4 Porezni broj | 5 Matični broj | 6 Matbroj |
    ///   7 Tip limita | 8 Odobreni limit | 9 Maksimalna očekivana utilizacija |
    ///   10 Rok očekivane utilizacije | 11 Komentar | 12 Regulatorni kapital | 13 Osnovni kapital
    /// </summary>
    public class ExcelExportTests
    {
        private const int HeaderRow = 1;
        private const int FirstDataRow = 2;

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

        private static Limit NewLimit(Guid legalEntityId, string tip = "MM",
            decimal? odobreniLimit = 1000m, decimal? maksUtil = 200m, string? komentar = null) => new()
        {
            LegalEntityId = legalEntityId,
            TipLimita = tip,
            OdobreniLimit = odobreniLimit,
            MaksimalnaOcekivanaUtilizacija = maksUtil,
            Komentar = komentar,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        };

        private static ClientLimit NewClientLimit(Guid legalEntityId, decimal regulatoryCapital = 5000m, decimal coreCapital = 4000m) => new()
        {
            LegalEntityId = legalEntityId,
            RegulatoryCapital = regulatoryCapital,
            CoreCapital = coreCapital,
            ExposureLimit = 1000m,
            CurrentExposure = 0m,
            Currency = "BAM",
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
            var entity = NewEntity("ACME", tax: "1111111111111");
            ctx.LegalEntities.Add(entity);
            ctx.Limiti.Add(NewLimit(entity.Id));
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
            var entity = NewEntity("NEREZ", resident: false, fba: "1234567890");
            ctx.LegalEntities.Add(entity);
            ctx.Limiti.Add(NewLimit(entity.Id));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("1234567890");

            DataRowCount(OpenSheet(bytes)).Should().Be(1);
        }

        // Export po matičnom broju (Matbroj)
        [Fact]
        public async Task ExportClientByIdAsync_ByMatbroj_ReturnsExcel()
        {
            var service = NewService(out var ctx);
            var entity = NewEntity("MATB", matbroj: "MB12345");
            ctx.LegalEntities.Add(entity);
            ctx.Limiti.Add(NewLimit(entity.Id));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("MB12345");

            DataRowCount(OpenSheet(bytes)).Should().Be(1);
        }

        // Export po matičnom broju (MaticniBroj)
        [Fact]
        public async Task ExportClientByIdAsync_ByMaticniBroj_ReturnsExcel()
        {
            var service = NewService(out var ctx);
            var entity = NewEntity("MATICNI", maticni: "4400001");
            ctx.LegalEntities.Add(entity);
            ctx.Limiti.Add(NewLimit(entity.Id));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("4400001");

            DataRowCount(OpenSheet(bytes)).Should().Be(1);
        }

        // Ispravnost filtera - case-insensitive identifikator
        [Fact]
        public async Task ExportClientByIdAsync_IsCaseInsensitive()
        {
            var service = NewService(out var ctx);
            var entity = NewEntity("CI", matbroj: "AbC123");
            ctx.LegalEntities.Add(entity);
            ctx.Limiti.Add(NewLimit(entity.Id));
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("abc123");

            DataRowCount(OpenSheet(bytes)).Should().Be(1);
        }

        // Ispravnost filtera - neaktivna pravna lica se preskaču
        [Fact]
        public async Task ExportClientByIdAsync_IgnoresInactiveEntity_ThrowsNotFound()
        {
            var service = NewService(out var ctx);
            var entity = NewEntity("STARI", tax: "9999999999999", active: false);
            ctx.LegalEntities.Add(entity);
            ctx.Limiti.Add(NewLimit(entity.Id));
            await ctx.SaveChangesAsync();

            var act = async () => await service.ExportClientByIdAsync("9999999999999");

            await act.Should().ThrowAsync<ValidationException>();
        }

        // Ispravnost filtera - export sadrži samo limite koji pripadaju traženom klijentu
        [Fact]
        public async Task ExportClientByIdAsync_IncludesOnlyMatchingClientLimits()
        {
            var service = NewService(out var ctx);
            var acme = NewEntity("ACME", tax: "1111111111111");
            var beta = NewEntity("BETA");
            ctx.LegalEntities.AddRange(acme, beta);
            ctx.Limiti.AddRange(
                NewLimit(acme.Id, tip: "MM"),
                NewLimit(acme.Id, tip: "SATAFXs"),
                NewLimit(beta.Id)); // drugi klijent — ne smije biti u exportu
            await ctx.SaveChangesAsync();

            var bytes = await service.ExportClientByIdAsync("1111111111111");

            var ws = OpenSheet(bytes);
            DataRowCount(ws).Should().Be(2, "samo ACME limiti");
            for (int r = FirstDataRow; r < FirstDataRow + 2; r++)
                ws.Cell(r, 1).GetString().Should().Be("ACME");
        }

        // ── Export svih klijenata ────────────────────────────────────────────────

        // Export svih klijenata - broj redova odgovara broju limita
        [Fact]
        public async Task ExportAllClientsWithLimitsAsync_ReturnsRowPerLimit()
        {
            var service = NewService(out var ctx);
            var a = NewEntity("A");
            var b = NewEntity("B");
            var c = NewEntity("C");
            ctx.LegalEntities.AddRange(a, b, c);
            ctx.Limiti.AddRange(NewLimit(a.Id), NewLimit(b.Id), NewLimit(c.Id));
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
            var zeta = NewEntity("Zeta");
            var alpha = NewEntity("Alpha");
            var mid = NewEntity("Mid");
            ctx.LegalEntities.AddRange(zeta, alpha, mid);
            ctx.Limiti.AddRange(NewLimit(zeta.Id), NewLimit(alpha.Id), NewLimit(mid.Id));
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
            var entity = NewEntity("A");
            ctx.LegalEntities.Add(entity);
            ctx.Limiti.Add(NewLimit(entity.Id));
            await ctx.SaveChangesAsync();

            var ws = OpenSheet(await service.ExportAllClientsWithLimitsAsync());

            ws.Cell(HeaderRow, 1).GetString().Should().Be("Naziv");
            ws.Cell(HeaderRow, 2).GetString().Should().Be("Rezidentnost");
            ws.Cell(HeaderRow, 7).GetString().Should().Be("Tip limita");
            ws.Cell(HeaderRow, 8).GetString().Should().Be("Odobreni limit");
            ws.Cell(HeaderRow, 12).GetString().Should().Be("Regulatorni kapital");
            ws.Cell(HeaderRow, 13).GetString().Should().Be("Osnovni kapital");
        }

        // Tačnost podataka - vrijednosti u ćelijama odgovaraju podacima iz baze
        [Fact]
        public async Task ExportAllClients_ExcelCellsMatchSourceData()
        {
            var service = NewService(out var ctx);
            var entity = NewEntity("ACME", tax: "1111111111111");
            ctx.LegalEntities.Add(entity);
            ctx.Limiti.Add(NewLimit(entity.Id, tip: "SATAFXs", odobreniLimit: 1500.50m, maksUtil: 750.25m, komentar: "Test komentar"));
            ctx.ClientLimits.Add(NewClientLimit(entity.Id, regulatoryCapital: 5000m, coreCapital: 4000m));
            await ctx.SaveChangesAsync();

            var ws = OpenSheet(await service.ExportAllClientsWithLimitsAsync());

            ws.Cell(FirstDataRow, 1).GetString().Should().Be("ACME");
            ws.Cell(FirstDataRow, 2).GetString().Should().Be("Rezident");
            ws.Cell(FirstDataRow, 7).GetString().Should().Be("SATAFXs");
            ws.Cell(FirstDataRow, 8).GetValue<decimal>().Should().Be(1500.50m);
            ws.Cell(FirstDataRow, 9).GetValue<decimal>().Should().Be(750.25m);
            ws.Cell(FirstDataRow, 11).GetString().Should().Be("Test komentar");
            ws.Cell(FirstDataRow, 12).GetValue<decimal>().Should().Be(5000m);
            ws.Cell(FirstDataRow, 13).GetValue<decimal>().Should().Be(4000m);
        }

        // Tačnost podataka - produkovani fajl je validan .xlsx (može se otvoriti)
        [Fact]
        public async Task ExportAllClients_ProducesValidXlsxWorkbook()
        {
            var service = NewService(out var ctx);
            var entity = NewEntity("A");
            ctx.LegalEntities.Add(entity);
            ctx.Limiti.Add(NewLimit(entity.Id));
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
            {
                var entity = NewEntity($"Klijent {i:D5}");
                ctx.LegalEntities.Add(entity);
                ctx.Limiti.Add(NewLimit(entity.Id, maksUtil: i));
            }
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
