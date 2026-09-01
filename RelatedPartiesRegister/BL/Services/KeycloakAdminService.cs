using System.Net.Http.Json;
using System.Text.Json;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.DL.DTO.Users;
using RBBH.ConnectedParties.Exceptions.Validations;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Calls the Keycloak Admin REST API to read/write realm users.
/// Uses the master-realm admin token (admin-cli client credentials).
/// </summary>
public class KeycloakAdminService
{
    private readonly HttpClient _http;
    private readonly ILogger<KeycloakAdminService> _logger;
    private readonly string _adminClientId;
    private readonly string _adminClientSecret;
    private readonly string _realmName;
    private readonly string _keycloakBase;
    public bool IsEnabled { get; }

    public KeycloakAdminService(
        IConfiguration config,
        HttpClient http,
        ILogger<KeycloakAdminService> logger)
    {
        IsEnabled = config.GetValue<bool>("KeycloakSettings:Enabled");
        var issuer = config["KeycloakSettings:Issuer"] ?? string.Empty;
        var realmMarker = issuer.LastIndexOf("/realms/", StringComparison.OrdinalIgnoreCase);
        _keycloakBase = realmMarker > 0 ? issuer[..realmMarker] : string.Empty;
        _realmName = realmMarker > 0 ? issuer[(realmMarker + 8)..].TrimEnd('/') : string.Empty;
        _adminClientId = config["KeycloakSettings:AdminClientId"] ?? string.Empty;
        _adminClientSecret = config["KeycloakSettings:AdminClientSecret"] ?? string.Empty;
        _logger = logger;

        _http = http;
        if (Uri.TryCreate(_keycloakBase, UriKind.Absolute, out var baseUri))
            _http.BaseAddress = baseUri;
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>Returns a service-account token via the OAuth client-credentials flow.</summary>
    private async Task<string?> GetAdminTokenAsync()
    {
        if (!IsEnabled) return null;
        if (string.IsNullOrWhiteSpace(_adminClientId) || string.IsNullOrWhiteSpace(_adminClientSecret))
        {
            _logger.LogWarning("Keycloak administration is enabled, but its service-account credentials are missing.");
            return null;
        }

        var form = new Dictionary<string, string>
        {
            ["client_id"] = _adminClientId,
            ["client_secret"] = _adminClientSecret,
            ["grant_type"] = "client_credentials"
        };

        var resp = await _http.PostAsync(
            $"/realms/{Uri.EscapeDataString(_realmName)}/protocol/openid-connect/token",
            new FormUrlEncodedContent(form));

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Keycloak service-account token request failed with status {StatusCode}.", (int)resp.StatusCode);
            return null;
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonDocument>();
        return json?.RootElement.GetProperty("access_token").GetString();
    }

    /// <summary>Returns all users from the Keycloak realm with their roles.</summary>
    public async Task<Result<List<UserDTO>>> GetUsersAsync(string? search = null)
    {
        var token = await GetAdminTokenAsync();
        if (token is null)
            return Result<List<UserDTO>>.InternalServerError("Ne mogu se spojiti na Keycloak Admin API.");

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Build query
        var url = $"/admin/realms/{_realmName}/users?max=200";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        var resp = await _http.GetAsync(url);
        if (!resp.IsSuccessStatusCode)
            return Result<List<UserDTO>>.InternalServerError("Greška pri dohvatu korisnika iz Keycloaka.");

        var kcUsers = await resp.Content.ReadFromJsonAsync<List<KeycloakUser>>();
        if (kcUsers is null) return Result<List<UserDTO>>.Success(new List<UserDTO>());

        // Parallel role fetch — fixes N+1 (was: ~1.5s per user = 30s for 20 users)
        var roleTasks = kcUsers.Select(async u =>
        {
            var rolesUrl  = $"/admin/realms/{_realmName}/users/{u.Id}/role-mappings/realm";
            var rolesResp = await _http.GetAsync(rolesUrl);
            if (!rolesResp.IsSuccessStatusCode) return new List<string>();

            var roleDocs = await rolesResp.Content.ReadFromJsonAsync<List<KeycloakRole>>();
            return roleDocs?
                .Where(r => !r.Name.StartsWith("default-roles")
                         && !r.Name.StartsWith("offline")
                         && r.Name != "uma_authorization")
                .Select(r => r.Name)
                .ToList() ?? new List<string>();
        });

        var allRoles = await Task.WhenAll(roleTasks);

        var dtos = kcUsers.Select((u, i) => new UserDTO
        {
            Id        = u.Id,
            Username  = u.Username  ?? string.Empty,
            FirstName = u.FirstName ?? string.Empty,
            LastName  = u.LastName  ?? string.Empty,
            Email     = u.Email     ?? string.Empty,
            IsActive  = u.Enabled,
            Roles     = allRoles[i].Where(ApplicationAccessRoles.All.Contains).ToList()
        }).ToList();

        return Result<List<UserDTO>>.Success(dtos);
    }

    /// <summary>Enables or disables a Keycloak user account.</summary>
    public async Task<bool> SetUserEnabledAsync(string keycloakUserId, bool enabled)
    {
        var token = await GetAdminTokenAsync();
        if (token is null) return false;

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var payload = new { enabled };
        var resp = await _http.PutAsync(
            $"/admin/realms/{_realmName}/users/{keycloakUserId}",
            new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8, "application/json"));

        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(string keycloakUserId)
    {
        var token = await GetAdminTokenAsync();
        if (token is null) return false;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await _http.DeleteAsync($"/admin/realms/{_realmName}/users/{keycloakUserId}");
        return response.IsSuccessStatusCode;
    }

    /// <summary>Creates a new user in Keycloak. Returns the new user's Keycloak UUID.</summary>
    public async Task<Result<string>> CreateKeycloakUserAsync(
        string username, string firstName, string lastName,
        string email, bool isActive)
    {
        var token = await GetAdminTokenAsync();
        if (token is null)
            return Result<string>.InternalServerError("Ne mogu se spojiti na Keycloak Admin API.");

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            username    = username,
            firstName   = firstName,
            lastName    = lastName,
            email       = email,
            enabled     = isActive,
            emailVerified = true
        };

        var resp = await _http.PostAsJsonAsync($"/admin/realms/{_realmName}/users", payload);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Keycloak user creation failed with status {StatusCode}.", (int)resp.StatusCode);
            return Result<string>.InternalServerError("Korisnika trenutno nije moguće kreirati u sistemu za prijavu.");
        }

        // Keycloak returns the new user ID in the Location header
        var location = resp.Headers.Location?.ToString() ?? string.Empty;
        var keycloakId = location.Contains('/') ? location.Split('/').Last() : string.Empty;
        return Result<string>.Success(keycloakId);
    }

    /// <summary>Assigns a realm role to a Keycloak user.</summary>
    public async Task<bool> AssignRealmRoleToUserAsync(string keycloakUserId, string roleId, string roleName)
    {
        var token = await GetAdminTokenAsync();
        if (token is null) return false;

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var roles = new[] { new { id = roleId, name = roleName } };
        var resp  = await _http.PostAsJsonAsync(
            $"/admin/realms/{_realmName}/users/{keycloakUserId}/role-mappings/realm", roles);
        return resp.IsSuccessStatusCode;
    }

    /// <summary>Assigns a validated set of application realm roles in one Keycloak request.</summary>
    public async Task<bool> AssignRealmRolesToUserAsync(string keycloakUserId, IEnumerable<KeycloakRolePublic> roles)
    {
        var token = await GetAdminTokenAsync();
        if (token is null) return false;

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var payload = roles.Select(role => new { id = role.Id, name = role.Name }).ToArray();
        if (payload.Length == 0) return false;
        var response = await _http.PostAsJsonAsync(
            $"/admin/realms/{_realmName}/users/{keycloakUserId}/role-mappings/realm", payload);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Gets current realm roles for a Keycloak user.</summary>
    public async Task<List<KeycloakRolePublic>> GetUserRealmRolesAsync(string keycloakUserId)
    {
        var token = await GetAdminTokenAsync();
        if (token is null) return new();

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var resp = await _http.GetAsync($"/admin/realms/{_realmName}/users/{keycloakUserId}/role-mappings/realm");
        if (!resp.IsSuccessStatusCode) return new();

        var roles = await resp.Content.ReadFromJsonAsync<List<KeycloakRole>>();
        return roles?
            .Where(r => !r.Name.StartsWith("default-roles") && !r.Name.StartsWith("offline") && r.Name != "uma_authorization")
            .Select(r => new KeycloakRolePublic(r.Id, r.Name))
            .ToList() ?? new();
    }

    /// <summary>Removes realm roles from a Keycloak user.</summary>
    public async Task<bool> RemoveRealmRolesFromUserAsync(string keycloakUserId, IEnumerable<KeycloakRolePublic> roles)
    {
        var token = await GetAdminTokenAsync();
        if (token is null) return false;

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var payload = roles.Select(r => new { id = r.Id, name = r.Name }).ToList();
        if (!payload.Any()) return true;

        var req  = new HttpRequestMessage(HttpMethod.Delete,
            $"/admin/realms/{_realmName}/users/{keycloakUserId}/role-mappings/realm")
        {
            Content = JsonContent.Create(payload)
        };
        var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode;
    }

    /// <summary>Returns all realm roles (excluding built-in system roles).</summary>
    public async Task<Result<List<RoleDTO>>> GetRealmRolesAsync()
    {
        var token = await GetAdminTokenAsync();
        if (token is null)
            return Result<List<RoleDTO>>.InternalServerError("Ne mogu se spojiti na Keycloak Admin API.");

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var resp = await _http.GetAsync($"/admin/realms/{_realmName}/roles");
        if (!resp.IsSuccessStatusCode)
            return Result<List<RoleDTO>>.InternalServerError("Greška pri dohvatu rola iz Keycloaka.");

        var kcRoles = await resp.Content.ReadFromJsonAsync<List<KeycloakRole>>();

        var filtered = kcRoles?
            .Where(r => !r.Name.StartsWith("default-roles")
                     && !r.Name.StartsWith("offline")
                     && r.Name != "uma_authorization")
            .Select(r => new RoleDTO { Id = Guid.TryParse(r.Id, out var g) ? g : Guid.NewGuid(), Name = r.Name })
            .OrderBy(r => r.Name)
            .ToList() ?? new List<RoleDTO>();

        return Result<List<RoleDTO>>.Success(filtered);
    }

    // ── Internal Keycloak response models ─────────────────────────────────────

    public record KeycloakRolePublic(string Id, string Name);

    private record KeycloakUser(
        Guid   Id,
        string? Username,
        string? FirstName,
        string? LastName,
        string? Email,
        bool    Enabled
    );

    private record KeycloakRole(string Id, string Name);
}
