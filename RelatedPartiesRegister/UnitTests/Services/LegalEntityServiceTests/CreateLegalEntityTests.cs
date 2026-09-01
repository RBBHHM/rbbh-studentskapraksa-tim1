using FluentAssertions;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.LegalEntity;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using UnitTests.Mocks.DB;

namespace UnitTests.Services.LegalEntityServiceTests
{
    /// <summary>
    /// PL-56 - Modul pravna lica  (backend servis <see cref="LegalEntityService"/>)
    /// PL-57 - Rezidentna pravna lica  |  PL-58 - Nerezidentna pravna lica
    ///
    /// Testira se poslovna logika kreiranja pravnog lica (validacija + perzistencija)
    /// preko EF Core InMemory baze. Zavisnost ILogger se mockira (Moq).
    /// </summary>
    public class CreateLegalEntityTests
    {
        // Algoritam kontrolne cifre je isti kao JMBG -> "1111111111111" je validan 13-cifreni porezni broj.
        private const string ValidTaxNumber = "1111111111111";

        private static LegalEntityService NewService(out ConnectedPartiesDbContext ctx)
        {
            ctx = InMemoryContextFactory.Create();
            var logger = new Mock<ILogger<LegalEntityService>>();
            return new LegalEntityService(ctx, logger.Object);
        }

        private static CreateLegalEntityDTO ValidResidentDto() => new()
        {
            IsResident = true,
            TaxNumber = ValidTaxNumber,
            Name = "Rezidentno DOO",
            GccNumber = "10001",
            GccName = "Rezidentna grupa",
            BasisOfConnection = "Vlasništvo",
            ConnectionDescription = "Opis povezanosti",
            ConnectedWithBank = true,
            DateFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        private static CreateLegalEntityDTO ValidNonResidentDto() => new()
        {
            IsResident = false,
            FbaId = "1234567890",
            Name = "Nerezidentno LLC",
            GccNumber = "10002",
            GccName = "Nerezidentna grupa",
            BasisOfConnection = "Vlasništvo",
            ConnectionDescription = "Opis povezanosti",
            ConnectedWithBank = true,
            DateFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // ── PL-57 - Rezidentna pravna lica ─────────────────────────────────────────

        // PL-57 - Rezidentna pravna lica — pozitivan unos
        [Fact]
        public async Task CreateAsync_WithValidResident_PersistsAndReturnsDraftEntity()
        {
            // Arrange
            var service = NewService(out var ctx);

            // Act
            var result = await service.CreateAsync(ValidResidentDto(), "tester");

            // Assert
            result.Should().NotBeNull();
            result.IsResident.Should().BeTrue();
            result.TaxNumber.Should().Be(ValidTaxNumber);
            result.FbaId.Should().BeNull();
            result.Status.Should().Be("Draft");
            ctx.LegalEntities.Should().ContainSingle(e => e.TaxNumber == ValidTaxNumber);
        }

        // Validacija - porez_broj  (PL-57: porez_broj obavezan za rezidente)
        [Fact]
        public async Task CreateAsync_ResidentWithoutTaxNumber_ThrowsValidationException()
        {
            // Arrange
            var service = NewService(out _);
            var dto = ValidResidentDto();
            dto.TaxNumber = null;

            // Act
            var act = async () => await service.CreateAsync(dto, "tester");

            // Assert
            (await act.Should().ThrowAsync<ValidationException>())
                .Which.Field.Should().Be("taxNumber");
        }

        // Validacija - porez_broj  (PL-57: porez_broj maksimalno 13 cifara)
        [Fact]
        public async Task CreateAsync_ResidentWithTaxNumberLongerThan13Digits_ThrowsValidationException()
        {
            // Arrange
            var service = NewService(out _);
            var dto = ValidResidentDto();
            dto.TaxNumber = "12345678901234"; // 14 cifara

            // Act
            var act = async () => await service.CreateAsync(dto, "tester");

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        // Validacija - porez_broj  (PL-57: format mora biti tačno 13 cifara)
        [Fact]
        public async Task CreateAsync_ResidentWithShorterTaxNumber_ThrowsValidationException()
        {
            // Arrange
            var service = NewService(out _);
            var dto = ValidResidentDto();
            dto.TaxNumber = "12345"; // prekratko

            // Act
            var act = async () => await service.CreateAsync(dto, "tester");

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        // Validacija - porez_broj  (jedinstvenost poreznog broja)
        [Fact]
        public async Task CreateAsync_DuplicateResidentTaxNumber_ThrowsValidationException()
        {
            // Arrange
            var service = NewService(out _);
            await service.CreateAsync(ValidResidentDto(), "tester");

            // Act — drugi unos s istim poreznim brojem
            var act = async () => await service.CreateAsync(ValidResidentDto(), "tester");

            // Assert
            (await act.Should().ThrowAsync<ValidationException>())
                .Which.Field.Should().Be("taxNumber");
        }

        // GCC broj je numerički, a naziv se čuva uz odabrani broj.
        [Fact]
        public async Task CreateAsync_WithGccFreeText_PersistsGccNumberAndNameAsEntered()
        {
            // Arrange
            var service = NewService(out _);
            var dto = ValidResidentDto();
            dto.GccNumber = "123";
            dto.GccName = "Grupa Primjer";

            // Act
            var result = await service.CreateAsync(dto, "tester");

            // Assert
            result.GccNumber.Should().Be("123");
            result.GccName.Should().Be("Grupa Primjer");
        }

        // ── PL-58 - Nerezidentna pravna lica ───────────────────────────────────────

        // PL-58 - Nerezidentna pravna lica — pozitivan unos
        [Fact]
        public async Task CreateAsync_WithValidNonResident_PersistsWithFbaIdAndNoTaxNumber()
        {
            // Arrange
            var service = NewService(out var ctx);

            // Act
            var result = await service.CreateAsync(ValidNonResidentDto(), "tester");

            // Assert
            result.IsResident.Should().BeFalse();
            result.FbaId.Should().Be("1234567890");
            result.TaxNumber.Should().BeNull("porez_broj je neaktivan za nerezidente");
            ctx.LegalEntities.Should().ContainSingle(e => e.FbaId == "1234567890");
        }

        // Validacija - FBA_ID  (PL-58: FBA_ID obavezan za nerezidente)
        [Fact]
        public async Task CreateAsync_NonResidentWithoutFbaId_ThrowsValidationException()
        {
            // Arrange
            var service = NewService(out _);
            var dto = ValidNonResidentDto();
            dto.FbaId = null;

            // Act
            var act = async () => await service.CreateAsync(dto, "tester");

            // Assert
            (await act.Should().ThrowAsync<ValidationException>())
                .Which.Field.Should().Be("fbaId");
        }

        // Validacija - FBA_ID  (PL-58: FBA_ID maksimalno 10 cifara)
        [Fact]
        public async Task CreateAsync_NonResidentWithFbaIdLongerThan10Digits_ThrowsValidationException()
        {
            // Arrange
            var service = NewService(out _);
            var dto = ValidNonResidentDto();
            dto.FbaId = "12345678901"; // 11 cifara

            // Act
            var act = async () => await service.CreateAsync(dto, "tester");

            // Assert
            (await act.Should().ThrowAsync<ValidationException>())
                .Which.Field.Should().Be("fbaId");
        }

        // Validacija - FBA_ID  (PL-58: FBA_ID mora biti numerički)
        [Fact]
        public async Task CreateAsync_NonResidentWithNonNumericFbaId_ThrowsValidationException()
        {
            // Arrange
            var service = NewService(out _);
            var dto = ValidNonResidentDto();
            dto.FbaId = "ABC123";

            // Act
            var act = async () => await service.CreateAsync(dto, "tester");

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        // PL-58 - porez_broj neaktivan za nerezidente:
        // čak i ako se TaxNumber pošalje, servis ga ignoriše i sprema null.
        [Fact]
        public async Task CreateAsync_NonResidentWithTaxNumberProvided_IgnoresTaxNumber()
        {
            // Arrange
            var service = NewService(out _);
            var dto = ValidNonResidentDto();
            dto.TaxNumber = ValidTaxNumber; // ne smije se koristiti za nerezidente

            // Act
            var result = await service.CreateAsync(dto, "tester");

            // Assert
            result.TaxNumber.Should().BeNull();
            result.FbaId.Should().Be("1234567890");
        }
    }
}
