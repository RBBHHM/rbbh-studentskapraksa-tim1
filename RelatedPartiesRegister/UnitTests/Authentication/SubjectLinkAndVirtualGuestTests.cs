using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace UnitTests.Authentication;

public sealed class SubjectLinkAndVirtualGuestTests
{
    private static ConnectedPartiesDbContext Store(string name) => new(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseInMemoryDatabase(name).Options);
    private static ClaimsPrincipal Identity(string subject = "real-subject", string email = "person@example.test", bool verified = true) =>
        new(new ClaimsIdentity([new("sub", subject), new(ClaimTypes.Email, email), new("email_verified", verified.ToString())], "Fixture"));
    private static IConfiguration ApprovedLink() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    { ["IdentityBinding:AllowVerifiedEmailFirstLink"] = "true" }).Build();
    private static AppUser Legacy(string email = "person@example.test", bool active = true) => new()
    { Email = email, Username = Guid.NewGuid().ToString(), KeycloakId = Guid.NewGuid().ToString(), IsActive = active };

    [Fact]
    public async Task UnmappedAuthenticatedIdentityHasVirtualGuestWithoutStoredGuestRoleOrUser()
    {
        await using var db = Store(Guid.NewGuid().ToString()); var principal = Identity();
        await new RoleClaimsTransformation(db).TransformAsync(principal);
        Assert.True(principal.IsInRole("gost")); Assert.False(principal.IsInRole("admin"));
        Assert.Equal(RolePermissionMatrix.Resolve([]).Order(), principal.FindAll("permission").Select(claim => claim.Value).Order());
        Assert.Empty(db.AppUsers); Assert.Empty(db.Roles); Assert.Empty(db.UserRoles);
    }

    [Fact]
    public async Task LegacyPlaceholderIsNotAConfirmedSubjectAndDefaultConfigurationDoesNotBind()
    {
        await using var db = Store(Guid.NewGuid().ToString()); var user = Legacy(); db.AppUsers.Add(user); await db.SaveChangesAsync();
        var result = await new ApplicationIdentityResolver(db).ResolveAsync(Identity(user.KeycloakId));
        Assert.Null(result.User); Assert.False(result.Denied); Assert.False((await db.AppUsers.SingleAsync()).KeycloakSubjectConfirmed);
        Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task ApprovedFirstLinkPersistsActualSubjectAndNeverRebindsOnLaterEmailChange()
    {
        var name = Guid.NewGuid().ToString();
        await using (var db = Store(name))
        {
            db.AppUsers.Add(Legacy()); await db.SaveChangesAsync();
            var result = await new ApplicationIdentityResolver(db, ApprovedLink()).ResolveAsync(Identity(email: "  Person@Example.Test  "));
            Assert.False(result.Denied); Assert.True(result.User!.KeycloakSubjectConfirmed);
        }
        await using var read = Store(name); var saved = await read.AppUsers.SingleAsync();
        Assert.Equal("real-subject", saved.KeycloakId); Assert.NotNull(saved.KeycloakSubjectLinkedAt);
        var linkedAt = saved.KeycloakSubjectLinkedAt;
        var later = await new ApplicationIdentityResolver(read).ResolveAsync(Identity(email: "new-address@example.test", verified: false));
        Assert.Equal(saved.Id, later.User!.Id); Assert.False(later.Denied);
        Assert.Equal(linkedAt, later.User.KeycloakSubjectLinkedAt);
        Assert.Equal("SUBJECT_LINK", Assert.Single(await read.AuditLogs.ToListAsync()).Action);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task DisabledLegacyOrConfirmedAccountCannotRegainGuestViaEmail(bool confirmed, bool sameSubject)
    {
        await using var db = Store(Guid.NewGuid().ToString()); var user = Legacy(active: false);
        user.KeycloakSubjectConfirmed = confirmed; db.AppUsers.Add(user); await db.SaveChangesAsync();
        var principal = Identity(sameSubject ? user.KeycloakId : "another-subject");
        await new RoleClaimsTransformation(db, ApprovedLink()).TransformAsync(principal);
        Assert.False(principal.IsInRole("gost")); Assert.Empty(principal.FindAll("permission")); Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task VerifiedEmailCannotOverrideAnotherConfirmedSubject()
    {
        await using var db = Store(Guid.NewGuid().ToString()); var user = Legacy();
        user.KeycloakSubjectConfirmed = true; db.AppUsers.Add(user); await db.SaveChangesAsync();
        var result = await new ApplicationIdentityResolver(db, ApprovedLink()).ResolveAsync(Identity());
        Assert.False(result.Denied); Assert.Null(result.User); Assert.Equal(user.KeycloakId, (await db.AppUsers.SingleAsync()).KeycloakId); Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task AmbiguousActiveNormalizedEmailNeverLinksOrReceivesExplicitRoles()
    {
        await using var db = Store(Guid.NewGuid().ToString()); db.AppUsers.AddRange(Legacy(), Legacy(" PERSON@example.test ")); await db.SaveChangesAsync();
        var result = await new ApplicationIdentityResolver(db, ApprovedLink()).ResolveAsync(Identity());
        Assert.False(result.Denied); Assert.Null(result.User); Assert.All(db.AppUsers, user => Assert.False(user.KeycloakSubjectConfirmed)); Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task UnverifiedEmailNeverCreatesDurableSubjectLink()
    {
        await using var db = Store(Guid.NewGuid().ToString()); db.AppUsers.Add(Legacy()); await db.SaveChangesAsync();
        var result = await new ApplicationIdentityResolver(db, ApprovedLink()).ResolveAsync(Identity(verified: false));
        Assert.Null(result.User); Assert.False((await db.AppUsers.SingleAsync()).KeycloakSubjectConfirmed); Assert.Empty(db.AuditLogs);
    }

    [Fact]
    public async Task ConcurrentDifferentSubjectsCannotBothClaimTheSameApprovedIdentity()
    {
        var name = Guid.NewGuid().ToString(); await using var first = Store(name); await using var second = Store(name);
        first.AppUsers.Add(Legacy()); await first.SaveChangesAsync();
        var results = await Task.WhenAll(new ApplicationIdentityResolver(first, ApprovedLink()).ResolveAsync(Identity("subject-one")),
            new ApplicationIdentityResolver(second, ApprovedLink()).ResolveAsync(Identity("subject-two")));
        Assert.Single(results, result => !result.Denied && result.User is not null);
        Assert.Single(results, result => result.User is null); // Loser may only remain virtual Guest, never another user's role mapping.
        await using var read = Store(name); Assert.True((await read.AppUsers.SingleAsync()).KeycloakSubjectConfirmed); Assert.Single(read.AuditLogs);
    }
}
