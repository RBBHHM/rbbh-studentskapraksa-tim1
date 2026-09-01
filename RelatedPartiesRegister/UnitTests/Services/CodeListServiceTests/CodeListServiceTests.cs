using FluentAssertions;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Sifarnici;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.DL.Persistence;
using UnitTests.Mocks.DB;

namespace UnitTests.Services.CodeListServiceTests
{
    /// <summary>
    /// Testovi za <see cref="CodeListService"/> (PL-36 / PL-37).
    /// Napomena: za Delete koristimo kategoriju različitu od "TipRola" jer
    /// "TipRola" grana poziva ExecuteSqlRaw koju EF InMemory provider ne podržava
    /// (to je integraciona, a ne unit putanja).
    /// </summary>
    public class CodeListServiceTests
    {
        private const string Kategorija = "VrstaLimita";

        private static CodeList NewEntity(string kod, string naziv, bool aktivan = true, int? redoslijed = null) => new()
        {
            Kategorija = Kategorija,
            Kod = kod,
            Naziv = naziv,
            Aktivan = aktivan,
            RedoslijedPrikaza = redoslijed,
            KreiranDatum = DateTime.UtcNow,
            KreiraoKorisnik = "seed"
        };

        // ─── Create ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_WithValidDto_ReturnsSuccessAndPersistsEntity()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new CodeListService(ctx);
            var dto = new CreateCodeListDTO { Kategorija = Kategorija, Kod = "vl", Naziv = "Veliki limit" };

            // Act
            var result = await service.Create(dto, "tester");

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value!.Kod.Should().Be("VL"); // kod se normalizuje na uppercase
            result.Value.Naziv.Should().Be("Veliki limit");
            result.Value.KreiraoKorisnik.Should().Be("tester");
            ctx.CodeLists.Should().ContainSingle();
        }

        [Theory]
        [InlineData("", "VL", "Naziv")]
        [InlineData("Kat", "", "Naziv")]
        [InlineData("Kat", "VL", "")]
        public async Task Create_WithMissingRequiredField_ReturnsValidationError(string kat, string kod, string naziv)
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new CodeListService(ctx);
            var dto = new CreateCodeListDTO { Kategorija = kat, Kod = kod, Naziv = naziv };

            // Act
            var result = await service.Create(dto, "tester");

            // Assert
            result.IsValidationError.Should().BeTrue();
        }

        [Fact]
        public async Task Create_WithDuplicateKodInCategory_ReturnsValidationError()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            ctx.CodeLists.Add(NewEntity("VL", "Postojeći"));
            await ctx.SaveChangesAsync();

            var service = new CodeListService(ctx);
            var dto = new CreateCodeListDTO { Kategorija = Kategorija, Kod = "VL", Naziv = "Drugi" };

            // Act
            var result = await service.Create(dto, "tester");

            // Assert
            result.IsValidationError.Should().BeTrue();
            result.ExceptionMessage.Should().Contain("već postoji");
        }

        // ─── Update ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Update_WithValidData_UpdatesNazivAndAuditFields()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var entity = NewEntity("VL", "Stari naziv");
            ctx.CodeLists.Add(entity);
            await ctx.SaveChangesAsync();

            var service = new CodeListService(ctx);
            var dto = new UpdateCodeListDTO { Naziv = "Novi naziv", Opis = "opis" };

            // Act
            var result = await service.Update(entity.ID, dto, "editor");

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value!.Naziv.Should().Be("Novi naziv");
            result.Value.IzmijenioKorisnik.Should().Be("editor");
            result.Value.IzmijenjenDatum.Should().NotBeNull();
        }

        [Fact]
        public async Task Update_WithNonExistentId_ReturnsNotFoundError()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new CodeListService(ctx);
            var dto = new UpdateCodeListDTO { Naziv = "Naziv" };

            // Act
            var result = await service.Update(9999, dto, "editor");

            // Assert
            result.IsNotFoundError.Should().BeTrue();
        }

        [Fact]
        public async Task Update_WithInvalidId_ReturnsValidationError()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new CodeListService(ctx);
            var dto = new UpdateCodeListDTO { Naziv = "Naziv" };

            // Act
            var result = await service.Update(0, dto, "editor");

            // Assert
            result.IsValidationError.Should().BeTrue();
        }

        [Fact]
        public async Task Update_WithEmptyNaziv_ReturnsValidationError()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var entity = NewEntity("VL", "Stari");
            ctx.CodeLists.Add(entity);
            await ctx.SaveChangesAsync();

            var service = new CodeListService(ctx);
            var dto = new UpdateCodeListDTO { Naziv = "   " };

            // Act
            var result = await service.Update(entity.ID, dto, "editor");

            // Assert
            result.IsValidationError.Should().BeTrue();
        }

        // ─── Delete (soft-delete) ────────────────────────────────────────────────

        [Fact]
        public async Task Delete_WhenNotInUse_SoftDeletesAndReturnsSuccess()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var entity = NewEntity("VL", "Za brisanje");
            ctx.CodeLists.Add(entity);
            await ctx.SaveChangesAsync();

            var service = new CodeListService(ctx);

            // Act
            var result = await service.Delete(entity.ID, "remover");

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Should().BeTrue();

            var reloaded = await ctx.CodeLists.FindAsync(entity.ID);
            reloaded!.Aktivan.Should().BeFalse(); // soft-delete, zapis i dalje postoji
            reloaded.IzmijenioKorisnik.Should().Be("remover");
        }

        [Fact]
        public async Task Delete_WithNonExistentId_ReturnsNotFoundError()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new CodeListService(ctx);

            // Act
            var result = await service.Delete(9999, "remover");

            // Assert
            result.IsNotFoundError.Should().BeTrue();
        }

        [Fact]
        public async Task Delete_WithInvalidId_ReturnsValidationError()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new CodeListService(ctx);

            // Act
            var result = await service.Delete(0, "remover");

            // Assert
            result.IsValidationError.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteCategory_WhenValuesAreNotInUse_SoftDeletesDefinitionAndValues()
        {
            using var ctx = InMemoryContextFactory.Create();
            var definition = new CodeListDefinition { Name = Kategorija, CreatedBy = "seed" };
            var first = NewEntity("A", "Prva vrijednost");
            var second = NewEntity("B", "Druga vrijednost");
            ctx.CodeListDefinitions.Add(definition);
            ctx.CodeLists.AddRange(first, second);
            await ctx.SaveChangesAsync();

            var result = await new CodeListService(ctx).DeleteCategory(Kategorija, "remover");

            result.IsSuccessful.Should().BeTrue();
            definition.IsActive.Should().BeFalse();
            first.Aktivan.Should().BeFalse();
            second.Aktivan.Should().BeFalse();
            first.IzmijenioKorisnik.Should().Be("remover");
        }

        [Fact]
        public async Task DeleteCategory_WhenAValueIsReferencedByBusinessData_ReturnsValidationErrorWithoutChanges()
        {
            using var ctx = InMemoryContextFactory.Create();
            var definition = new CodeListDefinition { Name = Kategorija, CreatedBy = "seed" };
            var value = NewEntity("A", "Vrijednost u upotrebi");
            ctx.CodeListDefinitions.Add(definition);
            ctx.CodeLists.Add(value);
            ctx.Limiti.Add(new Limit
            {
                Naziv = "Testni limit",
                TipLimita = value.Kod,
                CreatedBy = "seed"
            });
            await ctx.SaveChangesAsync();

            var result = await new CodeListService(ctx).DeleteCategory(Kategorija, "remover");

            result.IsValidationError.Should().BeTrue();
            result.ExceptionMessage.Should().Contain("ne može obrisati");
            definition.IsActive.Should().BeTrue();
            value.Aktivan.Should().BeTrue();
        }

        // ─── Read ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByID_WithExistingId_ReturnsItem()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var entity = NewEntity("VL", "Naziv");
            ctx.CodeLists.Add(entity);
            await ctx.SaveChangesAsync();

            var service = new CodeListService(ctx);

            // Act
            var result = await service.GetByID(entity.ID);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value!.Kod.Should().Be("VL");
        }

        [Fact]
        public async Task GetByID_WithNonExistentId_ReturnsNotFoundError()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new CodeListService(ctx);

            // Act
            var result = await service.GetByID(12345);

            // Assert
            result.IsNotFoundError.Should().BeTrue();
        }

        [Fact]
        public async Task GetByID_WithInvalidId_ReturnsValidationError()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new CodeListService(ctx);

            // Act
            var result = await service.GetByID(0);

            // Assert
            result.IsValidationError.Should().BeTrue();
        }

        [Fact]
        public async Task GetDropdownByKategorija_ReturnsOnlyActiveItems()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            ctx.CodeLists.Add(NewEntity("A", "Aktivan", aktivan: true));
            ctx.CodeLists.Add(NewEntity("N", "Neaktivan", aktivan: false));
            await ctx.SaveChangesAsync();

            var service = new CodeListService(ctx);

            // Act
            var result = await service.GetDropdownByKategorija(Kategorija);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Should().ContainSingle().Which.Kod.Should().Be("A");
        }

        [Fact]
        public async Task GetDropdownByKategorija_WithEmptyKategorija_ReturnsValidationError()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new CodeListService(ctx);

            // Act
            var result = await service.GetDropdownByKategorija("  ");

            // Assert
            result.IsValidationError.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllByKategorija_ReturnsActiveAndInactiveItemsForAdministration()
        {
            // Administratorski pregled mora zadržati i soft-deaktivirane vrijednosti.
            // Padajući meniji i dalje koriste GetDropdownByKategorija i prikazuju samo aktivne.

            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            ctx.CodeLists.Add(NewEntity("A", "Aktivan", aktivan: true));
            ctx.CodeLists.Add(NewEntity("N", "Neaktivan", aktivan: false));
            await ctx.SaveChangesAsync();

            var service = new CodeListService(ctx);

            // Act
            var result = await service.GetAllByKategorija(Kategorija);

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            result.Value.Should().Contain(item => item.Kod == "A" && item.Aktivan);
            result.Value.Should().Contain(item => item.Kod == "N" && !item.Aktivan);
        }

        [Fact]
        public async Task GetAllByKategorija_WithEmptyKategorija_ReturnsValidationError()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new CodeListService(ctx);

            // Act
            var result = await service.GetAllByKategorija("");

            // Assert
            result.IsValidationError.Should().BeTrue();
        }
    }
}
