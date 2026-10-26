using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Users;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace IntegrationTests.Tests.Users;

// Real HTTP/controller/service/audit/DB authorization path. Only identity and
// unique InMemory state are fixtures; never backend Program/.env or SQL/SSO.
public sealed class UsersSafetyHttpTests : UsersHttpFixture
{
    [Fact]
    public async Task CreateWithNoExplicitRolesRetainsIdentityAndDeactivationRemovesVirtualGuest()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new CreateUserDTO
        {
            Username = "guest-fixture",
            FirstName = "Guest",
            LastName = "Fixture",
            Email = "guest-fixture@raiffeisengroup.ba",
            RoleIds = []
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<UserDTO>())!;
        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
            var user = await db.AppUsers.SingleAsync(item => item.Id == created.Id);
            Assert.True(user.IsActive); Assert.False(user.KeycloakSubjectConfirmed);
            Assert.False(await db.UserRoles.AnyAsync(mapping => mapping.UserId == created.Id));
            var principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("sub", "guest-fixture-subject"), new Claim(ClaimTypes.Email, user.Email)], "Fixture"));
            await new RoleClaimsTransformation(db).TransformAsync(principal);
            Assert.True(principal.IsInRole(ApplicationAccessRoles.Guest));
            Assert.False(principal.HasClaim("permission", ApplicationPermissions.AdministrationManage));
            Assert.Equal("CREATE", Assert.Single(await db.AuditLogs.ToListAsync()).Action);
        }
        Assert.Equal(HttpStatusCode.OK, (await Mutate(_client, created.Id, "deactivate")).StatusCode);
        await using var disabledScope = _app.Services.CreateAsyncScope();
        var disabledDb = disabledScope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var disabled = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "guest-fixture-subject"), new Claim(ClaimTypes.Email, created.Email)], "Fixture"));
        await new RoleClaimsTransformation(disabledDb).TransformAsync(disabled);
        Assert.False(disabled.IsInRole(ApplicationAccessRoles.Guest));
        Assert.Empty(disabled.FindAll("permission"));
        Assert.Equal(2, await disabledDb.AuditLogs.CountAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ExistingSelfProtectionRejectsLocalAndKeycloakIds(bool permanent, bool subjectRoute)
    {
        var id = await ListedOwnIdAsync();
        var response = await _client.DeleteAsync($"/api/users/{(subjectRoute ? Subject : id.ToString())}" + (permanent ? "/permanent" : ""));
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var own = await db.AppUsers.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id);
        var nextRequest = await _client.GetAsync("/api/users");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest && own?.IsActive == true,
            $"Expected 400 and preserved active self mapping; actual {(int)response.StatusCode}, " +
            $"exists={own is not null}, active={own?.IsActive}, next administration read={(int)nextRequest.StatusCode}.");
        Assert.Equal(HttpStatusCode.OK, nextRequest.StatusCode);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task LastAdminRoleRemovalIsRejectedForBothExistingRoutes(bool empty, bool postAlias)
    {
        var id = await ListedOwnIdAsync();
        var payload = new { roleIds = empty ? [] : new[] { await RoleId(ApplicationAccessRoles.PhysicalPersons) } };
        var response = postAlias ? await _client.PostAsJsonAsync($"/api/users/{id}/roles", payload)
            : await _client.PutAsJsonAsync($"/api/users/{id}/role", payload);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("najmanje jedan aktivni Admin", await response.Content.ReadAsStringAsync());
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.AppUsers.SingleAsync()).IsActive);
        Assert.True((await db.UserRoles.SingleAsync()).IsActive);
        Assert.Empty(await db.AuditLogs.ToListAsync());
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/users")).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AChangedSubjectCannotRebindTheConfirmedAdminByEmail(bool permanent)
    {
        // Authorization currently resolves DB grants by trusted authenticated
        // email. A differing subject must not bypass protection of that mapping.
        _client.DefaultRequestHeaders.Add("X-Test-Subject", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.DeleteAsync($"/api/users/{_localId}" + (permanent ? "/permanent" : ""))).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnmappedOidcSubjectAndEmailClaimsAlsoProtectSelf(bool permanent)
    {
        _client.DefaultRequestHeaders.Add("X-Test-Raw-Claims", "true");
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.DeleteAsync($"/api/users/{_localId}" + (permanent ? "/permanent" : ""))).StatusCode);
    }

    [Fact]
    public async Task KeepingAdminRoleAndReactivatingOrdinaryUserRemainAllowed()
    {
        var other = await AddUser(ApplicationAccessRoles.PhysicalPersons, "other@example.test");
        var adminId = await RoleId(ApplicationAccessRoles.Administrator);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/users/{other}/role", new { roleIds = new[] { adminId } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Mutate(_client, other, "deactivate")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/users/{other}/reactivate", new { })).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.AppUsers.SingleAsync(user => user.Id == other)).IsActive);
        Assert.Equal(new[] { "ROLE_ASSIGN", "DEACTIVATE", "REACTIVATE" }, (await db.AuditLogs.OrderBy(log => log.Timestamp).Select(log => log.Action).ToArrayAsync()));
    }

    [Fact]
    public async Task InvalidRolesAndMissingTargetsStillReturnExistingSafeResultsWithoutAudit()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync($"/api/users/{_localId}/role", new { roleIds = new[] { Guid.NewGuid() } })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Mutate(_client, Guid.NewGuid(), "delete")).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("deactivate")]
    [InlineData("delete")]
    public async Task NonSelfWriterCannotRemoveLastAdmin(string action)
    {
        await AddDatabaseOperator();
        _client.DefaultRequestHeaders.Add("X-Test-Email", "operator@example.test");
        Assert.Equal(HttpStatusCode.Conflict, (await Mutate(_client, _localId, action)).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.AppUsers.SingleAsync(user => user.Id == _localId)).IsActive);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("role", true)]
    [InlineData("empty", true)]
    [InlineData("deactivate", true)]
    [InlineData("delete", true)]
    [InlineData("role", false)]
    [InlineData("empty", false)]
    [InlineData("deactivate", false)]
    [InlineData("delete", false)]
    public async Task OtherAdminOrOrdinaryUserCanChangeAndAuditRemainsCorrect(string action, bool admin)
    {
        var id = await AddUser(admin ? ApplicationAccessRoles.Administrator : ApplicationAccessRoles.PhysicalPersons, "other@example.test");
        Assert.Equal(HttpStatusCode.OK, (await Mutate(_client, id, action)).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.AppUsers.SingleAsync(user => user.Id == _localId)).IsActive);
        var changed = await db.AppUsers.SingleOrDefaultAsync(user => user.Id == id);
        if (action == "delete") Assert.Null(changed);
        else if (action == "empty") { Assert.True(changed!.IsActive); Assert.False(await db.UserRoles.AnyAsync(mapping => mapping.UserId == id && mapping.IsActive)); }
        else if (action == "deactivate") Assert.False(changed!.IsActive);
        else
        {
            Assert.True(changed!.IsActive);
            Assert.False(await db.UserRoles.AnyAsync(mapping => mapping.UserId == id && mapping.IsActive && mapping.Role.Name == ApplicationAccessRoles.Administrator));
        }
        var audit = Assert.Single(await db.AuditLogs.ToListAsync());
        Assert.Equal("AppUser", audit.TableName);
        Assert.Equal(id.ToString(), audit.RecordId);
        Assert.Equal(Email, audit.Username);
        Assert.Equal(action switch { "role" => "ROLE_ASSIGN", "empty" => "ROLE_REMOVE", "deactivate" => "DEACTIVATE", _ => "DELETE" }, audit.Action);
        if (action == "role") Assert.Contains(ApplicationAccessRoles.LegalPersons, audit.NewValues);
        if (action == "deactivate") { Assert.Contains("true", audit.OldValues); Assert.Contains("false", audit.NewValues); }
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/users")).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InactiveUsersOrAssignmentsDoNotCountAsRemainingAdmins(bool inactiveUser)
    {
        var id = await AddUser(ApplicationAccessRoles.Administrator, "inactive@example.test");
        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
            if (inactiveUser) (await db.AppUsers.SingleAsync(user => user.Id == id)).IsActive = false;
            else (await db.UserRoles.SingleAsync(mapping => mapping.UserId == id)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await Mutate(_client, _localId, "empty")).StatusCode);
    }

    [Theory]
    [InlineData("clear", false)]
    [InlineData("clear", true)]
    [InlineData("add", false)]
    [InlineData("add", true)]
    [InlineData("replace-without-admin", false)]
    [InlineData("replace-without-admin", true)]
    [InlineData("keep-admin", false)]
    [InlineData("keep-admin", true)]
    public async Task UserCannotChangeOwnRolesEvenWhenAnotherAdminRemains(string change, bool postAlias)
    {
        await AddUser(ApplicationAccessRoles.Administrator, "other@example.test");
        var admin = await RoleId(ApplicationAccessRoles.Administrator);
        var legal = await RoleId(ApplicationAccessRoles.LegalPersons);
        var roleIds = change switch
        {
            "clear" => Array.Empty<Guid>(),
            "add" => new[] { admin, legal },
            "replace-without-admin" => new[] { legal },
            _ => new[] { admin }
        };
        var payload = new { roleIds };
        var response = postAlias ? await _client.PostAsJsonAsync($"/api/users/{_localId}/roles", payload)
            : await _client.PutAsJsonAsync($"/api/users/{_localId}/role", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.AppUsers.SingleAsync(user => user.Id == _localId)).IsActive);
        var mappings = await db.UserRoles.Where(mapping => mapping.UserId == _localId).ToListAsync();
        Assert.Equal(admin, Assert.Single(mappings).RoleId);
        Assert.True(mappings[0].IsActive);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task SelfRoleChangeIsDetectedByIdentityNotByRequestId()
    {
        // Same account addressed by its Keycloak id instead of the local id.
        await AddUser(ApplicationAccessRoles.Administrator, "other@example.test");
        var response = await _client.PutAsJsonAsync($"/api/users/{Subject}/role", new { roleIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentRoleRemovalCannotRemoveBothActiveAdmins()
    {
        var otherId = await AddUser(ApplicationAccessRoles.Administrator, "other@example.test");
        using var second = _app.GetTestClient();
        second.DefaultRequestHeaders.Add("X-Test-Email", "other@example.test");
        await using (var lookup = _app.Services.CreateAsyncScope())
            second.DefaultRequestHeaders.Add("X-Test-Subject", (await lookup.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>()
                .AppUsers.SingleAsync(user => user.Id == otherId)).KeycloakId);
        _client.DefaultRequestHeaders.Add("X-Test-Race", "true");
        second.DefaultRequestHeaders.Add("X-Test-Race", "true");
        var outcomes = await Task.WhenAll(Mutate(_client, otherId, "empty"), Mutate(second, _localId, "empty"));
        Assert.Single(outcomes, result => result.StatusCode == HttpStatusCode.OK);
        Assert.Single(outcomes, result => result.StatusCode == HttpStatusCode.Conflict);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.Equal(2, await db.AppUsers.CountAsync(user => user.IsActive)); // Losing admin remains an active virtual Guest.
        Assert.Single(await db.UserRoles.Where(mapping => mapping.IsActive && mapping.Role.Name == ApplicationAccessRoles.Administrator).ToListAsync());
        Assert.Single(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task UnverifiedEmailOfAnAdminCannotBindToThatAccount()
    {
        // Unknown subject + the admin's exact email, but not verified by the provider.
        _client.DefaultRequestHeaders.Add("X-Test-Subject", Guid.NewGuid().ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Unverified-Email", "true");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/users")).StatusCode);
    }

    [Fact]
    public async Task SubjectBindingWinsOverAForeignEmail()
    {
        // The stable subject of the admin account stays the admin even when the
        // token carries another (verified) address; email is not the primary key.
        _client.DefaultRequestHeaders.Add("X-Test-Email", "someone.else@example.test");
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/users")).StatusCode);
    }

    [Fact]
    public async Task UnmappedIdentityCannotAcquireAdministrationFromProviderClaims()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Subject", Guid.NewGuid().ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", "ordinary@example.test");
        _client.DefaultRequestHeaders.Add("X-Test-Forge", "true");
        Assert.Equal(HttpStatusCode.Forbidden, (await Mutate(_client, _localId, "empty")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Mutate(_client, _localId, "delete")).StatusCode);
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True(await db.AppUsers.AnyAsync(user => user.Id == _localId && user.IsActive));
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }
}
