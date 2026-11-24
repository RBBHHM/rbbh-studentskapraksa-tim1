using System.Text.Json;
using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record ApplicationUser(Guid Id, string Username, string FirstName, string LastName, string Email,
    IReadOnlyList<string> Roles, IReadOnlyList<string> EffectivePermissions, bool IsActive, bool IdentityConfirmed = false);
public sealed record UserCreateResult(ApplicationUser? User, string? ErrorKey = null);
public sealed record UsersPage(IReadOnlyList<ApplicationUser> Users, int Total, int Page, int PageSize);
public sealed record ApplicationRole(Guid Id, string Name, int UserCount);
public sealed record RolesCatalog(IReadOnlyList<ApplicationRole> Roles);
public sealed record CreateApplicationUser(string Username, string FirstName, string LastName, string Email,
    IReadOnlyList<Guid> RoleIds, bool IsActive = true);
public interface IUsersApi
{
    Task<UsersPage> ListAsync(int page = 1, CancellationToken token = default, string? search = null, string? role = null);
    Task<RolesCatalog> RolesAsync(CancellationToken token = default);
    Task<UserCreateResult> CreateAsync(CreateApplicationUser request, CancellationToken token = default);
    Task<string?> UpdateRolesAsync(Guid id, IReadOnlyList<Guid> roles, CancellationToken token = default);
    /// <summary>Profile fields only (username, names, e-mail); the backend never accepts subject, active state or roles here.</summary>
    Task<string?> UpdateProfileAsync(Guid id, string username, string firstName, string lastName, string email, CancellationToken token = default);
    Task SetActiveAsync(Guid id, bool active, CancellationToken token = default);
    Task DeleteAsync(Guid id, CancellationToken token = default);
}
public sealed class UsersApi(IRbiApiTransport transport) : IUsersApi
{
    public async Task<UsersPage> ListAsync(int page = 1, CancellationToken token = default, string? search = null, string? role = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        var url = $"api/users?page={page}&pageSize=20";
        if (!string.IsNullOrWhiteSpace(search)) url += "&search=" + Uri.EscapeDataString(search.Trim());
        if (!string.IsNullOrWhiteSpace(role)) url += "&role=" + Uri.EscapeDataString(role.Trim());
        var result = await transport.GetAsync<UsersPage>(url, token);
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
    public async Task<UserCreateResult> CreateAsync(CreateApplicationUser request, CancellationToken token = default)
    {
        var result = await transport.SendAsync<ApplicationUser>(HttpMethod.Post, "api/users", request, token);
        token.ThrowIfCancellationRequested();
        return result.IsSuccess ? new(result.Value) : new(null, MutationError(result.Error));
    }
    public Task<string?> UpdateRolesAsync(Guid id, IReadOnlyList<Guid> roles, CancellationToken token = default) =>
        SaveAsync(HttpMethod.Put, $"api/users/{id}/role", new { roleIds = roles }, token);
    public Task<string?> UpdateProfileAsync(Guid id, string username, string firstName, string lastName, string email, CancellationToken token = default) =>
        SaveAsync(HttpMethod.Put, $"api/users/{id}", new { username, firstName, lastName, email }, token);

    private async Task<string?> SaveAsync(HttpMethod method, string path, object body, CancellationToken token)
    {
        var result = await transport.SendAsync(method, path, body, token);
        token.ThrowIfCancellationRequested();
        return result.IsSuccess ? null : MutationError(result.Error);
    }

    private static string MutationError(RbiApiError? error) =>
        error is { StatusCode: System.Net.HttpStatusCode.Conflict, Code: null }
            ? "users.safety-conflict"
            : new RegistryApiException(error?.StatusCode, error?.Code).MessageKey;

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
    public Task<UsersPage> ListAsync(int page = 1, CancellationToken token = default, string? search = null, string? role = null) => Task.FromException<UsersPage>(new RegistryApiException());
    public Task<RolesCatalog> RolesAsync(CancellationToken token = default) => Task.FromException<RolesCatalog>(new RegistryApiException());
    public Task<UserCreateResult> CreateAsync(CreateApplicationUser request, CancellationToken token = default) => Task.FromResult(new UserCreateResult(null, "errors.unavailable"));
    public Task<string?> UpdateRolesAsync(Guid id, IReadOnlyList<Guid> roles, CancellationToken token = default) => Task.FromResult<string?>("errors.unavailable");
    public Task<string?> UpdateProfileAsync(Guid id, string username, string firstName, string lastName, string email, CancellationToken token = default) => Task.FromResult<string?>("errors.unavailable");
    public Task SetActiveAsync(Guid id, bool active, CancellationToken token = default) => Task.FromException(new RegistryApiException());
    public Task DeleteAsync(Guid id, CancellationToken token = default) => Task.FromException(new RegistryApiException());
}
