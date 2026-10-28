using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.DL.Entities.LegalEntity;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Mocks.DB.Limiti
{
    public class LimitFixture
    {
        public readonly ConnectedPartiesDbContext _dbContext;
        public readonly int ValidLimitId;
        public readonly int ValidLimitId2;
        public readonly Guid ValidLegalEntityId;
        public readonly Guid ValidLegalEntityId2;
        public const string ValidTipLimita = "MM";
        public const string ValidTipLimita2 = "SATAFXS";
        public const string InactiveTipLimita = "NEAKTIVAN";

        public LimitFixture()
        {
            var options = new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ConnectedPartiesDbContext(options);

            var legalEntity1 = new LegalEntity
            {
                IsResident = true,
                TaxNumber = "4200000000099",
                MaticniBroj = "99999999",
                Name = "Test pravno lice d.o.o.",
                BasisOfConnection = "Test osnov",
                Status = "VERIFIED",
                CreatedBy = "test.user"
            };
            var legalEntity2 = new LegalEntity
            {
                IsResident = false,
                FbaId = "9001",
                Name = "Test Foreign Partner GmbH",
                BasisOfConnection = "Test osnov",
                Status = "VERIFIED",
                CreatedBy = "test.user"
            };
            _dbContext.LegalEntities.AddRange(legalEntity1, legalEntity2);

            _dbContext.CodeLists.AddRange(
                new CodeList { Kategorija = "VrstaLimita", Kod = ValidTipLimita, Naziv = "MM", Aktivan = true, KreiraoKorisnik = "test.user" },
                new CodeList { Kategorija = "VrstaLimita", Kod = ValidTipLimita2, Naziv = "SATA FX spot", Aktivan = true, KreiraoKorisnik = "test.user" },
                new CodeList { Kategorija = "VrstaLimita", Kod = InactiveTipLimita, Naziv = "Neaktivan tip", Aktivan = false, KreiraoKorisnik = "test.user" });

            _dbContext.SaveChanges();

            var limit1 = new Limit
            {
                LegalEntityId = legalEntity1.Id,
                TipLimita = ValidTipLimita,
                OdobreniLimit = 1_000_000m,
                MaksimalnaOcekivanaUtilizacija = 800_000m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test.user"
            };

            var limit2 = new Limit
            {
                LegalEntityId = legalEntity2.Id,
                TipLimita = ValidTipLimita2,
                OdobreniLimit = 500_000m,
                MaksimalnaOcekivanaUtilizacija = 450_000m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test.user"
            };

            _dbContext.Limiti.AddRange(limit1, limit2);
            _dbContext.SaveChanges();

            ValidLimitId = limit1.Id;
            ValidLimitId2 = limit2.Id;
            ValidLegalEntityId = legalEntity1.Id;
            ValidLegalEntityId2 = legalEntity2.Id;
        }
    }
}
