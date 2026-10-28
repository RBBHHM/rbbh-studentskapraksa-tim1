using FluentAssertions;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.LegalEntity;
using RBBH.ConnectedParties.DL.Entities.Report;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.Extensions.Logging;
using Moq;
using UnitTests.Mocks.DB;

namespace UnitTests.Services.ReportServiceTests
{
    /// <summary>
    /// Testiranje generisanja izvještaja.
    /// Ovaj fajl: Dnevni i mjesečni izvještaji (ReportService generisanje + liste + paginacija).
    /// Koristi EF Core InMemory bazu; ILogger se mockira (Moq).
    ///
    /// Izvor podataka za izvještaj je tabela ClientLimits (Kapital modul, 1:1 sa LegalEntity); agregati:
    ///   TotalClients = broj ClientLimit zapisa
    ///   ClientsWithBreachedLimit = broj zapisa gdje je IsLimitBreached true
    ///   TotalExposure = suma CurrentExposure
    /// </summary>
    public class ReportGenerationTests
    {
        private static ReportService NewService(out ConnectedPartiesDbContext ctx)
        {
            ctx = InMemoryContextFactory.Create();
            var logger = new Mock<ILogger<ReportService>>();
            return new ReportService(ctx, logger.Object);
        }

        private static LegalEntity NewLegalEntity(string name) => new()
        {
            IsResident = true,
            TaxNumber = Guid.NewGuid().ToString("N")[..13],
            Name = name,
            BasisOfConnection = "Test osnov",
            Status = "VERIFIED",
            CreatedBy = "seed"
        };

        private static ClientLimit NewClientLimit(Guid legalEntityId, decimal currentExposure, bool breached = false) => new()
        {
            LegalEntityId = legalEntityId,
            RegulatoryCapital = 5000m,
            CoreCapital = 4000m,
            ExposureLimit = 1000m,
            CurrentExposure = currentExposure,
            Currency = "BAM",
            IsLimitBreached = breached,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow
        };

        private static Report NewReport(string type, DateTime date) => new()
        {
            ReportType = type,
            ReportDate = date,
            TotalClients = 0,
            ClientsWithBreachedLimit = 0,
            TotalExposure = 0m,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // ── Dnevni izvještaj ────────────────────────────────────────────────────

        // Dnevni izvještaj - tačnost agregata (broj klijenata, prekoračenja, izloženost)
        [Fact]
        public async Task GenerateDailyReportAsync_WithLimits_ComputesAggregatesAndPersists()
        {
            // Arrange
            var service = NewService(out var ctx);
            var acme = NewLegalEntity("ACME");
            var beta = NewLegalEntity("BETA");
            ctx.LegalEntities.AddRange(acme, beta);
            ctx.ClientLimits.AddRange(
                NewClientLimit(acme.Id, currentExposure: 300m),
                NewClientLimit(beta.Id, currentExposure: 300m, breached: true) // prekoračen
            );
            await ctx.SaveChangesAsync();

            // Act
            var report = await service.GenerateDailyReportAsync("tester");

            // Assert
            report.ReportType.Should().Be("DAILY");
            report.ReportDate.Should().Be(DateTime.UtcNow.Date);
            report.TotalClients.Should().Be(2, "ACME i BETA su 2 različita klijenta");
            report.ClientsWithBreachedLimit.Should().Be(1, "samo BETA ima IsLimitBreached = true");
            report.TotalExposure.Should().Be(600m, "300 + 300");
            ctx.Reports.Should().ContainSingle(r => r.ReportType == "DAILY");
        }

        // Dnevni izvještaj - prazna baza limita
        [Fact]
        public async Task GenerateDailyReportAsync_WithNoLimits_ReturnsZeroAggregates()
        {
            // Arrange
            var service = NewService(out _);

            // Act
            var report = await service.GenerateDailyReportAsync("tester");

            // Assert
            report.TotalClients.Should().Be(0);
            report.ClientsWithBreachedLimit.Should().Be(0);
            report.TotalExposure.Should().Be(0m);
        }

        // Dnevni izvještaj - prekoračenje se broji samo kad je IsLimitBreached true
        [Fact]
        public async Task GenerateDailyReportAsync_CountsBreachedOnlyWhenIsLimitBreachedTrue()
        {
            // Arrange
            var service = NewService(out var ctx);
            var a = NewLegalEntity("A");
            var b = NewLegalEntity("B");
            var c = NewLegalEntity("C");
            ctx.LegalEntities.AddRange(a, b, c);
            ctx.ClientLimits.AddRange(
                NewClientLimit(a.Id, 10m, breached: false),
                NewClientLimit(b.Id, 10m, breached: true),
                NewClientLimit(c.Id, 10m, breached: true)
            );
            await ctx.SaveChangesAsync();

            // Act
            var report = await service.GenerateDailyReportAsync("tester");

            // Assert
            report.ClientsWithBreachedLimit.Should().Be(2);
        }

        // ── Mjesečni izvještaj ──────────────────────────────────────────────────

        // Mjesečni izvještaj - tip i datum (prvi dan u mjesecu)
        [Fact]
        public async Task GenerateMonthlyReportAsync_SetsTypeMonthlyAndFirstOfMonth()
        {
            // Arrange
            var service = NewService(out var ctx);
            var acme = NewLegalEntity("ACME");
            ctx.LegalEntities.Add(acme);
            ctx.ClientLimits.Add(NewClientLimit(acme.Id, 200m));
            await ctx.SaveChangesAsync();

            // Act
            var report = await service.GenerateMonthlyReportAsync(2026, 3, "tester");

            // Assert
            report.ReportType.Should().Be("MONTHLY");
            report.ReportDate.Should().Be(new DateTime(2026, 3, 1));
            report.TotalClients.Should().Be(1);
        }

        // ── Liste izvještaja ────────────────────────────────────────────────────

        // Dnevni izvještaji - lista vraća samo DAILY, najnoviji prvi
        [Fact]
        public async Task GetDailyReportsAsync_ReturnsOnlyDailyOrderedNewestFirst()
        {
            // Arrange
            var service = NewService(out var ctx);
            ctx.Reports.AddRange(
                NewReport("DAILY", new DateTime(2026, 1, 1)),
                NewReport("DAILY", new DateTime(2026, 2, 1)),
                NewReport("MONTHLY", new DateTime(2026, 3, 1))
            );
            await ctx.SaveChangesAsync();

            // Act
            var result = await service.GetDailyReportsAsync(1, 20);

            // Assert
            result.Total.Should().Be(2);
            result.Items.Should().OnlyContain(r => r.ReportType == "DAILY");
            result.Items.First().ReportDate.Should().Be(new DateTime(2026, 2, 1), "najnoviji prvi");
        }

        // Mjesečni izvještaji - lista vraća samo MONTHLY
        [Fact]
        public async Task GetMonthlyReportsAsync_ReturnsOnlyMonthly()
        {
            // Arrange
            var service = NewService(out var ctx);
            ctx.Reports.AddRange(
                NewReport("DAILY", new DateTime(2026, 1, 1)),
                NewReport("MONTHLY", new DateTime(2026, 2, 1)),
                NewReport("MONTHLY", new DateTime(2026, 3, 1))
            );
            await ctx.SaveChangesAsync();

            // Act
            var result = await service.GetMonthlyReportsAsync(1, 20);

            // Assert
            result.Total.Should().Be(2);
            result.Items.Should().OnlyContain(r => r.ReportType == "MONTHLY");
        }

        // Izvještaji - paginacija (page/pageSize)
        [Fact]
        public async Task GetDailyReportsAsync_Pagination_ReturnsRequestedPageSize()
        {
            // Arrange
            var service = NewService(out var ctx);
            ctx.Reports.AddRange(
                NewReport("DAILY", new DateTime(2026, 1, 1)),
                NewReport("DAILY", new DateTime(2026, 1, 2)),
                NewReport("DAILY", new DateTime(2026, 1, 3))
            );
            await ctx.SaveChangesAsync();

            // Act
            var result = await service.GetDailyReportsAsync(page: 1, pageSize: 2);

            // Assert
            result.Total.Should().Be(3, "ukupan broj je 3");
            result.Items.Should().HaveCount(2, "stranica vraća 2 zapisa");
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(2);
        }
    }
}
