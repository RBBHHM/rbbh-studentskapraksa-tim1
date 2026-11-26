using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record RoleDefinition(long Id, string Name, string DisplayName, string? Description,
    bool IsActive, DateTime Version, IReadOnlyList<long> PermissionIds, bool IsProtected);
public sealed record RoleCatalogPage(IReadOnlyList<RoleDefinition> Items, int Total, int Page, int PageSize);
public sealed record RolePermissionOption(long Id, string Code, string Module, string? Description);
public sealed record RolePermissionOptions(IReadOnlyList<RolePermissionOption> Items);
public sealed record CreateRoleDefinition(string Name, string? DisplayName, string? Description, long[] PermissionIds);
public sealed record UpdateRoleDefinition(string? DisplayName, string? Description, bool IsActive,
    DateTime ExpectedVersion, long[] PermissionIds);

public interface IRoleCatalogApi
{
    Task<RoleCatalogPage> ListAsync(int page = 1, string search = "", CancellationToken token = default);
    Task<RolePermissionOptions> PermissionsAsync(CancellationToken token = default);
    Task<RoleDefinition> CreateAsync(CreateRoleDefinition definition, CancellationToken token = default);
    Task DeleteAsync(long id, DateTime expectedVersion, CancellationToken token = default);
    Task<RoleDefinition> UpdateAsync(long id, UpdateRoleDefinition definition, CancellationToken token = default);
}

public sealed class RoleCatalogApi(IRbiApiTransport transport) : IRoleCatalogApi
{
    public async Task<RoleCatalogPage> ListAsync(int page = 1, string search = "", CancellationToken token = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        var response = await transport.GetAsync<RoleCatalogPage>(
            $"api/role-catalog?page={page}&pageSize=20&search={Uri.EscapeDataString(search.Trim())}", token);
        return Value(response, token, page => page.Items is not null);
    }

    public async Task<RolePermissionOptions> PermissionsAsync(CancellationToken token = default) =>
        Value(await transport.GetAsync<RolePermissionOptions>("api/role-catalog/permissions", token), token,
            result => result.Items is not null);

    public async Task<RoleDefinition> CreateAsync(CreateRoleDefinition definition, CancellationToken token = default) =>
        Value(await transport.SendAsync<RoleDefinition>(HttpMethod.Post, "api/role-catalog", definition, token), token);

    public async Task<RoleDefinition> UpdateAsync(long id, UpdateRoleDefinition definition, CancellationToken token = default) =>
        Value(await transport.SendAsync<RoleDefinition>(HttpMethod.Put, $"api/role-catalog/{id:D}", definition, token), token);

    public async Task DeleteAsync(long id, DateTime expectedVersion, CancellationToken token = default)
    {
        var version = Uri.EscapeDataString(expectedVersion.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        var result = await transport.SendAsync(HttpMethod.Delete, $"api/role-catalog/{id:D}?expectedVersion={version}", cancellationToken: token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode, result.Error?.Code);
    }

    private static T Value<T>(RbiApiResult<T> result, CancellationToken token, Func<T, bool>? valid = null)
    {
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value is null || (valid is not null && !valid(result.Value)))
            throw new RegistryApiException(result.Error?.StatusCode, result.Error?.Code);
        return result.Value;
    }
}

internal sealed class UnconfiguredRoleCatalogApi : IRoleCatalogApi
{
    private static Task<T> Unavailable<T>() => Task.FromException<T>(new RegistryApiException());
    public Task<RoleCatalogPage> ListAsync(int page = 1, string search = "", CancellationToken token = default) => Unavailable<RoleCatalogPage>();
    public Task<RolePermissionOptions> PermissionsAsync(CancellationToken token = default) => Unavailable<RolePermissionOptions>();
    public Task<RoleDefinition> CreateAsync(CreateRoleDefinition definition, CancellationToken token = default) => Unavailable<RoleDefinition>();
    public Task DeleteAsync(long id, DateTime expectedVersion, CancellationToken token = default) => Unavailable<bool>();
    public Task<RoleDefinition> UpdateAsync(long id, UpdateRoleDefinition definition, CancellationToken token = default) => Unavailable<RoleDefinition>();
}
