using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record ReleaseNoteEntry(string Tag, string Title, string Body,
    DateTimeOffset? PublishedAt, string Url, string Environment);
public sealed record ReleaseNoteEntries(IReadOnlyList<ReleaseNoteEntry> Items);

public interface IReleaseNotesApi
{
    Task<ReleaseNoteEntries> ListAsync(string environment = "uat", CancellationToken token = default);
}

public sealed class ReleaseNotesApi(IRbiApiTransport transport) : IReleaseNotesApi
{
    public async Task<ReleaseNoteEntries> ListAsync(string environment = "uat", CancellationToken token = default)
    {
        if (environment is not ("uat" or "prod")) throw new ArgumentOutOfRangeException(nameof(environment));
        var result = await transport.GetAsync<ReleaseNoteEntries>(
            $"api/release-notes?environment={environment}", token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess || result.Value.Items is null)
            throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}

internal sealed class UnconfiguredReleaseNotesApi : IReleaseNotesApi
{
    public Task<ReleaseNoteEntries> ListAsync(string environment = "uat", CancellationToken token = default) =>
        Task.FromException<ReleaseNoteEntries>(new RegistryApiException());
}
