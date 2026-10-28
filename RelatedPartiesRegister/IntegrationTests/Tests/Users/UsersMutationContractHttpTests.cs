using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace IntegrationTests.Tests.Users;

// Wire/persistence characterization added before moving controller operations.
public sealed class UsersMutationContractHttpTests : UsersHttpFixture
{
    [Fact]
    public async Task CancelledMutationNeverPersistsOrAudits()
    {
        var other = await AddUser(ApplicationAccessRoles.PhysicalPersons, "cancelled@example.test");
        await using var scope = _app.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IAppUserService>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.DeactivateAsync(
            other.ToString(), new ActorIdentity(Subject, Email, _localId), null, cancellation.Token));
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await db.AppUsers.SingleAsync(user => user.Id == other)).IsActive);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task PagingRetainsCaseInsensitiveRoleFilterAndInactivePermissionsContract()
    {
        var first = await AddUser(ApplicationAccessRoles.PhysicalPersons, "first@example.test");
        var second = await AddUser(ApplicationAccessRoles.PhysicalPersons, "second@example.test");
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var user = await db.AppUsers.SingleAsync(item => item.Id == first);
        user.LastName = "A"; user.IsActive = false;
        (await db.AppUsers.SingleAsync(item => item.Id == second)).LastName = "Z";
        await db.SaveChangesAsync();
        var page = await _client.GetFromJsonAsync<RBBH.ConnectedParties.DL.DTO.Users.GetUsersResponseDTO>("/api/users?role=fl&pageSize=1&page=1");
        Assert.Equal(2, page!.Total);
        var item = Assert.Single(page.Users);
        Assert.Equal(first, item.Id);
        Assert.Equal(new[] { ApplicationAccessRoles.PhysicalPersons }, item.Roles);
        Assert.Empty(item.EffectivePermissions);
        var next = await _client.GetFromJsonAsync<RBBH.ConnectedParties.DL.DTO.Users.GetUsersResponseDTO>("/api/users?role=fl&pageSize=1&page=2");
        var active = Assert.Single(next!.Users);
        Assert.Equal(second, active.Id);
        Assert.Contains(ApplicationPermissions.PhysicalPersonsEdit, active.EffectivePermissions);
        Assert.Contains(ApplicationPermissions.RegulatoryReportingView, active.EffectivePermissions);
        Assert.Equal(active.EffectivePermissions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal), active.EffectivePermissions);
    }

    [Fact]
    public async Task InvalidRolePrecedesMissingTarget()
    {
        var response = await _client.PutAsJsonAsync($"/api/users/{Guid.NewGuid()}/role", new { roleIds = new[] { Guid.NewGuid() } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Neispravna rola", body.GetProperty("title").GetString());
        Assert.Equal("Jedna ili više odabranih aplikacijskih rola nije ispravna.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task SelfDeactivationRetainsErrorsArrayContract()
    {
        var response = await _client.DeleteAsync($"/api/users/{_localId}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = Assert.Single(body.GetProperty("errors").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, error.GetProperty("field").ValueKind);
        Assert.Equal("Ne možete deaktivirati vlastiti nalog.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task FinalAdminConflictPrecedesSelfRoleRejection()
    {
        var response = await _client.PostAsJsonAsync($"/api/users/{_localId}/roles", new { roleIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Promjena nije dozvoljena", body.GetProperty("title").GetString());
        Assert.Equal("U sistemu mora ostati najmanje jedan aktivni Admin korisnik.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task RoleClearKeepsIdentityAndExactAuditMetadata()
    {
        var id = await AddUser(ApplicationAccessRoles.PhysicalPersons, "ordinary@example.test");
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var subject = (await db.AppUsers.SingleAsync(user => user.Id == id)).KeycloakId;
        var response = await _client.PutAsJsonAsync($"/api/users/{subject}/role", new { roleIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Role korisnika su uspješno ažurirane.", body.GetProperty("message").GetString());
        Assert.Empty(body.GetProperty("roles").EnumerateArray());
        Assert.True((await db.AppUsers.AsNoTracking().SingleAsync(user => user.Id == id)).IsActive);
        Assert.False(await db.UserRoles.AnyAsync(mapping => mapping.UserId == id));
        var audit = Assert.Single(await db.AuditLogs.AsNoTracking().ToListAsync());
        Assert.Equal("AppUser", audit.TableName);
        Assert.Equal(subject, audit.RecordId);
        Assert.Equal("ROLE_REMOVE", audit.Action);
        Assert.Equal(Email, audit.UserId);
        Assert.Equal(Email, audit.Username);
        Assert.Null(audit.OldValues);
        Assert.Equal("{\"Roles\":[]}", audit.NewValues);
    }

    [Fact]
    public async Task ReactivationKeepsLocalGuidLookupAndHistoricalAuditPayloads()
    {
        var id = await AddUser(ApplicationAccessRoles.PhysicalPersons, "reactivate@example.test");
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var subject = (await db.AppUsers.SingleAsync(user => user.Id == id)).KeycloakId;
        // A GUID-shaped subject is not a local-ID reactivation alias today.
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PostAsJsonAsync($"/api/users/{subject}/reactivate", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/users/{id}/reactivate", new { })).StatusCode);
        var audit = Assert.Single(await db.AuditLogs.AsNoTracking().ToListAsync());
        Assert.Equal(id.ToString(), audit.RecordId);
        Assert.Equal("REACTIVATE", audit.Action);
        Assert.Equal("{\"IsActive\":false}", audit.OldValues);
        Assert.Equal("{\"IsActive\":true}", audit.NewValues);
    }
}
