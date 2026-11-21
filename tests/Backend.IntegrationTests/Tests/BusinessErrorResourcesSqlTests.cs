using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Localization;
using IntegrationTests.Tests.Users;

namespace IntegrationTests.Tests;

public sealed class BusinessErrorResourcesSqlTests : RoleCatalogFixture
{
    protected override bool UseSql => true;

    [SqlServerFact]
    public async Task ScopedHttpErrorsUseRuntimeLanguageOverridesWithoutDeployment()
    {
        Client.DefaultRequestHeaders.Add("Accept-Language", "de-DE");
        var missing = Guid.NewGuid();
        var response = await Client.GetAsync($"/api/role-catalog/{missing}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Die Rolle wurde nicht gefunden.", body.GetProperty("detail").GetString());
        Assert.Equal("errors.roles.not.found", body.GetProperty("resourceKey").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/localization/resources/DE/errors.roles.not.found", new { label = "Runtime-Text geändert" })).StatusCode);
        var changed = await (await Client.GetAsync($"/api/role-catalog/{missing}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Runtime-Text geändert", changed.GetProperty("detail").GetString());
        Client.DefaultRequestHeaders.Remove("Accept-Language"); Client.DefaultRequestHeaders.Add("Accept-Language", "en");
        var english = await (await Client.GetAsync($"/api/role-catalog/{missing}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("The role was not found.", english.GetProperty("detail").GetString());
    }

    [SqlServerFact]
    public async Task ErrorFormattingUsesSafeParametersAndMalformedAdminTemplateFallsBackSafely()
    {
        async Task<string> Render()
        {
            await using var scope = App.Services.CreateAsyncScope();
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Headers.AcceptLanguage = "EN"; context.Response.Body = new MemoryStream();
            await new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance).TryHandleAsync(context,
                new BusinessRuleException(400, "period.invalid_response", new object?[] { 10, 490 }), default);
            context.Response.Body.Position = 0;
            return (await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body)).GetProperty("detail").GetString()!;
        }
        Assert.Equal("Enter between 10 and 490 characters.", await Render());
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/localization/resources/EN/errors.period.invalid.response", new { label = "Broken {0{" })).StatusCode);
        var fallback = await Render(); Assert.Contains("10", fallback); Assert.Contains("490", fallback);
        Assert.DoesNotContain("Broken", fallback); Assert.DoesNotContain("{0}", fallback);
    }

    [SqlServerFact]
    public async Task ResourceUpgradePreservesExistingTranslationsAndProvidesAllThreeLanguagePacks()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_RESOURCE_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261103084605_AddAdministrativeRoleCatalog");
            var valueKey = Guid.NewGuid().ToString("N");
            var createdAt = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT dbo.CodeLists (Kategorija, Kod, Naziv, Aktivan, KreiraoKorisnik, KreiranDatum, IsSystemRequired,
                    DefinitionId, LanguageCode, ResourceKey, ResourceLabel, ResourceHelper, ResourcePlaceholder)
                SELECT N'LanguagePacks', {valueKey}, N'Existing translation', 1, N'original', {createdAt}, 0,
                    Id, N'DE', N'errors.roles.not.found', N'Approved existing label', N'Existing helper', N'Existing placeholder'
                FROM dbo.CodeListDefinitions
                WHERE Name = N'LanguagePacks';
                """);
            await db.Database.MigrateAsync();
            foreach (var language in new[] { "BS", "EN", "DE" })
            {
                var keys = await db.CodeLists.Where(row => row.Kategorija == "LanguagePacks" && row.Localization!.LanguageCode == language).Select(row => row.Localization!.ResourceKey).ToArrayAsync();
                Assert.Empty(BackendMessages.All.Keys.Except(keys!));
            }
            var translation = await db.CodeLists.SingleAsync(row => row.Localization!.LanguageCode == "DE"
                && row.Localization.ResourceKey == "errors.roles.not.found");
            Assert.Equal(valueKey, translation.Kod);
            Assert.Equal("Approved existing label", translation.Localization!.ResourceLabel);
            Assert.Equal("Existing helper", translation.Localization.ResourceHelper);
            Assert.Equal("Existing placeholder", translation.Localization.ResourcePlaceholder);
            Assert.Equal(3, await db.CodeLists.CountAsync(row => row.Localization!.ResourceKey == "errors.roles.not.found"));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
