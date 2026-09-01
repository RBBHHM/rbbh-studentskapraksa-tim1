using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Mocks.DB
{
    /// <summary>
    /// Helper za kreiranje izolovanog EF Core InMemory <see cref="ConnectedPartiesDbContext"/>.
    /// Svaki test dobija svježu bazu (jedinstveno ime) — testovi ostaju nezavisni.
    /// </summary>
    public static class InMemoryContextFactory
    {
        public static ConnectedPartiesDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ConnectedPartiesDbContext(options);
        }
    }
}
