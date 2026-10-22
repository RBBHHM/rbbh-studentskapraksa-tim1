using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record ApplicationProfile(string? UserId, string? FullName, string? Username,
    string? Email, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public interface IProfileApi
{
    Task<ApplicationProfile> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class ProfileApi(IRbiApiTransport transport) : IProfileApi
{
    public async Task<ApplicationProfile> GetAsync(CancellationToken cancellationToken = default)
    {
        var result = await transport.GetAsync<ApplicationProfile>("api/auth/profile", cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Roles is null || result.Value.Permissions is null)
            throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}

internal sealed class UnconfiguredProfileApi : IProfileApi
{
    public Task<ApplicationProfile> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<ApplicationProfile>(new RegistryApiException());
    }
}

// Never expose upstream ProblemDetails, exception messages or database errors.
public sealed class RegistryApiException : Exception
{
    public RegistryApiException(System.Net.HttpStatusCode? statusCode = null) : base("The application service is unavailable.") => StatusCode = statusCode;
    public System.Net.HttpStatusCode? StatusCode { get; }
    public string MessageKey => (int?)StatusCode switch
    {
        401 => "errors.unauthorized", 403 => "errors.forbidden", 409 => "errors.conflict",
        423 => "errors.locked", 400 or 422 => "errors.validation", _ => "errors.unavailable"
    };
}
