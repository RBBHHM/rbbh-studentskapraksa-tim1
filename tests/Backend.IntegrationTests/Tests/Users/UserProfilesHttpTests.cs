using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.DL.DTO.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;

namespace IntegrationTests.Tests.Users;

public sealed class UserProfilesHttpTests : UsersHttpFixture
{
    [Fact]
    public async Task ProfileEditsPreserveSubjectRolesAndActiveStateDespiteOverposting()
    {
        var id = await AddUser(ApplicationAccessRoles.PhysicalPersons, "old@example.test");
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var before = (await db.AppUsers.AsNoTracking().SingleAsync(user => user.Id == id)).KeycloakId;
        var response = await _client.PutAsJsonAsync($"/api/users/{id}", new
        {
            username = "updated-user", firstName = "Željko", lastName = "Fixture", email = "updated@other.example",
            keycloakId = "forged", isActive = false, roleIds = new[] { await RoleId(ApplicationAccessRoles.Administrator) }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = await db.AppUsers.AsNoTracking().SingleAsync(user => user.Id == id);
        Assert.Equal(before, row.KeycloakId); Assert.True(row.KeycloakSubjectConfirmed); Assert.True(row.IsActive);
        Assert.Equal("updated@other.example", row.Email); Assert.Equal("updated-user", row.Username);
        Assert.Equal(ApplicationAccessRoles.PhysicalPersons, (await db.UserRoles.Include(item => item.Role).SingleAsync(item => item.UserId == id)).Role.Name);
        Assert.Equal("UPDATE", (await db.AuditLogs.SingleAsync()).Action);
    }

    [Fact]
    public async Task DuplicateNormalizedProfileAndNonAdminEditAreDenied()
    {
        var id = await AddUser(ApplicationAccessRoles.PhysicalPersons, "original@example.test");
        var response = await _client.PutAsJsonAsync($"/api/users/{id}", new { username = "new-name", firstName = "Fixture", lastName = "User", email = "ADMIN-FIXTURE@example.test" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        _client.DefaultRequestHeaders.Add("X-Test-Subject", "unknown"); _client.DefaultRequestHeaders.Add("X-Test-Email", "unknown@example.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PutAsJsonAsync($"/api/users/{id}", new { username = "new-name", firstName = "Fixture", lastName = "User", email = "other@example.test" })).StatusCode);
    }

    [Fact]
    public async Task ValidExternalEmailIsAcceptedButAnUnconfirmedMappingDoesNotClaimPrivilegedEffectiveAccess()
    {
        var response = await _client.PostAsJsonAsync("/api/users", new CreateUserDTO
        { Username = "new-user", FirstName = "Fixture", LastName = "User", Email = "person@external.example", RoleIds = [await RoleId(ApplicationAccessRoles.LegalPersons)] });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var row = await response.Content.ReadFromJsonAsync<UserDTO>();
        Assert.False(row!.IdentityConfirmed); Assert.Contains(ApplicationAccessRoles.LegalPersons, row.Roles);
        Assert.DoesNotContain(ApplicationPermissions.LegalPersonsCreate, row.EffectivePermissions);
        Assert.Contains(ApplicationPermissions.LegalPersonsView, row.EffectivePermissions);
    }
}
