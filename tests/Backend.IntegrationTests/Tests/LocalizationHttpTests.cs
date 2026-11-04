using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Entities.Users;

namespace IntegrationTests.Tests;

public abstract class LocalizationFixture : AuthenticatedApiFixture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<RuntimeLocalizationOptions>(); services.AddSingleton<RuntimeLocalizationCache>();
        services.AddScoped<RuntimeLocalizationCatalog>(); services.AddScoped<ICodeListService, CodeListService>();
    }
    protected async Task Seed()
    {
        await InDatabase(async db =>
        {
            if (await db.CodeListDefinitions.AnyAsync(row => row.Name == "Languages")) return;
            var languages = new CodeListDefinition { Name = "Languages", CreatedBy = "fixture", IsSystemRequired = true };
            var packs = new CodeListDefinition { Name = "LanguagePacks", CreatedBy = "fixture", IsSystemRequired = true };
            db.CodeListDefinitions.AddRange(languages, packs);
            foreach (var code in new[] { "BS", "EN", "DE" })
                db.CodeLists.Add(new() { DefinitionId = languages.Id, Kategorija = languages.Name, Kod = code, Naziv = code,
                    KreiraoKorisnik = "fixture", IsSystemRequired = true });
            await db.SaveChangesAsync();
        });
    }
    protected async Task<JsonElement> Pack(string language)
    {
        var response = await Client.GetAsync("/api/localization/resources/" + language);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}

public sealed class LocalizationHttpTests : LocalizationFixture
{
    [Fact]
    public async Task RuntimeUpdateInvalidatesCacheAndFallsBackPerFieldAcrossRequests()
    {
        await Seed();
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/localization/resources/BS/customers.form.name", new { label = "Naziv", helper = "Pomoć", placeholder = "Unesite" })).StatusCode);
        var first = await Pack("DE"); var item = first.GetProperty("items")[0];
        Assert.Equal("Naziv", item.GetProperty("label").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/localization/resources/DE/customers.form.name", new { label = "Name" })).StatusCode);
        var second = await Pack("DE"); Assert.NotEqual(first.GetProperty("version").GetString(), second.GetProperty("version").GetString());
        item = second.GetProperty("items")[0]; Assert.Equal("Name", item.GetProperty("label").GetString());
        Assert.Equal("Pomoć", item.GetProperty("helper").GetString()); Assert.Equal("Unesite", item.GetProperty("placeholder").GetString());
        await InDatabase(async db => { Assert.Equal(2, await db.AuditLogs.CountAsync()); Assert.Equal("creator", (await db.CodeListDefinitions.SingleAsync(row => row.Name == "LanguagePacks")).ModifiedBy); });
    }
    [Theory]
    [InlineData("BS", "Invalid_Key", "Text", 400)]
    [InlineData("XX", "valid.key", "Text", 400)]
    [InlineData("EN", "valid.key", "", 400)]
    public async Task InvalidInputsAreSafeAndDoNotWrite(string language, string key, string label, int status)
    {
        await Seed();
        using var response = await Client.PutAsJsonAsync($"/api/localization/resources/{language}/{key}", new { label });
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("SqlException", content);
        await InDatabase(async db => { Assert.Empty(db.AuditLogs); Assert.False(await db.CodeLists.AnyAsync(row => row.ResourceKey != null)); });
    }
    [Fact]
    public async Task LegacyKeyCasingIsReadableAndUpdatableButCannotBeIntroducedAsNew()
    {
        await Seed(); await InDatabase(async db =>
        {
            var definition = await db.CodeListDefinitions.SingleAsync(row => row.Name == "LanguagePacks");
            db.CodeLists.Add(new() { DefinitionId = definition.Id, Kategorija = definition.Name, Kod = "LEGACY", Naziv = "LegacyKey",
                LanguageCode = "BS", ResourceKey = "LegacyKey", ResourceLabel = "Before", KreiraoKorisnik = "fixture" });
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/localization/resources/BS/legacykey", new { label = "After" })).StatusCode);
        Assert.Equal("After", (await Pack("BS")).GetProperty("items")[0].GetProperty("label").GetString());
        await InDatabase(async db => Assert.Equal("LegacyKey", (await db.CodeLists.SingleAsync(row => row.ResourceKey != null)).ResourceKey));
    }
    [Fact]
    public async Task ResourceWritesRequireDatabaseAdminWhileGuestCanReadAndAnonymousCannot()
    {
        await Seed(); using var guest = NewClient("guest"); using var anonymous = NewClient("anonymous");
        Assert.Equal(HttpStatusCode.OK, (await guest.GetAsync("/api/localization/languages")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.PutAsJsonAsync("/api/localization/resources/BS/valid.key", new { label = "No" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/localization/languages")).StatusCode);
        await InDatabase(async db =>
        {
            db.AppUsers.Add(new AppUser { KeycloakId = "inactive", KeycloakSubjectConfirmed = true, IsActive = false, Username = "inactive", Email = "inactive@example.test" }); await db.SaveChangesAsync();
        });
        using var inactive = NewClient("inactive"); Assert.Equal(HttpStatusCode.Forbidden, (await inactive.GetAsync("/api/localization/languages")).StatusCode);
    }
    [Fact]
    public async Task RequiredLanguagesAndDefinitionsCannotBeDeactivatedThroughGenericCrud()
    {
        await Seed(); var id = 0;
        await InDatabase(async db => id = (await db.CodeLists.SingleAsync(row => row.Kategorija == "Languages" && row.Kod == "BS")).ID);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PutAsJsonAsync($"/api/code-lists/{id}", new { naziv = "BS", aktivan = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.DeleteAsync($"/api/code-lists/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.DeleteAsync("/api/code-lists/categories/Languages")).StatusCode);
        await InDatabase(async db => { Assert.Equal(3, await db.CodeLists.CountAsync()); Assert.Empty(db.AuditLogs); });
    }
    [Fact]
    public async Task MissingKeyFallsBackToStableKeyAndIsNotCachedAsAFalseTranslation()
    {
        await Seed(); await using var scope = App.Services.CreateAsyncScope();
        var catalog = scope.ServiceProvider.GetRequiredService<RuntimeLocalizationCatalog>();
        Assert.Equal("missing.key", await catalog.Text("missing.key", "fr-FR", default));
        await Client.PutAsJsonAsync("/api/localization/resources/BS/missing.key", new { label = "Now present" });
        Assert.Equal("Now present", await catalog.Text("missing.key", "DE", default));
    }
    [Fact]
    public async Task ExportCanBePreviewedAndImportedWithoutDuplicatingValues()
    {
        Assert.Equal(HttpStatusCode.Created, (await Client.PostAsJsonAsync("/api/code-lists", new { kategorija = "Roundtrip", kod = "A", naziv = "One" })).StatusCode);
        var exported = await Client.GetAsync("/api/code-lists/Roundtrip/export"); Assert.Equal(HttpStatusCode.OK, exported.StatusCode);
        var bytes = await exported.Content.ReadAsByteArrayAsync();
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(bytes), "file", "codebook.xlsx");
        var imported = await Client.PostAsync("/api/code-lists/Roundtrip/import?dryRun=false", form);
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        var body = await imported.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(0, body.GetProperty("imported").GetInt32());
        Assert.Single(body.GetProperty("duplicates").EnumerateArray());
        await InDatabase(async db => Assert.Single(await db.CodeLists.Where(row => row.Kategorija == "Roundtrip").ToListAsync()));
    }

    [Fact]
    public async Task DefinitionMetadataUsesExpectedVersionAndAtomicAudit()
    {
        await Seed(); var definitions = await Client.GetFromJsonAsync<JsonElement>("/api/code-lists/categories/definitions");
        var version = definitions.GetProperty("items").EnumerateArray().Single(row => row.GetProperty("name").GetString() == "Languages").GetProperty("version").GetInt32();
        var response = await Client.PutAsJsonAsync("/api/code-lists/categories/Languages", new { description = "Updated description", expectedVersion = version });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsJsonAsync("/api/code-lists/categories/Languages", new { description = "Stale", expectedVersion = version })).StatusCode);
        await InDatabase(async db => { Assert.Equal("Updated description", (await db.CodeListDefinitions.SingleAsync(row => row.Name == "Languages")).Description); Assert.Single(db.AuditLogs); });
    }

    [Fact]
    public async Task EnglishErrorUsesRuntimeResourceAndRetainsMachineCode()
    {
        await Seed(); await Client.PutAsJsonAsync("/api/localization/resources/EN/errors.localization.invalid.key", new { label = "Use lowercase semantic keys." });
        Client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");
        var response = await Client.PutAsJsonAsync("/api/localization/resources/EN/BadKey", new { label = "Invalid" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("localization.invalid_key", body.GetProperty("code").GetString());
        Assert.Equal("Use lowercase semantic keys.", body.GetProperty("detail").GetString());
    }
}
