using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record RequestableRole(Guid Id, string Name, string? DisplayName = null);
public sealed record RequestableRoles(IReadOnlyList<RequestableRole> Items);
/// <summary>Workflow state is lowercase and server-owned: pending, approved or rejected.</summary>
public sealed record RoleRequest(Guid Id, string Purpose, string Status, IReadOnlyList<Guid> RoleIds, IReadOnlyList<string> RoleNames,
    string? Note, DateTime CreatedAt, DateTime? ProcessedAt, string? ProcessedBy, string? DecisionNote, int Version,
    string? RequesterUsername = null, string? RequesterEmail = null);
public sealed record RoleRequestsPage(IReadOnlyList<RoleRequest> Items, int Total, int Page, int PageSize);
public sealed record RoleRequestMutation(RoleRequest? Request, string? ErrorKey = null)
{
    internal static RoleRequestMutation From(RbiApiResult<RoleRequest> result) => result.IsSuccess
        ? new(result.Value)
        : new(null, result.Error?.Code switch
        {
            "role_request.pending_exists" => "roles.pending-exists",
            "role_request.self_decision_denied" => "roles.error-self",
            "role_request.approver_unavailable" => "roles.error-no-reviewer",
            "role_request.already_processed" => "roles.error-processed",
            "role_request.invalid_roles" => "roles.error-invalid-roles",
            "request.concurrent_change" => "roles.error-stale",
            "request.busy" => "roles.error-busy",
            "request.not_found" => "roles.error-not-found",
            "identity.inactive" => "roles.error-inactive",
            "identity.reconciliation_required" => "roles.error-reconciliation",
            "users.final_admin" => "roles.error-final-admin",
            _ => new RegistryApiException(result.Error?.StatusCode, result.Error?.Code).MessageKey
        });
}

public interface IRoleRequestsApi
{
    Task<RequestableRoles> OptionsAsync(CancellationToken token = default);
    Task<RoleRequestMutation> SubmitAsync(Guid clientRequestId, string purpose, IReadOnlyList<Guid> roleIds, string? note, CancellationToken token = default);
    Task<RoleRequestsPage> MineAsync(int page = 1, string status = "", CancellationToken token = default, Guid? requestId = null);
    Task<RoleRequestsPage> ReviewAsync(int page = 1, string status = "", CancellationToken token = default, Guid? requestId = null);
    Task<RoleRequestMutation> DecideAsync(RoleRequest request, bool approve, string? note, CancellationToken token = default);
}
public sealed class RoleRequestsApi(IRbiApiTransport transport) : IRoleRequestsApi
{
    public async Task<RequestableRoles> OptionsAsync(CancellationToken token = default)
    {
        var result = await transport.GetAsync<RequestableRoles>("api/role-requests/options", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Items is null) throw new RegistryApiException(result.Error?.StatusCode, result.Error?.Code);
        return result.Value;
    }
    public async Task<RoleRequestMutation> SubmitAsync(Guid clientRequestId, string purpose, IReadOnlyList<Guid> roleIds, string? note, CancellationToken token = default)
    {
        var result = await transport.SendAsync<RoleRequest>(HttpMethod.Post, "api/role-requests", new { clientRequestId, purpose, roleIds, note }, token);
        token.ThrowIfCancellationRequested();
        return RoleRequestMutation.From(result);
    }
    public Task<RoleRequestsPage> MineAsync(int page = 1, string status = "", CancellationToken token = default, Guid? requestId = null) => ListAsync("api/role-requests/mine", page, status, token, requestId);
    public Task<RoleRequestsPage> ReviewAsync(int page = 1, string status = "", CancellationToken token = default, Guid? requestId = null) => ListAsync("api/role-requests", page, status, token, requestId);
    private async Task<RoleRequestsPage> ListAsync(string path, int page, string status, CancellationToken token, Guid? requestId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        var url = $"{path}?page={page}&pageSize=20&status={Uri.EscapeDataString(status)}";
        if (requestId is { } id) url += $"&requestId={id:D}";
        var result = await transport.GetAsync<RoleRequestsPage>(url, token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Items is null) throw new RegistryApiException(result.Error?.StatusCode, result.Error?.Code);
        return result.Value;
    }
    /// <summary>The decision id is derived from the request and version, so a repeated click replays the same final decision rather than granting twice.</summary>
    public async Task<RoleRequestMutation> DecideAsync(RoleRequest request, bool approve, string? note, CancellationToken token = default)
    {
        var result = await transport.SendAsync<RoleRequest>(HttpMethod.Post, $"api/role-requests/{request.Id}/{(approve ? "approve" : "reject")}",
            new { decisionId = DecisionId(request, approve), expectedVersion = request.Version, note }, token);
        token.ThrowIfCancellationRequested();
        return RoleRequestMutation.From(result);
    }
    private static Guid DecisionId(RoleRequest request, bool approve) =>
        new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"{request.Id:N}:{request.Version}:{(approve ? "approve" : "reject")}")));
}
internal sealed class UnconfiguredRoleRequestsApi : IRoleRequestsApi
{
    public Task<RequestableRoles> OptionsAsync(CancellationToken token = default) => Task.FromException<RequestableRoles>(new RegistryApiException());
    public Task<RoleRequestMutation> SubmitAsync(Guid clientRequestId, string purpose, IReadOnlyList<Guid> roleIds, string? note, CancellationToken token = default) => Task.FromResult(new RoleRequestMutation(null, "errors.unavailable"));
    public Task<RoleRequestsPage> MineAsync(int page = 1, string status = "", CancellationToken token = default, Guid? requestId = null) => Task.FromException<RoleRequestsPage>(new RegistryApiException());
    public Task<RoleRequestsPage> ReviewAsync(int page = 1, string status = "", CancellationToken token = default, Guid? requestId = null) => Task.FromException<RoleRequestsPage>(new RegistryApiException());
    public Task<RoleRequestMutation> DecideAsync(RoleRequest request, bool approve, string? note, CancellationToken token = default) => Task.FromResult(new RoleRequestMutation(null, "errors.unavailable"));
}
