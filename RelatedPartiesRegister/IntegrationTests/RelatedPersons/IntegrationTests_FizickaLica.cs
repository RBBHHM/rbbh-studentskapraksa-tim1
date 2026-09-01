using FluentAssertions;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Sdk;

namespace IntegrationTests.Tests.RelatedPersons
{
    /// <summary>
    /// Integration testovi za modul: Fizička lica i članovi porodice.
    ///
    /// Testira end-to-end kroz pravi SQL Server (Testcontainers) i pravi kontroler.
    /// Svaki test radi s čistom bazom (ResetAsync između testova).
    ///
    /// Pokrivenost:
    ///   FP-CREATE-*   — kreiranje fizičkog lica (CRUD: Create)
    ///   FP-READ-*     — čitanje i pretraga (CRUD: Read)
    ///   FP-DELETE-*   — brisanje (CRUD: Delete)
    ///   FAM-CREATE-*  — dodavanje člana porodice
    ///   FAM-TREE-*    — hijerarhijski prikaz
    ///   FAM-NEG-*     — negativni scenariji (izjava DA, nepostojeći zapisi)
    ///
    /// Napomena: Ako Docker nije dostupan, testovi se graciozno preskaču (SkippableFact).
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class IntegrationTests_FizickaLica
    {
        private readonly DatabaseFixture _fixture;
        private const string TestUser = "integration.tester";

        // Validni JMBG koji prođe kontrolnu cifru
        private const string ValidJMBG1 = "2801984175000";
        private const string ValidJMBG2 = "1501990175007";

        public IntegrationTests_FizickaLica(DatabaseFixture fixture) => _fixture = fixture;

        private RelatedPersonController NewController()
        {
            var ctx = _fixture.CreateContext();
            var emailService = new Mock<IEmailService>();
            var auditService = new Mock<IAuditService>();
            var emailSettings = Options.Create(new EmailSettings());
            return new RelatedPersonController(
                new RelatedPersonService(ctx, emailService.Object, emailSettings),
                auditService.Object
            ).WithHttpContext(TestUser);
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-CREATE-1 — Kreiranje rezidenta s validnim JMBG → 201
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpCreate1_RezidentValidanJmbg_Vraca201()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto(ValidJMBG1);

            var result = await controller.Create(dto);

            result.Result.Should().BeOfType<CreatedResult>(
                "validan rezident mora biti kreiran (201)");
            var created = (CreatedResult)result.Result!;
            var response = created.Value.Should().BeOfType<RelatedPersonResponseDTO>().Subject;
            response.Residency.Should().Be(ResidencyType.Resident);
            response.JMBG.Should().Be(ValidJMBG1);
            response.Status.Should().Be(RelatedPersonStatus.Draft, "novi zapis je uvijek Draft");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-CREATE-2 — Kreiranje rezidenta bez JMBG → 400
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpCreate2_RezidentBezJmbg_Vraca400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto(null!);

            var result = await controller.Create(dto);

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(400,
                "rezident bez JMBG mora biti odbijen (400)");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-CREATE-3 — Kreiranje rezidenta s JMBG kraćim od 13 cifara → 400
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpCreate3_RezidentJmbgKratak_Vraca400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto("123456");

            var result = await controller.Create(dto);

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(400,
                "JMBG kraći od 13 cifara mora biti odbijen");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-CREATE-4 — Kreiranje rezidenta s JMBG sa slovima → 400
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpCreate4_RezidentJmbgSaSlovima_Vraca400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto("2801978ABCDE5");

            var result = await controller.Create(dto);

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(400,
                "JMBG sa slovima mora biti odbijen");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-CREATE-5 — Nerezident bez FBA ID → 400
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpCreate5_NerezidentBezFbaId_Vraca400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = NerezidentDto(passportNumber: "BH123456", fbaId: null);

            var result = await controller.Create(dto);

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(400, "za nerezidenta su obavezni i pasoš i FBA ID");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-CREATE-6 — Nerezident bez pasoša → 400
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpCreate6_NerezidentBezPasosa_Vraca400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = NerezidentDto(passportNumber: null, fbaId: "9876543210");

            var result = await controller.Create(dto);

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(400, "za nerezidenta su obavezni i pasoš i FBA ID");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-CREATE-7 — Kreiranje nerezidenta bez pasoša i FBA ID → 400
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpCreate7_NerezidentBezIdentifikatora_Vraca400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = NerezidentDto(passportNumber: null, fbaId: null);

            var result = await controller.Create(dto);

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(400,
                "nerezident bez identifikatora mora biti odbijen");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-CREATE-8 — Datum do prije datuma od → 400
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpCreate8_DatoDoPreOd_Vraca400()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto(ValidJMBG1);
            dto.DateFrom = new DateTime(2026, 6, 15);
            dto.DateTo = new DateTime(2026, 6, 10); // prije od

            var result = await controller.Create(dto);

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(400,
                "datum do prije datuma od mora biti odbijen");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-READ-1 — GetAll vraća kreirani zapis
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpRead1_GetAll_VracaKreiraniZapis()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            await controller.Create(RezidentDto(ValidJMBG1));

            var result = await controller.GetAll();

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var lista = ok.Value.Should().BeAssignableTo<List<RelatedPersonSummaryDTO>>().Subject;
            lista.Should().HaveCount(1);
            lista[0].JMBG.Should().Be(ValidJMBG1);
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-READ-2 — GetById vraća tačne podatke
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpRead2_GetById_VracaTacneDetalje()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var created = ((CreatedResult)(await controller.Create(RezidentDto(ValidJMBG1))).Result!).Value
                as RelatedPersonResponseDTO;

            var result = await controller.GetById(created!.Id);

            var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = ok.Value.Should().BeOfType<RelatedPersonResponseDTO>().Subject;
            response.Id.Should().Be(created.Id);
            response.JMBG.Should().Be(ValidJMBG1);
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-READ-3 — GetById s nepostojećim ID → 404
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpRead3_GetByIdNepostojeci_Vraca404()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var result = await controller.GetById(Guid.NewGuid());

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(404,
                "nepostojeći ID mora vraćati 404");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-DELETE-1 — Delete briše zapis; GetAll više ne vraća obrisani zapis
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpDelete1_Delete_ZapisNijeVisibleUListi()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var created = ((CreatedResult)(await controller.Create(RezidentDto(ValidJMBG1))).Result!).Value
                as RelatedPersonResponseDTO;

            await controller.Delete(created!.Id);

            var listResult = await controller.GetAll();
            var lista = ((OkObjectResult)listResult.Result!).Value as List<RelatedPersonSummaryDTO>;
            lista.Should().BeEmpty("obrisani zapis ne smije biti vidljiv u listi");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FP-DELETE-2 — Delete nepostojećeg zapisa → 404
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FpDelete2_DeleteNepostojeci_Vraca404()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var result = await controller.Delete(Guid.NewGuid());

            result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(404,
                "brisanje nepostojećeg zapisa mora vraćati 404");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-CREATE-1 — Dodavanje člana porodice kad je izjava = NE → 201
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FamCreate1_IzjavaNE_DodajeClanaPorordice()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto(ValidJMBG1);
            dto.DeclarationNoFamilyMembers = false;
            var created = ((CreatedResult)(await controller.Create(dto)).Result!).Value
                as RelatedPersonResponseDTO;

            var famDto = FamilyMemberDto(ValidJMBG2);
            var result = await controller.AddFamilyMember(created!.Id, famDto);

            result.Result.Should().BeOfType<CreatedResult>(
                "dodavanje člana porodice (izjava NE) mora vraćati 201");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-CREATE-2 — Dodavanje člana porodice kad je izjava = DA → 400
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FamCreate2_IzjavaDA_BlokiraDodavanjeClana()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto(ValidJMBG1);
            dto.DeclarationNoFamilyMembers = true;
            var created = ((CreatedResult)(await controller.Create(dto)).Result!).Value
                as RelatedPersonResponseDTO;

            var famDto = FamilyMemberDto(ValidJMBG2);
            var result = await controller.AddFamilyMember(created!.Id, famDto);

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(400,
                "dodavanje člana porodice uz izjavu DA mora biti blokirano (400)");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-CREATE-3 — Dodavanje više članova porodice → svi se čuvaju
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FamCreate3_ViseClanova_SviSeCuvaju()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto(ValidJMBG1);
            dto.DeclarationNoFamilyMembers = false;
            var created = ((CreatedResult)(await controller.Create(dto)).Result!).Value
                as RelatedPersonResponseDTO;

            await controller.AddFamilyMember(created!.Id, FamilyMemberDto(ValidJMBG2));
            await controller.AddFamilyMember(created.Id, NerezidentFamilyMemberDto("BH555666"));

            var listResult = await controller.GetFamilyMembers(created.Id);
            var lista = ((OkObjectResult)listResult.Result!).Value as List<FamilyMemberResponseDTO>;
            lista.Should().HaveCount(2, "oba člana porodice moraju biti sačuvana");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-CREATE-4 — Dodavanje člana porodice za nepostojeće matično lice → 404
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FamCreate4_MaticnoNePronaseno_Vraca404()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var result = await controller.AddFamilyMember(Guid.NewGuid(), FamilyMemberDto(ValidJMBG1));

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(404,
                "dodavanje člana na nepostojeće matično lice mora vraćati 404");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-TREE-1 — GetFamilyTree vraća hijerarhijsku strukturu (roditelj + dijete)
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FamTree1_Hijerarhija_PrikazujePredakDijete()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto(ValidJMBG1);
            dto.DeclarationNoFamilyMembers = false;
            var created = ((CreatedResult)(await controller.Create(dto)).Result!).Value
                as RelatedPersonResponseDTO;

            // Dodaj roditeljski čvor
            var roditeljDto = FamilyMemberDto(ValidJMBG2);
            var roditeljResult = ((CreatedResult)(await controller.AddFamilyMember(created!.Id, roditeljDto)).Result!).Value
                as FamilyMemberResponseDTO;

            // Dodaj dijete pod roditeljem
            var dijeteDto = NerezidentFamilyMemberDto("BH777888");
            dijeteDto.ParentFamilyMemberId = roditeljResult!.Id;
            await controller.AddFamilyMember(created.Id, dijeteDto);

            var treeResult = await controller.GetFamilyTree(created.Id);
            var stablo = ((OkObjectResult)treeResult.Result!).Value as List<FamilyMemberResponseDTO>;

            stablo.Should().HaveCount(1, "stablo ima jednog korijenskog člana");
            stablo![0].Children.Should().HaveCount(1, "korijenski član ima jedno dijete");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-NEG-1 — Brisanje nepostojećeg člana porodice → 404
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FamNeg1_DeleteNepostojeci_Vraca404()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto(ValidJMBG1);
            dto.DeclarationNoFamilyMembers = false;
            var created = ((CreatedResult)(await controller.Create(dto)).Result!).Value
                as RelatedPersonResponseDTO;

            var result = await controller.DeleteFamilyMember(created!.Id, Guid.NewGuid());

            result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(404,
                "brisanje nepostojećeg člana mora vraćati 404");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-NEG-2 — Izjava DA sprečava dodavanje člana (API level — bez UI-a)
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FamNeg2_IzjaviDaApiLevel_BlokiraClanove()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto(ValidJMBG1);
            dto.DeclarationNoFamilyMembers = true;
            var created = ((CreatedResult)(await controller.Create(dto)).Result!).Value
                as RelatedPersonResponseDTO;

            // Pokušaj zaobići UI i direktno pozvati API
            var result = await controller.AddFamilyMember(created!.Id, FamilyMemberDto(ValidJMBG2));

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(400,
                "API mora blokirati unos čak i ako UI nije prisutan (direct API call)");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-NEG-3 — GetFamilyTree za nepostojeće matično lice → 404
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FamNeg3_GetTreeNepostojeci_Vraca404()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var result = await controller.GetFamilyTree(Guid.NewGuid());

            result.Result.Should().BeAssignableTo<ObjectResult>()
                .Which.StatusCode.Should().Be(404,
                "family tree nepostojećeg lica mora vraćati 404");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-CRUD-1 — Create → GetFamilyMembers → Delete → GetFamilyMembers (prazno)
        // Potvrđuje puni CRUD ciklus za članove porodice
        // ─────────────────────────────────────────────────────────────────────

        [SkippableFact]
        public async Task FamCrud1_PotpuniCiklus_CreateReadDelete()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);
            await _fixture.ResetAsync();

            var controller = NewController();
            var dto = RezidentDto(ValidJMBG1);
            dto.DeclarationNoFamilyMembers = false;
            var created = ((CreatedResult)(await controller.Create(dto)).Result!).Value
                as RelatedPersonResponseDTO;

            // Create
            var famCreated = ((CreatedResult)(await controller.AddFamilyMember(created!.Id, FamilyMemberDto(ValidJMBG2))).Result!).Value
                as FamilyMemberResponseDTO;

            // Read
            var listaBefore = ((OkObjectResult)(await controller.GetFamilyMembers(created.Id)).Result!).Value
                as List<FamilyMemberResponseDTO>;
            listaBefore.Should().HaveCount(1);

            // Delete
            await controller.DeleteFamilyMember(created.Id, famCreated!.Id);

            // Read after delete
            var listaAfter = ((OkObjectResult)(await controller.GetFamilyMembers(created.Id)).Result!).Value
                as List<FamilyMemberResponseDTO>;
            listaAfter.Should().BeEmpty("obrisani član porodice ne smije biti vidljiv");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper metode
        // ─────────────────────────────────────────────────────────────────────

        private static CreateRelatedPersonDTO RezidentDto(string? jmbg) => new()
        {
            FirstName = "Hana",
            LastName = "Kovač",
            Residency = ResidencyType.Resident,
            JMBG = jmbg,
            GCCNumber = "1001",
            GCCName = "Test GCC",
            RelationBasis = "TestOsnov",
            RelationDescription = "TestOsnov",
            SpecialRelationBasis = "B1",
            IsIdentifiedStaff = true,
            DateFrom = new DateTime(2026, 1, 1),
            DateTo = new DateTime(2026, 12, 31),
            DeclarationNoFamilyMembers = false
        };

        private static CreateRelatedPersonDTO NerezidentDto(string? passportNumber, string? fbaId) => new()
        {
            FirstName = "John",
            LastName = "Doe",
            Residency = ResidencyType.NonResident,
            PassportNumber = passportNumber,
            FBAId = fbaId,
            GCCNumber = "1002",
            GCCName = "Test GCC NR",
            RelationBasis = "TestOsnov",
            RelationDescription = "TestOsnov",
            SpecialRelationBasis = "NKF",
            IsIdentifiedStaff = true,
            DateFrom = new DateTime(2026, 1, 1),
            DateTo = new DateTime(2026, 12, 31),
            DeclarationNoFamilyMembers = false
        };

        private static CreateFamilyMemberDTO FamilyMemberDto(string jmbg) => new()
        {
            FirstName = "Amira",
            LastName = "Kovač",
            Residency = ResidencyType.Resident,
            JMBG = jmbg,
            RelationshipType = FamilyRelationshipType.Spouse
        };

        private static CreateFamilyMemberDTO NerezidentFamilyMemberDto(string passportNumber) => new()
        {
            FirstName = "Anna",
            LastName = "Smith",
            Residency = ResidencyType.NonResident,
            PassportNumber = passportNumber,
            RelationshipType = FamilyRelationshipType.Child
        };
    }
}
