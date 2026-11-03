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
            db.CodeLists.Add(new() { Kategorija = "LanguagePacks", Kod = Guid.NewGuid().ToString("N"), Naziv = "Existing translation",
                DefinitionId = (await db.CodeListDefinitions.SingleAsync(row => row.Name == "LanguagePacks")).Id,
                LanguageCode = "DE", ResourceKey = "errors.roles.not.found", ResourceLabel = "Approved existing label", KreiraoKorisnik = "original" });
            await db.SaveChangesAsync();
            await db.Database.MigrateAsync();
            foreach (var language in new[] { "BS", "EN", "DE" })
            {
                var keys = await db.CodeLists.Where(row => row.Kategorija == "LanguagePacks" && row.LanguageCode == language).Select(row => row.ResourceKey).ToArrayAsync();
                Assert.Empty(BackendErrorFallbacks.All.Keys.Except(keys!));
            }
            Assert.Equal("Approved existing label", (await db.CodeLists.SingleAsync(row => row.LanguageCode == "DE" && row.ResourceKey == "errors.roles.not.found")).ResourceLabel);
            Assert.Equal(3, await db.CodeLists.CountAsync(row => row.ResourceKey == "errors.roles.not.found"));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
