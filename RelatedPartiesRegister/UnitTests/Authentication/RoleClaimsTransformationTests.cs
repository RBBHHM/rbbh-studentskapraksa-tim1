using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using UnitTests.Mocks.DB;

namespace UnitTests.Authentication;

public sealed class RoleClaimsTransformationTests
{
    [Fact]
    public async Task FreshDevelopmentDatabaseSeedsGuestPermissions()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.SeedAsync(db);
        var principal = Principal("new-user@example.com");

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.True(principal.HasClaim("permission", ApplicationPermissions.PhysicalPersonsView));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.PhysicalPersonsCreate));
    }

    [Fact]
    public async Task AuthenticatedUserWithoutDatabaseRecordGetsGuestBaseline()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        var principal = Principal("guest@example.com");

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.True(principal.IsInRole(ApplicationAccessRoles.Guest));
        Assert.True(principal.HasClaim("permission", ApplicationPermissions.LegalPersonsView));
        Assert.True(principal.HasClaim("permission", ApplicationPermissions.RegulatoryReportingExport));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.LegalPersonsCreate));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.AdministrationView));
    }

    [Fact]
    public async Task ExplicitRolesAddPermissionsToGuestBaseline()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "user@example.com", ApplicationAccessRoles.LegalPersons, ApplicationAccessRoles.Capital);
        var principal = Principal("user@example.com");

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.True(principal.HasClaim("permission", ApplicationPermissions.LegalPersonsEdit));
        Assert.True(principal.HasClaim("permission", ApplicationPermissions.CapitalDelete));
        Assert.True(principal.HasClaim("permission", ApplicationPermissions.PhysicalPersonsView));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.PhysicalPersonsEdit));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.AdministrationView));
    }

    [Fact]
    public async Task AdministratorGetsEveryPermission()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "admin@example.com", ApplicationAccessRoles.Administrator);
        var principal = Principal("admin@example.com");

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.All(ApplicationPermissions.All, permission =>
            Assert.True(principal.HasClaim("permission", permission)));
    }

    [Fact]
    public async Task UnauthenticatedUserDoesNotGetGuestAccess()
    {
        await using var db = InMemoryContextFactory.Create();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.Empty(principal.FindAll(ClaimTypes.Role));
        Assert.Empty(principal.FindAll("permission"));
    }

    [Fact]
    public async Task RemovingFinalExplicitRoleFallsBackToGuest()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "former-editor@example.com", ApplicationAccessRoles.LegalPersons);
        var user = await db.AppUsers.SingleAsync();
        db.UserRoles.RemoveRange(db.UserRoles.Where(item => item.UserId == user.Id));
        db.AppUsers.Remove(user);
        await db.SaveChangesAsync();
        var principal = Principal("former-editor@example.com");

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.True(principal.HasClaim("permission", ApplicationPermissions.LegalPersonsView));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.LegalPersonsEdit));
    }

    private static ClaimsPrincipal Principal(string email) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.Email, email)], "Test"));

    private static async Task AssignAsync(
        RBBH.ConnectedParties.DL.Persistence.ConnectedPartiesDbContext db,
        string email,
        params string[] roleNames)
    {
        var user = new AppUser { Email = email, Username = email, KeycloakId = Guid.NewGuid().ToString() };
        db.AppUsers.Add(user);
        foreach (var roleName in roleNames)
        {
            var role = await db.Roles.SingleAsync(item => item.Name == roleName);
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role });
        }
        await db.SaveChangesAsync();
    }
}