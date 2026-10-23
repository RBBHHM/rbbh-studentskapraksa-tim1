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
    public async Task KnownActiveUserWithoutExplicitRoleGetsGuestBaseline()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "known@example.com");
        var principal = Principal("known@example.com");

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.True(principal.IsInRole(ApplicationAccessRoles.Guest));
        Assert.True(principal.HasClaim("permission", ApplicationPermissions.LegalPersonsView));
        Assert.True(principal.HasClaim("permission", ApplicationPermissions.RegulatoryReportingExport));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.LegalPersonsCreate));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.AdministrationView));
    }

    [Fact]
    public async Task AuthenticatedIdentityWithoutApplicationUserGetsNoBusinessAccess()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        var principal = Principal("stranger@example.com", subject: Guid.NewGuid().ToString());

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.False(principal.IsInRole(ApplicationAccessRoles.Guest));
        Assert.Empty(principal.FindAll("permission"));
    }

    [Fact]
    public async Task InactiveUserGetsNoAccessAndDoesNotFallBackToEmail()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "inactive@example.com", ApplicationAccessRoles.Administrator);
        var user = await db.AppUsers.SingleAsync();
        user.IsActive = false;
        await db.SaveChangesAsync();

        foreach (var principal in new[] { Principal("inactive@example.com"), Principal("inactive@example.com", subject: user.KeycloakId) })
        {
            await new RoleClaimsTransformation(db).TransformAsync(principal);
            Assert.Empty(principal.FindAll("permission"));
            Assert.False(principal.IsInRole(ApplicationAccessRoles.Guest));
        }
    }

    [Fact]
    public async Task SubjectIsThePrimaryBindingAndEmailCannotOverrideIt()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "admin@example.com", ApplicationAccessRoles.Administrator);
        await AssignAsync(db, "limits@example.com", ApplicationAccessRoles.Limits);
        var limits = await db.AppUsers.SingleAsync(user => user.Email == "limits@example.com");
        // The token subject belongs to the limits account; its verified email names the admin.
        var principal = Principal("admin@example.com", subject: limits.KeycloakId);

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.True(principal.HasClaim("permission", ApplicationPermissions.LimitsEdit));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.AdministrationManage));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("")]
    [InlineData("yes")]
    public async Task UnverifiedOrMalformedEmailVerificationNeverBindsByEmail(string verified)
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "admin@example.com", ApplicationAccessRoles.Administrator);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, "admin@example.com"), new Claim("email_verified", verified)], "Test"));

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.Empty(principal.FindAll("permission"));
        Assert.False(principal.IsInRole(ApplicationAccessRoles.Guest));
    }

    [Fact]
    public async Task MissingEmailVerificationClaimNeverBindsByEmail()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "admin@example.com", ApplicationAccessRoles.Administrator);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, "admin@example.com")], "Test"));

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.Empty(principal.FindAll("permission"));
    }

    [Fact]
    public async Task VerifiedEmailBootstrapUsesApplicationNormalization()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "mixed.case@example.com", ApplicationAccessRoles.Capital);
        var principal = Principal("  Mixed.Case@Example.com ");

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.True(principal.HasClaim("permission", ApplicationPermissions.CapitalEdit));
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
    public async Task RemovingFinalExplicitRoleOfAKnownUserFallsBackToGuest()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "former-editor@example.com", ApplicationAccessRoles.LegalPersons);
        var user = await db.AppUsers.SingleAsync();
        db.UserRoles.RemoveRange(db.UserRoles.Where(item => item.UserId == user.Id));
        await db.SaveChangesAsync();
        var principal = Principal("former-editor@example.com");

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.True(principal.HasClaim("permission", ApplicationPermissions.LegalPersonsView));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.LegalPersonsEdit));
    }

    [Fact]
    public async Task DeletedUserLosesAllAccess()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "deleted@example.com", ApplicationAccessRoles.LegalPersons);
        var user = await db.AppUsers.SingleAsync();
        db.UserRoles.RemoveRange(db.UserRoles.Where(item => item.UserId == user.Id));
        db.AppUsers.Remove(user);
        await db.SaveChangesAsync();
        var principal = Principal("deleted@example.com");

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.Empty(principal.FindAll("permission"));
    }

    [Fact]
    public async Task ProviderRolesPermissionsAndOldMarkerNeverGrantApplicationAccess()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        var principal = Principal("unassigned@example.com");
        var identity = (ClaimsIdentity)principal.Identity!;
        identity.AddClaim(new Claim(ClaimTypes.Role, ApplicationAccessRoles.Administrator));
        identity.AddClaim(new Claim("permission", ApplicationPermissions.CapitalCreate));
        identity.AddClaim(new Claim("rct_done", "1"));

        await new RoleClaimsTransformation(db).TransformAsync(principal);

        Assert.False(principal.IsInRole(ApplicationAccessRoles.Guest));
        Assert.False(principal.IsInRole(ApplicationAccessRoles.Administrator));
        Assert.False(principal.HasClaim("permission", ApplicationPermissions.CapitalCreate));
        Assert.Equal("unassigned@example.com", principal.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public async Task RepeatedTransformationRevokesRemovedDatabaseRoleWithoutAccumulatingIdentities()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "editor@example.com", ApplicationAccessRoles.Capital);
        var principal = Principal("editor@example.com");
        var transformation = new RoleClaimsTransformation(db);
        await transformation.TransformAsync(principal);
        Assert.True(principal.HasClaim("permission", ApplicationPermissions.CapitalCreate));
        var identities = principal.Identities.Count();
        db.UserRoles.RemoveRange(db.UserRoles);
        await db.SaveChangesAsync();

        await transformation.TransformAsync(principal);

        Assert.False(principal.HasClaim("permission", ApplicationPermissions.CapitalCreate));
        Assert.Equal(identities, principal.Identities.Count());
        Assert.True(principal.IsInRole(ApplicationAccessRoles.Guest));
    }

    [Fact]
    public async Task RepeatedTransformationRevokesEverythingWhenTheUserIsDeactivated()
    {
        await using var db = InMemoryContextFactory.Create();
        await DevelopmentDataSeeder.EnsureApplicationRolesAsync(db);
        await AssignAsync(db, "editor@example.com", ApplicationAccessRoles.Capital);
        var principal = Principal("editor@example.com");
        var transformation = new RoleClaimsTransformation(db);
        await transformation.TransformAsync(principal);
        (await db.AppUsers.SingleAsync()).IsActive = false;
        await db.SaveChangesAsync();

        await transformation.TransformAsync(principal);

        Assert.Empty(principal.FindAll("permission"));
        Assert.False(principal.IsInRole(ApplicationAccessRoles.Guest));
    }

    private static ClaimsPrincipal Principal(string email, string? subject = null) => new(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.Email, email), new Claim("email_verified", "true") }
            .Concat(subject is null ? [] : [new Claim("sub", subject)]), "Test"));

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
