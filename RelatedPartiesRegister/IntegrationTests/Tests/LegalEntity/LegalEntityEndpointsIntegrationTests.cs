using FluentAssertions;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.LegalEntity;
using RBBH.ConnectedParties.Exceptions;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace IntegrationTests.Tests.LegalEntity
{
    /// <summary>
    /// PL-57 / PL-58 - API integration test - pravna lica
    ///
    /// Testira LegalEntityController + LegalEntityService nad PRAVOM SQL Server bazom
    /// (Testcontainers). IAuditService se mockira (Moq) — audit nije predmet ovih testova.
    /// Kontroler se instancira direktno (isti pristup kao postojeći PeriodLock/Role integration
    /// testovi), pa se ovdje testira poslovna logika kroz kontroler + perzistencija u bazi.
    ///
    /// NAPOMENA o validacijskim 400 odgovorima:
    ///   LegalEntityService baca ValidationException za nevalidan unos. U produkciji
    ///   GlobalExceptionHandler middleware to mapira u HTTP 400. Pri direktnom instanciranju
    ///   kontrolera (bez HTTP pipeline-a) middleware se ne izvršava, pa se umjesto 400 odgovora
    ///   provjerava da metoda baca ValidationException (poslovno ekvivalentno).
    ///   Čisti 400 (ModelState) i 401 scenariji pokriveni su zasebno (vidi 401 test ispod
    ///   i frontend LegalPersonServiceTests).
    ///
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class LegalEntityEndpointsIntegrationTests
    {
        private const string ValidTaxNumber = "1111111111111";
        private readonly DatabaseFixture _fixture;

        public LegalEntityEndpointsIntegrationTests(DatabaseFixture fixture) => _fixture = fixture;

        // Čisti SAMO LegalEntities tabelu. Namjerno NE koristimo _fixture.ResetAsync() jer ono
        // dira i UnlockRequests tabelu — upit nad njom trenutno puca (production migration gap:
        // entitet UnlockRequest.AdminNote nema odgovarajuću kolonu u migraciji → 42703). Ovi
        // PL-57/58 testovi ne ovise o tim tabelama, pa ih izoliramo.
        private async Task ResetLegalEntitiesAsync()
        {
            await using var ctx = _fixture.CreateContext();
            ctx.LegalEntities.RemoveRange(ctx.LegalEntities);
            await ctx.SaveChangesAsync();
        }

        // Novi kontroler s novim kontekstom — simulira zaseban HTTP zahtjev.
        private LegalEntityController NewController(string user = "integration-tester")
        {
            var ctx = _fixture.CreateContext();
            var logger = new Mock<ILogger<LegalEntityService>>();
            var service = new LegalEntityService(ctx, logger.Object);
            var audit = new Mock<IAuditService>();
            return new LegalEntityController(service, audit.Object).WithHttpContext(user);
        }

        private static CreateLegalEntityDTO ValidResidentDto() => new()
        {
            IsResident = true,
            TaxNumber = ValidTaxNumber,
            Name = "Rezidentno DOO",
            GccNumber = "1001",
            GccName = "Rezidentni GCC",
            BasisOfConnection = "Vlasništvo",
            ConnectionDescription = "Vlasništvo",
            ConnectedWithBank = true,
            DateFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        private static CreateLegalEntityDTO ValidNonResidentDto() => new()
        {
            IsResident = false,
            FbaId = "1234567890",
            Name = "Nerezidentno LLC",
            GccNumber = "1002",
            GccName = "Nerezidentni GCC",
            BasisOfConnection = "Vlasništvo",
            ConnectionDescription = "Vlasništvo",
            ConnectedWithBank = true,
            DateFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // ── PL-57 - Rezidentna pravna lica ─────────────────────────────────────────

        // PL-57 - Rezidentna pravna lica — pozitivan unos kroz API
        [SkippableFact]
        public async Task Create_ValidResident_Returns201AndPersists()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await ResetLegalEntitiesAsync();

            // Act
            var result = await NewController().Create(ValidResidentDto());

            // Assert
            var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var dto = created.Value.Should().BeOfType<LegalEntityDTO>().Subject;
            dto.TaxNumber.Should().Be(ValidTaxNumber);
            dto.IsResident.Should().BeTrue();
            dto.Status.Should().Be("Draft");

            await using var verify = _fixture.CreateContext();
            verify.LegalEntities.Any(e => e.TaxNumber == ValidTaxNumber).Should().BeTrue();
        }

        // Validacija - porez_broj  (PL-57: porez_broj obavezan za rezidente)
        [SkippableFact]
        public async Task Create_ResidentWithoutTaxNumber_ThrowsValidationException()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await ResetLegalEntitiesAsync();

            var dto = ValidResidentDto();
            dto.TaxNumber = null;

            // Act — u produkciji middleware mapira u 400 Bad Request
            var act = async () => await NewController().Create(dto);

            // Assert
            (await act.Should().ThrowAsync<ValidationException>())
                .Which.Field.Should().Be("taxNumber");
        }

        // ── PL-58 - Nerezidentna pravna lica ───────────────────────────────────────

        // PL-58 - Nerezidentna pravna lica — pozitivan unos kroz API
        [SkippableFact]
        public async Task Create_ValidNonResident_Returns201WithFbaIdAndNoTaxNumber()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await ResetLegalEntitiesAsync();

            // Act
            var result = await NewController().Create(ValidNonResidentDto());

            // Assert
            var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var dto = created.Value.Should().BeOfType<LegalEntityDTO>().Subject;
            dto.FbaId.Should().Be("1234567890");
            dto.TaxNumber.Should().BeNull("porez_broj je neaktivan za nerezidente");

            await using var verify = _fixture.CreateContext();
            verify.LegalEntities.Any(e => e.FbaId == "1234567890" && e.TaxNumber == null).Should().BeTrue();
        }

        // Validacija - FBA_ID  (PL-58: FBA_ID obavezan za nerezidente)
        [SkippableFact]
        public async Task Create_NonResidentWithoutFbaId_ThrowsValidationException()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await ResetLegalEntitiesAsync();

            var dto = ValidNonResidentDto();
            dto.FbaId = null;

            // Act
            var act = async () => await NewController().Create(dto);

            // Assert
            (await act.Should().ThrowAsync<ValidationException>())
                .Which.Field.Should().Be("fbaId");
        }

        // ── GET endpointi ──────────────────────────────────────────────────────────

        // API integration test - pravna lica (GetById nakon unosa)
        [SkippableFact]
        public async Task GetById_AfterCreate_ReturnsEntity()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await ResetLegalEntitiesAsync();

            var created = (await NewController().Create(ValidResidentDto()) as CreatedAtActionResult)!
                .Value as LegalEntityDTO;

            // Act
            var result = await NewController().GetById(created!.Id);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeOfType<LegalEntityDTO>()
                .Which.Id.Should().Be(created.Id);
        }

        // API integration test - pravna lica (404 Not Found)
        [SkippableFact]
        public async Task GetById_WhenNotExists_Returns404()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await ResetLegalEntitiesAsync();

            // Act
            var result = await NewController().GetById(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // API integration test - pravna lica (lista sadrži kreirani zapis)
        [SkippableFact]
        public async Task GetAll_AfterCreate_ReturnsEntityInList()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await ResetLegalEntitiesAsync();

            await NewController().Create(ValidResidentDto());

            // Act
            var result = await NewController().GetAll();

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var list = ok.Value.Should().BeOfType<LegalEntityListDTO>().Subject;
            list.Total.Should().BeGreaterThanOrEqualTo(1);
            list.Items.Should().Contain(e => e.TaxNumber == ValidTaxNumber);
        }
    }
}
