using FluentAssertions;
using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using RBBH.ConnectedParties.Helpers.Validators;

namespace UnitTests.RelatedPersons
{
    /// <summary>
    /// Unit testovi za modul: Fizička lica i članovi porodice.
    ///
    /// Testira isključivo poslovnu logiku validatora i servisnih helpera
    /// bez pokretanja baze, Dockera ili HTTP pipeline-a.
    ///
    /// Pokrivenost:
    ///   REZ-*   — JMBG validacija za rezidente
    ///   NREZ-*  — identifikacija za nerezidente (pasoš / FBA ID)
    ///   DAT-*   — validacija datumskog perioda
    ///   FAM-*   — ValidateCanAddFamilyMember (izjava o nepostojanju)
    ///   CRUD-*  — logika kreiranja DTO-a (bez baze)
    /// </summary>
    public class UnitTests_FizickaLica_RelatedPersonValidator
    {
        // ─────────────────────────────────────────────────────────────────────
        // REZ-1 — Rezident: validan JMBG (13 cifara + ispravna kontrolna cifra)
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Rezident_ValidanJMBG_ValidacijaProdje()
        {
            // JMBG 2801984175000 — provjeren kontrolnom cifrom
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.Resident, "2801984175000", null, null);

            error.Should().BeNull("validan JMBG ne smije generisati grešku");
        }

        // ─────────────────────────────────────────────────────────────────────
        // REZ-2 — Rezident: prazan JMBG → greška
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Rezident_PrazanJMBG_VracaGresku()
        {
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.Resident, "", null, null);

            error.Should().NotBeNullOrEmpty("prazan JMBG mora biti odbijen");
            error!.ToLower().Should().Contain("jmbg");
        }

        // ─────────────────────────────────────────────────────────────────────
        // REZ-3 — Rezident: JMBG s manje od 13 cifara → greška
        // ─────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("123456789012")]    // 12 cifara
        [InlineData("1")]              // 1 cifra
        [InlineData("280197817500")]   // 12 cifara
        public void Rezident_JMBGPreko13Cifara_VracaGresku(string jmbgKratak)
        {
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.Resident, jmbgKratak, null, null);

            error.Should().NotBeNullOrEmpty($"JMBG '{jmbgKratak}' s manje od 13 cifara mora biti odbijen");
        }

        // ─────────────────────────────────────────────────────────────────────
        // REZ-4 — Rezident: JMBG sa slovima → greška
        // ─────────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("ABCDEFGHIJKLM")]
        [InlineData("2801978ABCDE5")]
        public void Rezident_JMBGSaSlovima_VracaGresku(string jmbgSlova)
        {
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.Resident, jmbgSlova, null, null);

            error.Should().NotBeNullOrEmpty($"JMBG sa slovima '{jmbgSlova}' mora biti odbijen");
        }

        // ─────────────────────────────────────────────────────────────────────
        // REZ-5 — Rezident: JMBG s 13 cifara ali neispravna kontrolna cifra → greška
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Rezident_JMBG13CifaraNeispravnaKontrolna_VracaGresku()
        {
            // 2801984175009 — 13 cifara ali pogrešna kontrolna cifra (ispravna je 5)
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.Resident, "2801984175009", null, null);

            error.Should().NotBeNullOrEmpty("JMBG s neispravnom kontrolnom cifrom mora biti odbijen");
            error!.ToLower().Should().Contain("kontrolna");
        }

        // ─────────────────────────────────────────────────────────────────────
        // REZ-6 — Rezident: JMBG null → greška
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Rezident_NullJMBG_VracaGresku()
        {
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.Resident, null, null, null);

            error.Should().NotBeNullOrEmpty("null JMBG mora biti odbijen za rezidenta");
        }

        // ─────────────────────────────────────────────────────────────────────
        // NREZ-1 — Nerezident: broj pasoša dostupan, FBA ID prazan → validno
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Nerezident_SamoPasosUnesen_ValidacijaProdje()
        {
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.NonResident, null, "BH123456", null);

            error.Should().BeNull("pasoš je dovoljan za nerezidenta");
        }

        // ─────────────────────────────────────────────────────────────────────
        // NREZ-2 — Nerezident: FBA ID dostupan, pasoš prazan → validno
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Nerezident_SamoFbaIdUnesen_ValidacijaProdje()
        {
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.NonResident, null, null, "1234567890");

            error.Should().BeNull("FBA ID je dovoljan za nerezidenta");
        }

        // ─────────────────────────────────────────────────────────────────────
        // NREZ-3 — Nerezident: ni pasoš ni FBA ID → greška
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Nerezident_BezPasosaIFbaId_VracaGresku()
        {
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.NonResident, null, null, null);

            error.Should().NotBeNullOrEmpty("nerezident bez pasoša i FBA ID mora biti odbijen");
            error!.ToLower().Should().Contain("pasoš");
        }

        // ─────────────────────────────────────────────────────────────────────
        // NREZ-4 — Nerezident: pasoš i FBA ID oba unesena → validno
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Nerezident_PasosIFbaIdObaUnesena_ValidacijaProdje()
        {
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.NonResident, null, "BH123456", "1234567890");

            error.Should().BeNull("oba identifikatora unesena je validno za nerezidenta");
        }

        // ─────────────────────────────────────────────────────────────────────
        // NREZ-5 — Nerezident: prazan pasoš (whitespace) i prazan FBA ID → greška
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Nerezident_WhitespacePasosIFbaId_VracaGresku()
        {
            var error = RelatedPersonValidator.ValidateIdentification(
                ResidencyType.NonResident, null, "   ", "   ");

            error.Should().NotBeNullOrEmpty("whitespace polja se tretiraju kao prazna");
        }

        // ─────────────────────────────────────────────────────────────────────
        // DAT-1 — Datum do ispred datuma od → greška
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Datumi_DatoDoPreDatumOd_VracaGresku()
        {
            var dateFrom = new DateTime(2026, 6, 15);
            var dateTo = new DateTime(2026, 6, 10); // prije od

            var error = RelatedPersonValidator.ValidateDateRange(dateFrom, dateTo);

            error.Should().NotBeNullOrEmpty("datum do ne može biti prije datuma od");
        }

        // ─────────────────────────────────────────────────────────────────────
        // DAT-2 — Datum do jednak datumu od → validno
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Datumi_DatoDoJednakoOd_ValidacijaProdje()
        {
            var datum = new DateTime(2026, 6, 15);

            var error = RelatedPersonValidator.ValidateDateRange(datum, datum);

            error.Should().BeNull("isti datum od i do je dozvoljen");
        }

        // ─────────────────────────────────────────────────────────────────────
        // DAT-3 — Datum do iza datuma od → validno
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Datumi_DatoDoIzaDatumOd_ValidacijaProdje()
        {
            var dateFrom = new DateTime(2026, 1, 1);
            var dateTo = new DateTime(2026, 12, 31);

            var error = RelatedPersonValidator.ValidateDateRange(dateFrom, dateTo);

            error.Should().BeNull("datum do iza datuma od je validan");
        }

        // ─────────────────────────────────────────────────────────────────────
        // DAT-4 — Null datumi → validno (datum može biti neobavezan)
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Datumi_ObaNullDatuma_ValidacijaProdje()
        {
            var error = RelatedPersonValidator.ValidateDateRange(null, null);

            error.Should().BeNull("null datumi ne trebaju generisati grešku na nivou range validacije");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-1 — Izjava = DA → nije moguće dodati člana porodice
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void IzjavaDA_NeDozvoljavaUnosClanaPorordice()
        {
            var relatedPerson = new RelatedPerson
            {
                DeclarationNoFamilyMembers = true
            };

            var error = RelatedPersonValidator.ValidateCanAddFamilyMember(relatedPerson);

            error.Should().NotBeNullOrEmpty("izjava DA mora blokirati unos člana porodice");
            error!.ToLower().Should().Contain("izjava");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-2 — Izjava = NE → dozvoljeno je dodati člana porodice
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void IzjavaNE_DozvoljavaUnosClanaPorordice()
        {
            var relatedPerson = new RelatedPerson
            {
                DeclarationNoFamilyMembers = false
            };

            var error = RelatedPersonValidator.ValidateCanAddFamilyMember(relatedPerson);

            error.Should().BeNull("izjava NE smije dozvoliti unos člana porodice");
        }

        // ─────────────────────────────────────────────────────────────────────
        // CRUD-1 — Validate(CreateRelatedPersonDTO) Rezident sa validnim JMBG → null
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ValidateCreateDto_RezidentValidanJMBG_VracaNull()
        {
            var dto = new CreateRelatedPersonDTO
            {
                FirstName = "Hana",
                LastName = "Kovač",
                Residency = ResidencyType.Resident,
                JMBG = "2801984175000",
                GCCNumber = "10001",
                GCCName = "Hana Kovač",
                RelationBasis = "Upravljačka povezanost",
                RelationDescription = "Opis povezanosti",
                SpecialRelationBasis = "NKF",
                IsIdentifiedStaff = true,
                DateFrom = new DateTime(2026, 1, 1),
                DateTo = new DateTime(2026, 12, 31)
            };

            var error = RelatedPersonValidator.Validate(dto);

            error.Should().BeNull("validan DTO ne smije vraćati grešku");
        }

        // ─────────────────────────────────────────────────────────────────────
        // CRUD-2 — Validate(CreateRelatedPersonDTO) Rezident bez JMBG → greška
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ValidateCreateDto_RezidentBezJMBG_VracaGresku()
        {
            var dto = new CreateRelatedPersonDTO
            {
                FirstName = "Hana",
                LastName = "Kovač",
                Residency = ResidencyType.Resident,
                JMBG = null,
                DateFrom = new DateTime(2026, 1, 1),
                DateTo = new DateTime(2026, 12, 31)
            };

            var error = RelatedPersonValidator.Validate(dto);

            error.Should().NotBeNullOrEmpty("rezident bez JMBG ne može biti snimljen");
        }

        // ─────────────────────────────────────────────────────────────────────
        // CRUD-3 — Validate(CreateRelatedPersonDTO) Nerezident sa pasošem → null
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ValidateCreateDto_NerezidentSaPasosom_VracaNull()
        {
            var dto = new CreateRelatedPersonDTO
            {
                FirstName = "John",
                LastName = "Doe",
                Residency = ResidencyType.NonResident,
                PassportNumber = "US999888777",
                FBAId = "123456",
                GCCNumber = "10002",
                GCCName = "John Doe",
                RelationBasis = "Vlasništvo",
                RelationDescription = "Opis povezanosti",
                SpecialRelationBasis = "B1",
                IsIdentifiedStaff = true,
                DateFrom = new DateTime(2026, 1, 1),
                DateTo = new DateTime(2026, 12, 31)
            };

            var error = RelatedPersonValidator.Validate(dto);

            error.Should().BeNull("nerezident sa pasošem je validan");
        }

        // ─────────────────────────────────────────────────────────────────────
        // CRUD-4 — Validate(CreateRelatedPersonDTO) Datum do prije od → greška
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ValidateCreateDto_DatoDoPreOd_VracaGresku()
        {
            var dto = new CreateRelatedPersonDTO
            {
                FirstName = "Hana",
                LastName = "Kovač",
                Residency = ResidencyType.Resident,
                JMBG = "2801984175000",
                GCCNumber = "10003",
                GCCName = "Hana Kovač",
                RelationBasis = "Upravljačka povezanost",
                RelationDescription = "Opis povezanosti",
                SpecialRelationBasis = "NKF",
                IsIdentifiedStaff = true,
                DateFrom = new DateTime(2026, 6, 15),
                DateTo = new DateTime(2026, 6, 10)
            };

            var error = RelatedPersonValidator.Validate(dto);

            error.Should().NotBeNullOrEmpty("datum do prije datuma od mora biti odbijen");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-CRUD-1 — Validate(CreateFamilyMemberDTO) Rezident s validnim JMBG → null
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ValidateFamilyMemberDto_RezidentValidanJMBG_VracaNull()
        {
            var dto = new CreateFamilyMemberDTO
            {
                FirstName = "Amira",
                LastName = "Kovač",
                Residency = ResidencyType.Resident,
                JMBG = "2801984175000"
            };

            var error = RelatedPersonValidator.Validate(dto);

            error.Should().BeNull("validan član porodice ne smije vraćati grešku");
        }

        // ─────────────────────────────────────────────────────────────────────
        // FAM-CRUD-2 — Validate(CreateFamilyMemberDTO) Nerezident bez pasoša i FBA → greška
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ValidateFamilyMemberDto_NerezidentBezIdentifikatora_VracaGresku()
        {
            var dto = new CreateFamilyMemberDTO
            {
                FirstName = "John",
                LastName = "Doe",
                Residency = ResidencyType.NonResident,
                JMBG = null,
                PassportNumber = null,
                FBAId = null
            };

            var error = RelatedPersonValidator.Validate(dto);

            error.Should().NotBeNullOrEmpty("nerezident bez identifikatora mora biti odbijen");
        }
    }
}
