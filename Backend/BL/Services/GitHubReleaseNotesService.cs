using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using RBBH.ConnectedParties.Exceptions;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Reads GitHub Releases from the fixed public project repository. No credential,
/// client-supplied URL, external markdown renderer, local script or generated file.
/// The server owns the GitHub request, bounds the result, and caches successful reads.
/// </summary>
public sealed class GitHubReleaseNotesService(HttpClient client, IMemoryCache cache)
{
    private const string ReleasesUrl = "https://api.github.com/repos/RBBHHM/rbbh-studentskapraksa-tim1/releases?per_page=50";
    private const string ReleaseLinkPrefix = "https://github.com/RBBHHM/rbbh-studentskapraksa-tim1/releases/";
    private const string CacheKey = "rbbh.public-releases.v1";

    public async Task<ReleaseNotesResult> GetAsync(string environment, CancellationToken cancellationToken)
    {
        if (environment is not ("uat" or "prod"))
            throw new ArgumentOutOfRangeException(nameof(environment));

        var releases = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await ReadAsync(cancellationToken);
        }) ?? [];
        return new(releases.Where(item => item.Environment == environment).ToArray());
    }

    private async Task<IReadOnlyList<PublicReleaseNote>> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RBBH-RelatedPartiesRegister", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode) throw Unavailable();

            // A public GitHub release list is small. Do not buffer an unbounded
            // third-party response or render remote HTML in a privileged page.
            await response.Content.LoadIntoBufferAsync(1024 * 1024, cancellationToken);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream,
                new JsonDocumentOptions { MaxDepth = 12 }, cancellationToken);

            if (json.RootElement.ValueKind != JsonValueKind.Array) throw Unavailable();
            var items = new List<PublicReleaseNote>();
            foreach (var row in json.RootElement.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || Flag(row, "draft")) continue;
                var tag = Field(row, "tag_name");
                if (string.IsNullOrWhiteSpace(tag) || tag.Length > 150) continue;

                var url = Field(row, "html_url");
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    uri.Scheme != Uri.UriSchemeHttps ||
                    uri.Host != "github.com" ||
                    !uri.AbsoluteUri.StartsWith(ReleaseLinkPrefix, StringComparison.Ordinal) ||
                    uri.UserInfo.Length > 0)
                    url = "";

                var title = Field(row, "name");
                if (string.IsNullOrWhiteSpace(title)) title = tag;
                var body = Field(row, "body");
                var published = DateTimeOffset.TryParse(Field(row, "published_at"),
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
                    ? date : (DateTimeOffset?)null;

                items.Add(new(tag, title[..Math.Min(title.Length, 250)],
                    body[..Math.Min(body.Length, 12000)], published, url,
                    Flag(row, "prerelease") || tag.StartsWith("uat-", StringComparison.OrdinalIgnoreCase)
                        ? "uat" : "prod"));
            }
            // A promoted production version displays the identical UAT text,
            // even if its GitHub release was created with different notes.
            // Versions without an actual published production release are never invented.
            var approvedUatNotes = items
                .Where(item => item.Environment == "uat" &&
                    item.Tag.StartsWith("uat-v", StringComparison.OrdinalIgnoreCase))
                .GroupBy(item => item.Tag[4..], StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Body,
                    StringComparer.OrdinalIgnoreCase);

            return items.Select(item => item.Environment == "prod" &&
                    approvedUatNotes.TryGetValue(item.Tag, out var notes)
                    ? item with { Body = notes } : item).ToArray();
        }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (JsonException) { throw Unavailable(); }
        catch (InvalidOperationException) { throw Unavailable(); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { throw Unavailable(); }
    }

    private static string Field(JsonElement value, string name) =>
        value.TryGetProperty(name, out var field) && field.ValueKind == JsonValueKind.String
            ? field.GetString() ?? "" : "";
    private static bool Flag(JsonElement value, string name) =>
        value.TryGetProperty(name, out var field) && field.ValueKind == JsonValueKind.True;
    private static BusinessRuleException Unavailable() => new(503, "release_notes.unavailable");
}

public sealed record PublicReleaseNote(string Tag, string Title, string Body,
    DateTimeOffset? PublishedAt, string Url, string Environment);
public sealed record ReleaseNotesResult(IReadOnlyList<PublicReleaseNote> Items);
