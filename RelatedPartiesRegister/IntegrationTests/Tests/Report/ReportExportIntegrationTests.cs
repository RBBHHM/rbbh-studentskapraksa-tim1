using ClosedXML.Excel;
using FluentAssertions;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.Exceptions;
using IntegrationTests.Infrastructure;
using LegalEntityEntity = RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace IntegrationTests.Tests.Report
{
    /// <summary>
    /// Integracijsko testiranje izvještaja i izvoza.
    /// API integration test - Excel export preko ReportController + prava SQL Server baza (Testcontainers).
    ///
    /// Fokus je na read-only export endpointima (ExportClient / ExportAllClients) i validaciji ulaza,
    /// jer je to determinističko na pravoj bazi. Generisanje izvještaja (daily/monthly) se testira
    /// unit testovima (ReportService + InMemory) — ReportDate koristi DateTime Kind=Unspecified pa
    /// pisanje u SQL Server 'timestamptz' nije pouzdano bez izmjene production koda.
    ///
    /// Namjerno se NE koristi _fixture.ResetAsync() (dira unlock_requests tabelu s poznatim
    /// migration bug-om 'AdminNote') — čisti se samo Limiti i LegalEntities.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class ReportExportIntegrationTests
    {
        private const string XlsxContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly DatabaseFixture _fixture;

        public ReportExportIntegrationTests(DatabaseFixture fixture) => _fixture = fixture;

        private async Task ResetReportDataAsync()
        {
            await using var ctx = _fixture.CreateContext();
            ctx.Limiti.RemoveRange(ctx.Limiti);
            ctx.LegalEntities.RemoveRange(ctx.LegalEntities);
            await ctx.SaveChangesAsync();
        }

        private ReportController NewController(string user = "integration-tester")
        {
            var ctx = _fixture.CreateContext();
            var logger = new Mock<ILogger<ReportService>>();
            var service = new ReportService(ctx, logger.Object);
            return new ReportController(service).WithHttpContext(user);
        }

        private static Limit NewLimit(string naziv, string tip = "Izloženost") => new()
        {
            Naziv = naziv,
            TipLimita = tip,
            IznosLimita = 1000m,
            Utilizacija = 200m,
            RaspoloziviLimit = 100m,
            RegulatorniKapital = 5000m,
            OsnovniKapital = 4000m,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        };

        private static LegalEntityEntity NewEntity(string name, string? tax = null) => new()
        {
            Name = name,
            IsResident = true,
            TaxNumber = tax,
            BasisOfConnection = "Vlasništvo",
            Status = "Draft",
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        private static int DataRowCount(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var ws = new XLWorkbook(ms).Worksheet(1);
            var last = ws.LastRowUsed();
            return last is null ? 0 : Math.Max(0, last.RowNumber() - 3); // header red = 3
        }

        // Export svih klijenata - vraća .xlsx sa redom po limitu (prava baza)
        [SkippableFact]
        public async Task ExportAllClients_ReturnsXlsxFileWithRows()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await ResetReportDataAsync();

            await using (var ctx = _fixture.CreateContext())
            {
                ctx.Limiti.AddRange(NewLimit("ACME"), NewLimit("BETA"));
                await ctx.SaveChangesAsync();
            }

            // Act
            var result = await NewController().ExportAllClients();

            // Assert
            var file = result.Should().BeOfType<FileContentResult>().Subject;
            file.ContentType.Should().Be(XlsxContentType);
            DataRowCount(file.FileContents).Should().Be(2);
        }

        // Export po poreznom broju - vraća .xlsx za pronađenog klijenta (prava baza)
        [SkippableFact]
        public async Task ExportClient_ByTaxNumber_ReturnsXlsxFile()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await ResetReportDataAsync();

            await using (var ctx = _fixture.CreateContext())
            {
                ctx.LegalEntities.Add(NewEntity("ACME", tax: "1111111111111"));
                ctx.Limiti.Add(NewLimit("ACME"));
                await ctx.SaveChangesAsync();
            }

            // Act
            var result = await NewController().ExportClient("1111111111111");

            // Assert
            var file = result.Should().BeOfType<FileContentResult>().Subject;
            file.ContentType.Should().Be(XlsxContentType);
            DataRowCount(file.FileContents).Should().Be(1);
        }

        // Ispravnost filtera - nepostojeći klijent baca ValidationException (u produkciji 400/404)
        [SkippableFact]
        public async Task ExportClient_WhenNotFound_ThrowsValidationException()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await ResetReportDataAsync();

            var act = async () => await NewController().ExportClient("0000000000000");

            await act.Should().ThrowAsync<ValidationException>();
        }

        // Mjesečni izvještaj - neispravan mjesec vraća 400 (bez pristupa bazi)
        [SkippableFact]
        public async Task GenerateMonthlyReport_WithInvalidMonth_Returns400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var result = await NewController().GenerateMonthlyReport(2026, 13);

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
