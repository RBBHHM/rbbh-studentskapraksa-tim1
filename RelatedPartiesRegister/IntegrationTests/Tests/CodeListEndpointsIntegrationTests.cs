using FluentAssertions;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Sifarnici;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Tests
{
    /// <summary>
    /// Integration testovi za CodeList API (PL-36/37): CodeListController → CodeListService → EF Core → SQL Server.
    /// Pokriva pozitivne i negativne scenarije (200/201/400/404) i puni CRUD tok kroz pravu bazu.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class CodeListEndpointsIntegrationTests
    {
        private const string Kategorija = "VrstaLimita";
        private readonly DatabaseFixture _fixture;

        public CodeListEndpointsIntegrationTests(DatabaseFixture fixture) => _fixture = fixture;

        // Novi kontroler sa svježim kontekstom — simulira zaseban HTTP zahtjev.
        private CodeListController NewController()
        {
            var context = _fixture.CreateContext();
            return new CodeListController(new CodeListService(context), context).WithHttpContext();
        }

        private ReferenceDataController NewReferenceController()
        {
            var context = _fixture.CreateContext();
            return new ReferenceDataController(new CodeListService(context)).WithHttpContext();
        }

        [SkippableFact]
        public async Task Create_WithValidDto_Returns201AndPersistsToDatabase()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // Arrange
            var dto = new CreateCodeListDTO { Kategorija = Kategorija, Kod = "vl", Naziv = "Veliki limit" };

            // Act
            var result = await NewController().Create(dto);

            // Assert — HTTP 201 Created + normalizovan kod
            var created = result.Result.Should().BeOfType<CreatedResult>().Subject;
            var dtoOut = created.Value.Should().BeOfType<CodeListResponseDTO>().Subject;
            dtoOut.Kod.Should().Be("VL");

            // Provjera perzistencije u pravoj bazi (zaseban kontekst)
            await using var ctx = _fixture.CreateContext();
            ctx.CodeLists.Any(x => x.ID == dtoOut.ID && x.Naziv == "Veliki limit").Should().BeTrue();
        }

        [SkippableFact]
        public async Task Create_WithDuplicateKod_Returns400BadRequest()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // Arrange — prvi unos
            await NewController().Create(new CreateCodeListDTO { Kategorija = Kategorija, Kod = "VL", Naziv = "Prvi" });

            // Act — duplikat (isti Kod u istoj Kategoriji)
            var result = await NewController().Create(new CreateCodeListDTO { Kategorija = Kategorija, Kod = "VL", Naziv = "Drugi" });

            // Assert
            var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().BeOfType<ProblemDetails>().Which.Status.Should().Be(400);
        }

        [SkippableFact]
        public async Task GetByID_WhenNotFound_Returns404()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // Act
            var result = await NewController().GetByID(999999);

            // Assert
            var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFound.Value.Should().BeOfType<ProblemDetails>().Which.Status.Should().Be(404);
        }

        [SkippableFact]
        public async Task FullCrudFlow_CreateReadUpdateSoftDelete_WorksThroughDatabase()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // CREATE
            var createResult = await NewController().Create(
                new CreateCodeListDTO { Kategorija = Kategorija, Kod = "KR", Naziv = "Kratkoročni" });
            var id = ((CodeListResponseDTO)((CreatedResult)createResult.Result!).Value!).ID;

            // READ
            var getResult = await NewController().GetByID(id);
            var fetched = ((OkObjectResult)getResult.Result!).Value.Should().BeOfType<CodeListResponseDTO>().Subject;
            fetched.Kod.Should().Be("KR");

            // UPDATE vraća oblik koji direktno koristi frontend tabela.
            // ne CodeListResponseDTO. Zato samo provjeravamo HTTP 200, a efekat verifikujemo kroz bazu.
            var updateResult = await NewController().Update(id, new UpdateCodeListDTO { Naziv = "Kratkoročni limit" });
            updateResult.Result.Should().BeOfType<OkObjectResult>();

            // DELETE (soft-delete)
            var deleteResult = await NewController().Delete(id);
            deleteResult.Result.Should().BeOfType<OkObjectResult>();

            // Provjera u bazi: zapis i dalje postoji, naziv je ažuriran, a Aktivan = false.
            // Koristimo IgnoreQueryFilters() jer CodeList ima globalni filter Aktivan,
            // pa bi soft-obrisani (neaktivni) zapis inače bio odfiltriran.
            await using var ctx = _fixture.CreateContext();
            var row = await ctx.CodeLists.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.ID == id);
            row.Should().NotBeNull();
            row!.Naziv.Should().Be("Kratkoročni limit");
            row.IzmijenioKorisnik.Should().Be("integration-tester");
            row.Aktivan.Should().BeFalse();
        }

        [SkippableFact]
        public async Task GetDropdown_ReturnsOnlyActiveItems()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            // Arrange
            await NewController().Create(new CreateCodeListDTO { Kategorija = Kategorija, Kod = "A", Naziv = "Aktivan" });
            var bDto = ((CodeListResponseDTO)((CreatedResult)(await NewController()
                .Create(new CreateCodeListDTO { Kategorija = Kategorija, Kod = "B", Naziv = "Neaktivan" })).Result!).Value!);
            await NewController().Delete(bDto.ID); // soft-delete -> Aktivan = false

            // Act
            var result = await NewReferenceController().GetDropdown(Kategorija);

            // Assert
            var items = ((OkObjectResult)result.Result!).Value
                .Should().BeAssignableTo<List<CodeListDropdownDTO>>().Subject;
            items.Should().ContainSingle().Which.Kod.Should().Be("A");
        }
    }
}
