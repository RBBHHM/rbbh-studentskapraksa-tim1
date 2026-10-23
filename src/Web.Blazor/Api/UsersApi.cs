using System.Text.Json;
using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record ApplicationUser(Guid Id, string Username, string FirstName, string LastName, string Email,
    IReadOnlyList<string> Roles, IReadOnlyList<string> EffectivePermissions, bool IsActive);
public sealed record UsersPage(IReadOnlyList<ApplicationUser> Users, int Total, int Page, int PageSize);
public sealed record ApplicationRole(Guid Id, string Name, int UserCount);
public sealed record RolesCatalog(IReadOnlyList<ApplicationRole> Roles);
public sealed record CreateApplicationUser(string Username, string FirstName, string LastName, string Email,
    IReadOnlyList<Guid> RoleIds, bool IsActive = true);
public interface IUsersApi
{
    Task<UsersPage> ListAsync(int page = 1, CancellationToken token = default);
    Task<RolesCatalog> RolesAsync(CancellationToken token = default);
    Task<ApplicationUser> CreateAsync(CreateApplicationUser request, CancellationToken token = default);
    Task UpdateRolesAsync(Guid id, IReadOnlyList<Guid> roles, CancellationToken token = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken token = default);
    Task DeleteAsync(Guid id, CancellationToken token = default);
}
public sealed class UsersApi(IRbiApiTransport transport) : IUsersApi
{
    public async Task<UsersPage> ListAsync(int page = 1, CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        var result = await transport.GetAsync<UsersPage>($"api/users?page={page}&pageSize=20", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Users is null) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public async Task<RolesCatalog> RolesAsync(CancellationToken token = default)
    {
        var result = await transport.GetAsync<RolesCatalog>("api/roles", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Roles is null) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public async Task<ApplicationUser> CreateAsync(CreateApplicationUser request, CancellationToken token = default)
    {
        var result = await transport.SendAsync<ApplicationUser>(HttpMethod.Post, "api/users", request, token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public Task UpdateRolesAsync(Guid id, IReadOnlyList<Guid> roles, CancellationToken token = default) =>
        SendAsync(HttpMethod.Put, $"api/users/{id}/role", new { roleIds = roles }, token);
    public Task SetActiveAsync(Guid id, bool active, CancellationToken token = default) => active
        ? SendAsync(HttpMethod.Post, $"api/users/{id}/reactivate", new { }, token)
        : SendAsync(HttpMethod.Delete, $"api/users/{id}", null, token);
    public Task DeleteAsync(Guid id, CancellationToken token = default) => SendAsync(HttpMethod.Delete, $"api/users/{id}/permanent", null, token);
    private async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken token)
    {
        var result = await transport.SendAsync<JsonElement>(method, path, body, token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
    }
}
internal sealed class UnconfiguredUsersApi : IUsersApi
{
    public Task<UsersPage> ListAsync(int page = 1, CancellationToken token = default) => Task.FromException<UsersPage>(new RegistryApiException());
    public Task<RolesCatalog> RolesAsync(CancellationToken token = default) => Task.FromException<RolesCatalog>(new RegistryApiException());
    public Task<ApplicationUser> CreateAsync(CreateApplicationUser request, CancellationToken token = default) => Task.FromException<ApplicationUser>(new RegistryApiException());
    public Task UpdateRolesAsync(Guid id, IReadOnlyList<Guid> roles, CancellationToken token = default) => Task.FromException(new RegistryApiException());
    public Task SetActiveAsync(Guid id, bool active, CancellationToken token = default) => Task.FromException(new RegistryApiException());
    public Task DeleteAsync(Guid id, CancellationToken token = default) => Task.FromException(new RegistryApiException());
}
