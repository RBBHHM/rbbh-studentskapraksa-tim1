using FluentAssertions;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using UnitTests.Mocks.DB;

namespace UnitTests.Services.RoleServiceTests
{
    public class GetAllRolesTests
    {
        private static Role NewRole(Guid id, string name, bool isActive) => new()
        {
            Id = id,
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        private static UserRole NewUserRole(Guid roleId, bool isActive) => new()
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            UserId = Guid.NewGuid(),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        [Fact]
        public async Task GetAllRoles_WithActiveRoles_ReturnsOnlyActiveRoles()
        {
            // Arrange
            var activeId = Guid.NewGuid();
            var inactiveId = Guid.NewGuid();
            using var ctx = InMemoryContextFactory.Create();
            ctx.Roles.Add(NewRole(activeId, ApplicationAccessRoles.PhysicalPersons, isActive: true));
            ctx.Roles.Add(NewRole(inactiveId, "StaraRola", isActive: false));
            await ctx.SaveChangesAsync();

            var service = new RoleService(ctx);

            // Act
            var result = await service.GetAllRoles();

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value!.Roles.Should().ContainSingle()
                .Which.Name.Should().Be(ApplicationAccessRoles.PhysicalPersons);
        }

        [Fact]
        public async Task GetAllRoles_CountsOnlyActiveUserRoles()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            using var ctx = InMemoryContextFactory.Create();
            ctx.Roles.Add(NewRole(roleId, ApplicationAccessRoles.Limits, isActive: true));
            ctx.UserRoles.Add(NewUserRole(roleId, isActive: true));
            ctx.UserRoles.Add(NewUserRole(roleId, isActive: true));
            ctx.UserRoles.Add(NewUserRole(roleId, isActive: false)); // ne smije se brojati
            await ctx.SaveChangesAsync();

            var service = new RoleService(ctx);

            // Act
            var result = await service.GetAllRoles();

            // Assert
            result.Value!.Roles.Should().ContainSingle()
                .Which.UserCount.Should().Be(2);
        }

        [Fact]
        public async Task GetAllRoles_WithNoActiveRoles_ReturnsEmptyList()
        {
            // Arrange
            using var ctx = InMemoryContextFactory.Create();
            var service = new RoleService(ctx);

            // Act
            var result = await service.GetAllRoles();

            // Assert
            result.IsSuccessful.Should().BeTrue();
            result.Value!.Roles.Should().BeEmpty();
        }
    }
}
