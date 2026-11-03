using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using RBBH.ConnectedParties.Exceptions;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>Read-only identity lookup. Application roles are never read/written in Keycloak.</summary>
public sealed class KeycloakAdminService
{
    private readonly HttpClient _http;
    private readonly ILogger<KeycloakAdminService> _logger;
    private readonly string _client;
    private readonly string _secret;
    private readonly string _realm;
    public bool IsEnabled { get; }

    public KeycloakAdminService(IConfiguration configuration, HttpClient http, ILogger<KeycloakAdminService> logger)
    {
        IsEnabled = configuration.GetValue<bool>("KeycloakSettings:Enabled");
        var issuer = configuration["KeycloakSettings:Issuer"] ?? "";
        var marker = issuer.LastIndexOf("/realms/", StringComparison.OrdinalIgnoreCase);
        _realm = marker > 0 ? issuer[(marker + 8)..].TrimEnd('/') : "";
        _client = configuration["KeycloakSettings:AdminClientId"] ?? "";
        if (string.IsNullOrWhiteSpace(_client)) _client = configuration["KeycloakSettings:ClientId"] ?? "";
        _secret = configuration["KeycloakSettings:AdminClientSecret"] ?? "";
        _http = http; _logger = logger;
        if (marker > 0 && Uri.TryCreate(issuer[..marker].TrimEnd('/') + "/", UriKind.Absolute, out var address)) _http.BaseAddress = address;
        _http.Timeout = TimeSpan.FromSeconds(10);
        _http.MaxResponseContentBufferSize = 1024 * 1024;
    }

    public async Task<KeycloakIdentity?> FindByEmailAsync(string email, CancellationToken ct)
    {
        if (!IsEnabled || _http.BaseAddress is null || string.IsNullOrWhiteSpace(_client) || string.IsNullOrWhiteSpace(_secret))
            throw Unavailable();
        try
        {
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            { ["client_id"] = _client, ["client_secret"] = _secret, ["grant_type"] = "client_credentials" });
            using var tokenResponse = await _http.PostAsync($"realms/{Uri.EscapeDataString(_realm)}/protocol/openid-connect/token", form, ct);
            if (!tokenResponse.IsSuccessStatusCode) throw Unavailable();
            using var token = await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>(ct) ?? throw Unavailable();
            if (!token.RootElement.TryGetProperty("access_token", out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(value.GetString())) throw Unavailable();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"admin/realms/{Uri.EscapeDataString(_realm)}/users?email={Uri.EscapeDataString(email)}&exact=true&max=2");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", value.GetString());
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) throw Unavailable();
            var identities = await response.Content.ReadFromJsonAsync<List<KeycloakIdentity>>(ct) ?? [];
            var matches = identities.Where(item => string.Equals(item.Email?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if (matches.Length > 1) throw new BusinessRuleException(409, "identity.ambiguous");
            var match = matches.SingleOrDefault();
            if (match is not null && (string.IsNullOrWhiteSpace(match.Subject) || match.Subject.Length > 100)) throw Unavailable();
            return match;
        }
        catch (Exception error) when (error is HttpRequestException or JsonException || error is TaskCanceledException && !ct.IsCancellationRequested)
        {
            _logger.LogWarning("Identity lookup failed. FailureType {FailureType}", error.GetType().Name);
            throw Unavailable();
        }
    }

    private static BusinessRuleException Unavailable() => new(503, "identity.provider_unavailable");
}

public sealed record KeycloakIdentity([property: JsonPropertyName("id")] string Subject, string? Username,
    string? FirstName, string? LastName, string? Email, bool Enabled);
