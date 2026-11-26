using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Mocks.DB.Role
{
    public class RoleFixture
    {
        public readonly ConnectedPartiesDbContext _dbContext;
        public readonly long Role1Id = Random.Shared.NextInt64(1L << 32, 1L << 50);
        public readonly long Role2Id = Random.Shared.NextInt64(1L << 32, 1L << 50);
        public readonly long User1Id = Random.Shared.NextInt64(1L << 32, 1L << 50);
        public readonly long User2Id = Random.Shared.NextInt64(1L << 32, 1L << 50);
        public readonly long User3Id = Random.Shared.NextInt64(1L << 32, 1L << 50);

        public RoleFixture()
        {
            var options = new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ConnectedPartiesDbContext(options);

            // Seed roles
            _dbContext.Roles.Add(new RBBH.ConnectedParties.DL.Entities.Role.Role
            {
                Id = Role1Id, Name = "Compliance", IsActive = true,
                CreatedAt = DateTime.UtcNow, CreatedBy = "system"
            });
            _dbContext.Roles.Add(new RBBH.ConnectedParties.DL.Entities.Role.Role
            {
                Id = Role2Id, Name = "Market Risk", IsActive = true,
                CreatedAt = DateTime.UtcNow, CreatedBy = "system"
            });

            // Seed user roles - Role1 has 2 users
            _dbContext.UserRoles.Add(new UserRole
            {
                Id = Random.Shared.NextInt64(1L << 32, 1L << 50), UserId = User1Id, RoleId = Role1Id,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "system"
            });
            _dbContext.UserRoles.Add(new UserRole
            {
                Id = Random.Shared.NextInt64(1L << 32, 1L << 50), UserId = User2Id, RoleId = Role1Id,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "system"
            });

            _dbContext.SaveChanges();
        }
    }
}
