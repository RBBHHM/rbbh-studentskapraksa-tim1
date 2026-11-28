using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.Helpers.Constants;

namespace IntegrationTests.Tests.Users;

public abstract class RoleCatalogFixture : RoleRequestFixture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddScoped<RoleCatalogService>();
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddHttpClient<KeycloakAdminService>();
    }

    protected async Task<RoleCatalogDTO> CreateCatalog(string name = "custom-reviewer")
    {
        long permission = 0L;
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
    private static string DeleteUrl(RoleCatalogDTO role) => $"/api/role-catalog/{role.Id}?expectedVersion={role.Version:O}";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CustomAdministrationGrantsPermitReadsAndControlWrites(bool canManage)
    {
        long[] grants = [];
        await InDatabase(async db =>
            grants = await db.Permissions.Where(p => p.Code == ApplicationPermissions.AdministrationView ||
                (canManage && p.Code == ApplicationPermissions.AdministrationManage)).Select(p => p.Id).ToArrayAsync());
        var created = await Client.PostAsJsonAsync("/api/role-catalog", new CreateRoleCatalogDTO("custom-administration", null, null, grants));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var role = (await created.Content.ReadFromJsonAsync<RoleCatalogDTO>())!;
        using var custom = NewClient("custom-admin");
        var grantRequest = await Submit(custom, roles: [role.Id]);
        var grantRequestId = grantRequest.GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/role-requests/{grantRequestId}/approve",
            new { decisionId = Guid.NewGuid(), expectedVersion = 1 })).StatusCode);

        foreach (var url in new[] { "/api/users", "/api/role-catalog", "/api/audit-logs", "/api/role-requests" })
        {
            using var response = await custom.GetAsync(url);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{url} returned {response.StatusCode}");
        }

        var newRole = await custom.PostAsJsonAsync("/api/role-catalog", new CreateRoleCatalogDTO("managed-role", null, null, []));
        Assert.Equal(canManage ? HttpStatusCode.Created : HttpStatusCode.Forbidden, newRole.StatusCode);
        if (canManage)
            Assert.Equal(HttpStatusCode.Forbidden, (await custom.PutAsJsonAsync($"/api/role-catalog/{role.Id}",
                new UpdateRoleCatalogDTO(null, null, true, role.Version, []))).StatusCode);

        using var applicant = NewClient("new-applicant");
        var request = await Submit(applicant);
        var requestId = request.GetProperty("id").GetInt64();
        var decision = await custom.PostAsJsonAsync($"/api/role-requests/{requestId}/approve",
            new { decisionId = Guid.NewGuid(), expectedVersion = 1 });
        Assert.Equal(canManage ? HttpStatusCode.OK : HttpStatusCode.Forbidden, decision.StatusCode);
    }

    [Fact]
    public async Task RoleWithoutDisplayNameCanBeDeletedWithItsPermissionsAndAudit()
    {
        long permission = default;
        await InDatabase(async db => permission = (await db.Permissions.SingleAsync(row => row.Code == ApplicationPermissions.AdministrationView)).Id);
        var response = await Client.PostAsJsonAsync("/api/role-catalog", new { name = "to-remove", description = "Temporary", permissionIds = new[] { permission } });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var role = (await response.Content.ReadFromJsonAsync<RoleCatalogDTO>())!;
        Assert.Equal(role.Name, role.DisplayName);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.DeleteAsync(DeleteUrl(role with { Version = role.Version.AddSeconds(-1) }))).StatusCode);
        using var applicant = NewClient("custom-applicant");
        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.DeleteAsync(DeleteUrl(role))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync(DeleteUrl(role))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.DeleteAsync(DeleteUrl(role))).StatusCode);
        await InDatabase(async db =>
        {
            Assert.False(await db.Roles.AnyAsync(row => row.Id == role.Id));
            Assert.False(await db.RolePermissions.AnyAsync(row => row.RoleId == role.Id));
            Assert.Single(await db.AuditLogs.Where(row => row.Action == "ROLE_CATALOG_DELETE").ToListAsync());
        });
    }

    [Fact]
    public async Task CustomDatabaseRoleCanBeRequestedApprovedAndDisabledWithoutProviderRoleChanges()
    {
        var role = await CreateCatalog(); using var applicant = NewClient("custom-applicant");
        var options = await applicant.GetFromJsonAsync<JsonElement>("/api/role-requests/options");
        Assert.Contains(options.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetInt64() == role.Id && item.GetProperty("displayName").GetString() == role.DisplayName);
        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.GetAsync("/api/role-catalog")).StatusCode);
        var request = await Submit(applicant, roles: [role.Id]); var requestId = request.GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/role-requests/{requestId}/approve", new { decisionId = Guid.NewGuid(), expectedVersion = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await applicant.GetAsync("/api/role-catalog")).StatusCode);
        // Read permission is not permission to mutate catalog/grants, even if body overposts a provider role.
        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO("Forged", null, false, role.Version, []))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO(role.DisplayName, role.Description, false, role.Version, role.PermissionIds))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.GetAsync("/api/role-catalog")).StatusCode);
        var disabled = (await Client.GetFromJsonAsync<RoleCatalogDTO>($"/api/role-catalog/{role.Id}"))!;
        Assert.Equal(HttpStatusCode.Conflict, (await Client.DeleteAsync(DeleteUrl(disabled))).StatusCode);
        var plan = await Client.GetFromJsonAsync<RoleDeletionPlan>($"/api/role-catalog/{role.Id}/deletion");
        var user = Assert.Single(plan!.Users);
        var replacement = Assert.Single(plan.Targets, target => target.Name == ApplicationAccessRoles.LegalPersons);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsJsonAsync($"/api/role-catalog/{role.Id}/transfer",
            new TransferRoleAssignment(user.UserId, replacement.Id, disabled.Version))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync(DeleteUrl(disabled))).StatusCode);
        await InDatabase(async db =>
        {
            Assert.Equal(3, await db.AuditLogs.CountAsync(row => row.TableName == "Role"));
            Assert.False(await db.Roles.AnyAsync(row => row.Id == role.Id));
            Assert.True(await db.UserRoles.AnyAsync(row => row.RoleId == replacement.Id && row.UserId == user.UserId && row.IsActive));
            Assert.False(await db.Set<RBBH.ConnectedParties.DL.Entities.Authorization.RequestedRole>().AnyAsync(row => row.RoleId == role.Id));
        });
    }

    [Fact]
    public async Task TechnicalDefinitionsAndOwnAssignedRoleCannotBypassSelfOrFinalAdminProtection()
    {
        var role = await CreateCatalog(); long admin = 0L; long guest = 0L;
        await InDatabase(async db =>
        {
            admin = (await db.Roles.SingleAsync(row => row.Name == ApplicationAccessRoles.Administrator)).Id;
            guest = (await db.Roles.SingleAsync(row => row.Name == ApplicationAccessRoles.Guest)).Id;
            db.UserRoles.Add(new UserRole { UserId = (await db.AppUsers.SingleAsync(row => row.KeycloakId == "creator")).Id, RoleId = role.Id }); await db.SaveChangesAsync();
        });
        var owned = await Client.GetFromJsonAsync<RoleCatalogDTO>($"/api/role-catalog/{role.Id}");
        Assert.True(owned!.IsAssignedToCurrentUser);
        var edit = new UpdateRoleCatalogDTO("Changed", null, false, role.Version, []);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsJsonAsync($"/api/role-catalog/{admin}", edit)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsJsonAsync($"/api/role-catalog/{guest}", edit)).StatusCode);
        var selfEdit = await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", edit);
        Assert.Equal(HttpStatusCode.Forbidden, selfEdit.StatusCode);
        Assert.Equal("roles.self_management_denied", (await selfEdit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await InDatabase(async db => { Assert.All(await db.Roles.ToListAsync(), row => Assert.True(row.IsActive)); Assert.Single(await db.AuditLogs.ToListAsync()); });
    }

    [Fact]
    public async Task CatalogValidatesNormalizedDuplicatesPermissionIdsAndStaleVersions()
    {
        var role = await CreateCatalog();
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsJsonAsync("/api/role-catalog", new CreateRoleCatalogDTO(" CUSTOM-REVIEWER ", "Duplicate", null, []))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync("/api/role-catalog", new CreateRoleCatalogDTO("admin", "Forbidden", null, []))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync("/api/role-catalog", new CreateRoleCatalogDTO("unknown-grant", "Unknown", null, [Random.Shared.NextInt64(1L << 32, 1L << 50)]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO("Invalid", null, true, role.Version, [role.PermissionIds[0], role.PermissionIds[0]]))).StatusCode);
        var first = await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO("Updated", null, true, role.Version, []));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.True((await first.Content.ReadFromJsonAsync<RoleCatalogDTO>())!.Version > role.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO("Stale", null, false, role.Version, []))).StatusCode);
        await InDatabase(async db => { Assert.Empty(await db.RolePermissions.Where(row => row.RoleId == role.Id).ToListAsync()); Assert.Equal(2, await db.AuditLogs.CountAsync()); });
    }
}
