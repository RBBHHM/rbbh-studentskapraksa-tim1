using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Mocks.DB.Role
{
    public class RoleFixture
    {
        public readonly ConnectedPartiesDbContext _dbContext;
        public readonly Guid Role1Id = Guid.NewGuid();
        public readonly Guid Role2Id = Guid.NewGuid();
        public readonly Guid User1Id = Guid.NewGuid();
        public readonly Guid User2Id = Guid.NewGuid();
        public readonly Guid User3Id = Guid.NewGuid();

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
                Id = Guid.NewGuid(), UserId = User1Id, RoleId = Role1Id,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "system"
            });
            _dbContext.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(), UserId = User2Id, RoleId = Role1Id,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = "system"
            });

            _dbContext.SaveChanges();
        }
    }
}
