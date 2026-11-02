using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.Exceptions;

namespace UnitTests.Services;

public sealed class KeycloakIdentityLookupTests
{
    [Fact]
    public async Task ExactLookupUsesConfiguredAdminClientPreservesBasePathAndDisposesResponsesWithoutRoleCalls()
    {
        var handler = new IdentityResponses(); using var http = new HttpClient(handler);
        var result = await new KeycloakAdminService(Config(), http, NullLogger<KeycloakAdminService>.Instance)
            .FindByEmailAsync("person@example.test", default);
        Assert.Equal("opaque-subject", result!.Subject);
        Assert.Equal(2, handler.Paths.Count);
        Assert.StartsWith("/auth/realms/fixture/protocol/openid-connect/token", handler.Paths[0]);
        Assert.Contains("client_id=service-reader", handler.TokenForm);
        Assert.StartsWith("/auth/admin/realms/fixture/users?", handler.Paths[1]);
        Assert.Contains("exact=true", handler.Paths[1]); Assert.DoesNotContain("role-mappings", string.Join(" ", handler.Paths));
        Assert.All(handler.Contents, content => Assert.True(content.Disposed));
    }

    [Fact]
    public async Task MissingServiceAccountFailsSafelyWithoutInventingCredentialsOrCallingNetwork()
    {
        var handler = new IdentityResponses(); using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => new KeycloakAdminService(Config(false), http,
            NullLogger<KeycloakAdminService>.Instance).FindByEmailAsync("person@example.test", default));
        Assert.Equal(503, error.Status); Assert.Equal("identity.provider_unavailable", error.Code); Assert.Empty(handler.Paths);
    }

    [Fact]
    public async Task ProviderFailureDoesNotExposeRawBody()
    {
        var handler = new IdentityResponses { Status = HttpStatusCode.Forbidden }; using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => new KeycloakAdminService(Config(), http,
            NullLogger<KeycloakAdminService>.Instance).FindByEmailAsync("person@example.test", default));
        Assert.Equal(503, error.Status); Assert.DoesNotContain("private-provider-detail", error.Message);
        Assert.All(handler.Contents, content => Assert.True(content.Disposed));
    }

    private static IConfiguration Config(bool secret = true) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["KeycloakSettings:Enabled"] = "true", ["KeycloakSettings:Issuer"] = "https://identity.example.test/auth/realms/fixture",
        ["KeycloakSettings:ClientId"] = "web-client", ["KeycloakSettings:AdminClientId"] = "service-reader",
        ["KeycloakSettings:AdminClientSecret"] = secret ? "test-placeholder" : null
    }).Build();

    private sealed class IdentityResponses : HttpMessageHandler
    {
        internal readonly List<string> Paths = [];
        internal readonly List<TrackingContent> Contents = [];
        internal string? TokenForm;
        internal HttpStatusCode Status = HttpStatusCode.OK;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Paths.Add(request.RequestUri!.PathAndQuery);
            if (request.Method == HttpMethod.Post) TokenForm = await request.Content!.ReadAsStringAsync(ct);
            var json = Status != HttpStatusCode.OK ? "private-provider-detail" : request.Method == HttpMethod.Post
                ? "{\"access_token\":\"fixture-opaque-token\"}"
                : "[{\"id\":\"opaque-subject\",\"username\":\"person\",\"email\":\"person@example.test\",\"enabled\":true}]";
            var content = new TrackingContent(json); Contents.Add(content);
            return new HttpResponseMessage(Status) { Content = content };
        }
    }

    private sealed class TrackingContent(string value) : StringContent(value, Encoding.UTF8, "application/json")
    {
        internal bool Disposed;
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
}
