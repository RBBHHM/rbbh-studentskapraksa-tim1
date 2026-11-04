using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record RequestableRole(Guid Id, string Name, string? DisplayName = null);
public sealed record RequestableRoles(IReadOnlyList<RequestableRole> Items);
/// <summary>Workflow state is lowercase and server-owned: pending, approved or rejected.</summary>
public sealed record RoleRequest(Guid Id, string Purpose, string Status, IReadOnlyList<Guid> RoleIds, IReadOnlyList<string> RoleNames,
    string? Note, DateTime CreatedAt, DateTime? ProcessedAt, string? ProcessedBy, string? DecisionNote, int Version,
    string? RequesterUsername = null, string? RequesterEmail = null);
public sealed record RoleRequestsPage(IReadOnlyList<RoleRequest> Items, int Total, int Page, int PageSize);
public interface IRoleRequestsApi
{
    Task<RequestableRoles> OptionsAsync(CancellationToken token = default);
    Task<RoleRequest> SubmitAsync(Guid clientRequestId, string purpose, IReadOnlyList<Guid> roleIds, string? note, CancellationToken token = default);
    Task<RoleRequestsPage> MineAsync(int page = 1, string status = "", CancellationToken token = default);
    Task<RoleRequestsPage> ReviewAsync(int page = 1, string status = "", CancellationToken token = default);
    Task<RoleRequest> DecideAsync(RoleRequest request, bool approve, string? note, CancellationToken token = default);
}
public sealed class RoleRequestsApi(IRbiApiTransport transport) : IRoleRequestsApi
{
    public async Task<RequestableRoles> OptionsAsync(CancellationToken token = default)
    {
        var result = await transport.GetAsync<RequestableRoles>("api/role-requests/options", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Items is null) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public async Task<RoleRequest> SubmitAsync(Guid clientRequestId, string purpose, IReadOnlyList<Guid> roleIds, string? note, CancellationToken token = default)
    {
        var result = await transport.SendAsync<RoleRequest>(HttpMethod.Post, "api/role-requests", new { clientRequestId, purpose, roleIds, note }, token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    public Task<RoleRequestsPage> MineAsync(int page = 1, string status = "", CancellationToken token = default) => ListAsync("api/role-requests/mine", page, status, token);
    public Task<RoleRequestsPage> ReviewAsync(int page = 1, string status = "", CancellationToken token = default) => ListAsync("api/role-requests", page, status, token);
    private async Task<RoleRequestsPage> ListAsync(string path, int page, string status, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        var result = await transport.GetAsync<RoleRequestsPage>($"{path}?page={page}&pageSize=20&status={Uri.EscapeDataString(status)}", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Items is null) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    /// <summary>The decision id is derived from the request and version, so a repeated click replays the same final decision rather than granting twice.</summary>
    public async Task<RoleRequest> DecideAsync(RoleRequest request, bool approve, string? note, CancellationToken token = default)
    {
        var result = await transport.SendAsync<RoleRequest>(HttpMethod.Post, $"api/role-requests/{request.Id}/{(approve ? "approve" : "reject")}",
            new { decisionId = DecisionId(request, approve), expectedVersion = request.Version, note }, token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
    private static Guid DecisionId(RoleRequest request, bool approve) =>
        new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"{request.Id:N}:{request.Version}:{(approve ? "approve" : "reject")}")));
}
internal sealed class UnconfiguredRoleRequestsApi : IRoleRequestsApi
{
    public Task<RequestableRoles> OptionsAsync(CancellationToken token = default) => Task.FromException<RequestableRoles>(new RegistryApiException());
    public Task<RoleRequest> SubmitAsync(Guid clientRequestId, string purpose, IReadOnlyList<Guid> roleIds, string? note, CancellationToken token = default) => Task.FromException<RoleRequest>(new RegistryApiException());
    public Task<RoleRequestsPage> MineAsync(int page = 1, string status = "", CancellationToken token = default) => Task.FromException<RoleRequestsPage>(new RegistryApiException());
    public Task<RoleRequestsPage> ReviewAsync(int page = 1, string status = "", CancellationToken token = default) => Task.FromException<RoleRequestsPage>(new RegistryApiException());
    public Task<RoleRequest> DecideAsync(RoleRequest request, bool approve, string? note, CancellationToken token = default) => Task.FromException<RoleRequest>(new RegistryApiException());
}
