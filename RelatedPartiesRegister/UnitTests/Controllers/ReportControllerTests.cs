using System.Security.Claims;
using FluentAssertions;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Report;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace UnitTests.Controllers
{
    /// <summary>
    /// Testiranje endpointa za izvještaje i izvoz.
    /// Ovaj fajl: ReportController — validacija ulaza, HTTP odgovori i Excel file odgovori.
    /// IReportService se mockira (Moq); ne dira se prava baza.
    /// </summary>
    public class ReportControllerTests
    {
        private const string XlsxContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private static ReportController CreateController(Mock<IReportService> service)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "tester") }, "TestAuth");
            return new ReportController(service.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        // ── Mjesečni izvještaj - validacija ─────────────────────────────────────

        // Mjesečni izvještaj - neispravan mjesec vraća 400
        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(-1)]
        public async Task GenerateMonthlyReport_WithInvalidMonth_Returns400(int month)
        {
            // Arrange
            var service = new Mock<IReportService>();
            var controller = CreateController(service);

            // Act
            var result = await controller.GenerateMonthlyReport(2026, month);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            service.Verify(s => s.GenerateMonthlyReportAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
                Times.Never, "servis se ne smije pozvati za nevalidan mjesec");
        }

        // Mjesečni izvještaj - neispravna godina vraća 400
        [Theory]
        [InlineData(1999)]
        [InlineData(3000)]
        public async Task GenerateMonthlyReport_WithInvalidYear_Returns400(int year)
        {
            // Arrange
            var service = new Mock<IReportService>();
            var controller = CreateController(service);

            // Act
            var result = await controller.GenerateMonthlyReport(year, 6);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // Mjesečni izvještaj - validan ulaz vraća 201 Created
        [Fact]
        public async Task GenerateMonthlyReport_WithValidInput_ReturnsCreated()
        {
            // Arrange
            var service = new Mock<IReportService>();
            service.Setup(s => s.GenerateMonthlyReportAsync(2026, 6, "tester"))
                   .ReturnsAsync(new ReportDTO { ReportType = "MONTHLY" });
            var controller = CreateController(service);

            // Act
            var result = await controller.GenerateMonthlyReport(2026, 6);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            service.Verify(s => s.GenerateMonthlyReportAsync(2026, 6, "tester"), Times.Once);
        }

        // ── Dnevni izvještaj ────────────────────────────────────────────────────

        // Dnevni izvještaj - kreiranje vraća 201 Created
        [Fact]
        public async Task GenerateDailyReport_ReturnsCreated()
        {
            // Arrange
            var service = new Mock<IReportService>();
            service.Setup(s => s.GenerateDailyReportAsync("tester"))
                   .ReturnsAsync(new ReportDTO { ReportType = "DAILY" });
            var controller = CreateController(service);

            // Act
            var result = await controller.GenerateDailyReport();

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
        }

        // Dnevni izvještaj - lista vraća 200 OK
        [Fact]
        public async Task GetDailyReports_ReturnsOk()
        {
            // Arrange
            var service = new Mock<IReportService>();
            service.Setup(s => s.GetDailyReportsAsync(1, 20))
                   .ReturnsAsync(new ReportListDTO { Total = 0 });
            var controller = CreateController(service);

            // Act
            var result = await controller.GetDailyReports();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        // ── Excel export - file odgovori ────────────────────────────────────────

        // Export po identifikatoru - vraća .xlsx file s ispravnim content-type i imenom
        [Fact]
        public async Task ExportClient_ReturnsXlsxFile()
        {
            // Arrange
            var content = new byte[] { 1, 2, 3, 4 };
            var service = new Mock<IReportService>();
            service.Setup(s => s.ExportClientByIdAsync("1111111111111")).ReturnsAsync(content);
            var controller = CreateController(service);

            // Act
            var result = await controller.ExportClient("1111111111111");

            // Assert
            var file = result.Should().BeOfType<FileContentResult>().Subject;
            file.ContentType.Should().Be(XlsxContentType);
            file.FileContents.Should().BeEquivalentTo(content);
            file.FileDownloadName.Should().Contain("1111111111111").And.EndWith(".xlsx");
        }

        // Export svih klijenata - vraća .xlsx file
        [Fact]
        public async Task ExportAllClients_ReturnsXlsxFile()
        {
            // Arrange
            var content = new byte[] { 9, 8, 7 };
            var service = new Mock<IReportService>();
            service.Setup(s => s.ExportAllClientsWithLimitsAsync()).ReturnsAsync(content);
            var controller = CreateController(service);

            // Act
            var result = await controller.ExportAllClients();

            // Assert
            var file = result.Should().BeOfType<FileContentResult>().Subject;
            file.ContentType.Should().Be(XlsxContentType);
            file.FileContents.Should().BeEquivalentTo(content);
            file.FileDownloadName.Should().EndWith(".xlsx");
        }
    }
}
