using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Mocks.DB.Limiti
{
    public class LimitFixture
    {
        public readonly ConnectedPartiesDbContext _dbContext;
        public readonly int ValidLimitId;
        public readonly int ValidLimitId2;

        public LimitFixture()
        {
            var options = new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ConnectedPartiesDbContext(options);

            var limit1 = new Limit
            {
                Naziv = "Regulatorni limit 1",
                TipLimita = "Regulatorni",
                IznosLimita = 1_000_000m,
                Utilizacija = 200_000m,
                RaspoloziviLimit = 800_000m,
                RegulatorniKapital = 5_000_000m,
                OsnovniKapital = 3_000_000m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test.user"
            };

            var limit2 = new Limit
            {
                Naziv = "Interni limit 2",
                TipLimita = "Interni",
                IznosLimita = 500_000m,
                Utilizacija = 50_000m,
                RaspoloziviLimit = 450_000m,
                RegulatorniKapital = 2_000_000m,
                OsnovniKapital = 1_000_000m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test.user"
            };

            _dbContext.Limiti.AddRange(limit1, limit2);
            _dbContext.SaveChanges();

            ValidLimitId = limit1.Id;
            ValidLimitId2 = limit2.Id;
        }
    }
}
