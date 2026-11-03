using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.Helpers.Constants;

namespace IntegrationTests.Tests.Users;

public abstract class RoleCatalogFixture : RoleRequestFixture
{
    protected override void ConfigureServices(IServiceCollection services)
    { base.ConfigureServices(services); services.AddScoped<RoleCatalogService>(); }

    protected async Task<RoleCatalogDTO> CreateCatalog(string name = "custom-reviewer")
    {
        Guid permission = Guid.Empty;
        await InDatabase(async db => permission = (await db.Permissions.SingleAsync(row => row.Code == ApplicationPermissions.AdministrationView)).Id);
        var response = await Client.PostAsJsonAsync("/api/role-catalog", new CreateRoleCatalogDTO(name, "Čitač kataloga", "Custom catalog fixture", [permission]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<RoleCatalogDTO>())!;
        var read = await Client.GetFromJsonAsync<RoleCatalogDTO>(response.Headers.Location!);
        Assert.Equal(created.Id, read!.Id); Assert.Equal(created.PermissionIds, read.PermissionIds);
        return created;
    }
}

public sealed class RoleCatalogHttpTests : RoleCatalogFixture
{
    [Fact]
    public async Task CustomDatabaseRoleCanBeRequestedApprovedAndDisabledWithoutProviderRoleChanges()
    {
        var role = await CreateCatalog(); using var applicant = NewClient("custom-applicant");
        var options = await applicant.GetFromJsonAsync<JsonElement>("/api/role-requests/options");
        Assert.Contains(options.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == role.Id && item.GetProperty("displayName").GetString() == role.DisplayName);
        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.GetAsync("/api/role-catalog")).StatusCode);
        var request = await Submit(applicant, roles: [role.Id]); var requestId = request.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/role-requests/{requestId}/approve", new { decisionId = Guid.NewGuid(), expectedVersion = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await applicant.GetAsync("/api/role-catalog")).StatusCode);
        // Read permission is not permission to mutate catalog/grants, even if body overposts a provider role.
        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO("Forged", null, false, 1, []))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO(role.DisplayName, role.Description, false, role.Version, role.PermissionIds))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.GetAsync("/api/role-catalog")).StatusCode);
        await InDatabase(async db =>
        {
            Assert.Equal(2, await db.AuditLogs.CountAsync(row => row.TableName == "Role"));
            Assert.False((await db.Roles.SingleAsync(row => row.Id == role.Id)).IsActive);
            Assert.Equal(1, await db.UserRoles.CountAsync(row => row.RoleId == role.Id)); // Disable, not destructive deletion.
        });
    }

    [Fact]
    public async Task TechnicalDefinitionsAndOwnAssignedRoleCannotBypassSelfOrFinalAdminProtection()
    {
        var role = await CreateCatalog(); Guid admin = Guid.Empty; Guid guest = Guid.Empty;
        await InDatabase(async db =>
        {
            admin = (await db.Roles.SingleAsync(row => row.Name == ApplicationAccessRoles.Administrator)).Id;
            guest = (await db.Roles.SingleAsync(row => row.Name == ApplicationAccessRoles.Guest)).Id;
            db.UserRoles.Add(new UserRole { UserId = (await db.AppUsers.SingleAsync(row => row.KeycloakId == "creator")).Id, RoleId = role.Id }); await db.SaveChangesAsync();
        });
        var edit = new UpdateRoleCatalogDTO("Changed", null, false, 1, []);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsJsonAsync($"/api/role-catalog/{admin}", edit)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsJsonAsync($"/api/role-catalog/{guest}", edit)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", edit)).StatusCode);
        await InDatabase(async db => { Assert.All(await db.Roles.ToListAsync(), row => Assert.True(row.IsActive)); Assert.Single(await db.AuditLogs.ToListAsync()); });
    }

    [Fact]
    public async Task CatalogValidatesNormalizedDuplicatesPermissionIdsAndStaleVersions()
    {
        var role = await CreateCatalog();
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsJsonAsync("/api/role-catalog", new CreateRoleCatalogDTO(" CUSTOM-REVIEWER ", "Duplicate", null, []))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync("/api/role-catalog", new CreateRoleCatalogDTO("admin", "Forbidden", null, []))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync("/api/role-catalog", new CreateRoleCatalogDTO("unknown-grant", "Unknown", null, [Guid.NewGuid()]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO("Invalid", null, true, 1, [role.PermissionIds[0], role.PermissionIds[0]]))).StatusCode);
        var first = await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO("Updated", null, true, 1, []));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(2, (await first.Content.ReadFromJsonAsync<RoleCatalogDTO>())!.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO("Stale", null, false, 1, []))).StatusCode);
        await InDatabase(async db => { Assert.Empty(await db.RolePermissions.Where(row => row.RoleId == role.Id).ToListAsync()); Assert.Equal(2, await db.AuditLogs.CountAsync()); });
    }
}
