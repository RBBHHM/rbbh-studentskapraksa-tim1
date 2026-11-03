using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.IoC.Extensions.Controllers;
using RBBH.ConnectedParties.Localization;
using IntegrationTests.Tests.Users;

namespace IntegrationTests.Tests;

public sealed class ControllerResourcesSqlTests : RoleCatalogFixture
{
    protected override bool UseSql => true;
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services); services.AddControllersExtension();
        services.AddScoped<BackendLocalizationFilter>();
        services.AddScoped<IAppUserService, AppUserService>(); services.AddHttpClient<KeycloakAdminService>();
    }

    [SqlServerFact]
    public async Task ExplicitAndFrameworkFieldValidatorsUseRuntimeResourcesBeforeActionExecution()
    {
        Client.DefaultRequestHeaders.Add("Accept-Language", "EN");
        var missingName = new { isResident = false, name = "", fbaId = "123", basisOfConnection = "basis", connectedWithBank = true, dateFrom = "2026-10-01" };
        var first = await Client.PostAsJsonAsync("/api/legal-entities", missingName);
        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        var initial = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Name is required.", initial.GetProperty("errors").GetProperty("name")[0].GetString());
        Assert.Equal("The submitted data is invalid.", initial.GetProperty("title").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/localization/resources/EN/validation.registry.name.required", new { label = "Runtime name requirement" })).StatusCode);
        var updated = await (await Client.PostAsJsonAsync("/api/legal-entities", missingName)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Runtime name requirement", updated.GetProperty("errors").GetProperty("name")[0].GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/localization/resources/EN/validation.registry.name.required", new { label = "Broken {0{" })).StatusCode);
        var safe = await Client.PostAsJsonAsync("/api/legal-entities", missingName);
        Assert.Equal(HttpStatusCode.BadRequest, safe.StatusCode);
        var fallback = await safe.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Naziv je obavezan.", fallback.GetProperty("errors").GetProperty("name")[0].GetString());
        Client.DefaultRequestHeaders.Remove("Accept-Language"); Client.DefaultRequestHeaders.Add("Accept-Language", "de");
        var generic = await Client.PostAsJsonAsync("/api/role-catalog", new { name = "valid-role", displayName = "", permissionIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.BadRequest, generic.StatusCode);
        var german = await generic.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Die eingegebenen Daten sind ungültig.", german.GetProperty("title").GetString());
        Assert.Contains("erforderlich", german.GetProperty("errors").GetProperty("displayName")[0].GetString());
        await InDatabase(async db =>
        {
            var legacy = await db.CodeLists.SingleAsync(row => row.LanguageCode == "DE" && row.ResourceKey == "validation.registry.name.required");
            legacy.ResourceKey = legacy.ResourceKey!.ToUpperInvariant(); legacy.ResourceLabel = "Legacy-Name erforderlich"; await db.SaveChangesAsync();
        });
        var legacyResponse = await (await Client.PostAsJsonAsync("/api/legal-entities", missingName)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Legacy-Name erforderlich", legacyResponse.GetProperty("errors").GetProperty("name")[0].GetString());
        await InDatabase(async db => { Assert.Empty(db.LegalEntities); Assert.False(await db.Roles.AnyAsync(row => row.Name == "valid-role")); });
    }

    [SqlServerFact]
    public async Task ExistingUsersAndPeriodResponseContractsHaveLocalizedMessagesAndNoMutationOnDenial()
    {
        Client.DefaultRequestHeaders.Add("Accept-Language", "DE");
        var self = await Client.DeleteAsync("/api/users/creator/permanent");
        Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);
        var body = await self.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Sie können Ihr eigenes Konto nicht löschen.", body.GetProperty("detail").GetString());
        Client.DefaultRequestHeaders.Remove("Accept-Language"); Client.DefaultRequestHeaders.Add("Accept-Language", "EN");
        var response = await Client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{Guid.NewGuid()}/respond", new { message = new string('x', 9) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("message", error.GetProperty("errors")[0].GetProperty("field").GetString());
        Assert.Equal("The response must contain 10 to 490 characters.", error.GetProperty("errors")[0].GetProperty("message").GetString());
        await InDatabase(async db => { Assert.Equal(3, await db.AppUsers.CountAsync()); Assert.Empty(db.UnlockRequests); Assert.Empty(db.AuditLogs); });
    }

    [SqlServerFact]
    public async Task ConcurrentLanguagesRemainIsolatedAndResourcesDoNotLeakOutsideRequestScope()
    {
        var languages = new[] { "BS", "EN", "DE", "EN", "BS", "DE" };
        await Task.WhenAll(languages.Select(async language =>
        {
            using var client = NewClient("creator"); client.DefaultRequestHeaders.Add("Accept-Language", language);
            var response = await client.PostAsJsonAsync("/api/role-catalog", new { name = "isolated-role", displayName = "", permissionIds = Array.Empty<Guid>() });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var expected = language switch { "EN" => "The submitted data is invalid.", "DE" => "Die eingegebenen Daten sind ungültig.", _ => "Uneseni podaci nisu ispravni." };
            Assert.Equal(expected, body.GetProperty("title").GetString());
        }));
        Assert.Equal("Uneseni podaci nisu ispravni.", BackendMessages.Message("errors.model.invalid.title"));
        await InDatabase(async db => Assert.Empty(db.AuditLogs));
    }
}
