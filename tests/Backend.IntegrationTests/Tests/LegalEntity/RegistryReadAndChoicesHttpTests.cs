using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;

namespace IntegrationTests.Tests.LegalEntity;

public sealed class RegistryReadAndChoicesHttpTests : AuthenticatedApiFixture
{
    protected override void ConfigureServices(IServiceCollection services) => services.AddScoped<ILimitService, LimitService>();
    [Fact]
    public async Task PagedPhysicalAndLimitReadsAreBoundedStableAndCaseInsensitive()
    {
        await InDatabase(async db =>
        {
            var legal = new RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity { Name = "Mixed Company", CreatedBy = "fixture" }; db.LegalEntities.Add(legal);
            for (var index = 0; index < 25; index++)
            {
                db.RelatedPersons.Add(new() { FirstName = "Mixed", LastName = "Fixture", CreatedBy = "fixture" });
                db.Limiti.Add(new() { LegalEntityId = legal.Id, TipLimita = "TYPE", CreatedBy = "fixture" });
            }
            await db.SaveChangesAsync();
        });
        foreach (var path in new[] { "/api/related-persons/page?search=mIxEd", "/api/limiti/page?search=mIxEd" })
        {
            var first = await Client.GetFromJsonAsync<JsonElement>(path + "&page=2&pageSize=20"); var repeat = await Client.GetFromJsonAsync<JsonElement>(path + "&page=2&pageSize=20");
            Assert.Equal(25, first.GetProperty("total").GetInt32()); Assert.Equal(5, first.GetProperty("items").GetArrayLength());
            Assert.Equal(first.GetProperty("items").GetRawText(), repeat.GetProperty("items").GetRawText());
            var beyond = await Client.GetFromJsonAsync<JsonElement>(path + "&page=2147483647&pageSize=2147483647");
            Assert.Equal(100, beyond.GetProperty("pageSize").GetInt32()); Assert.Empty(beyond.GetProperty("items").EnumerateArray());
        }
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DefinedCatalogRejectsInactiveOrUnknownValuesBeforeAnyMutation(bool legal)
    {
        await InDatabase(async db =>
        {
            var definition = new CodeListDefinition { Name = "OsnovPovezanosti", CreatedBy = "fixture" }; db.CodeListDefinitions.Add(definition);
            db.CodeLists.Add(new() { DefinitionId = definition.Id, Kategorija = definition.Name, Kod = "VL", Naziv = "Legacy ownership", Aktivan = false, KreiraoKorisnik = "fixture" }); await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync(legal ? "/api/legal-entities" : "/api/related-persons", legal ? RegistryVerificationHttpTests.Legal() : RegistryVerificationHttpTests.Physical())).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.AuditLogs); Assert.Empty(db.LegalEntities); Assert.Empty(db.RelatedPersons); });
    }
    [Fact]
    public async Task ActiveLegacyChoiceLabelRemainsCompatibleWhileInactiveDefinitionIsAuthoritative()
    {
        await InDatabase(async db =>
        {
            var definition = new CodeListDefinition { Name = "OsnovPovezanosti", CreatedBy = "fixture" }; db.CodeListDefinitions.Add(definition);
            db.CodeLists.Add(new() { DefinitionId = definition.Id, Kategorija = definition.Name, Kod = "CODE", Naziv = "VL", KreiraoKorisnik = "fixture" }); await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Created, (await Client.PostAsJsonAsync("/api/legal-entities", RegistryVerificationHttpTests.Legal())).StatusCode);
        await InDatabase(async db => { (await db.CodeListDefinitions.SingleAsync()).IsActive = false; await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync("/api/related-persons", RegistryVerificationHttpTests.Physical())).StatusCode);
        await InDatabase(async db => Assert.Single(db.AuditLogs));
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExportsRejectOverFiveThousandRowsRatherThanSilentlyTruncating(bool legal)
    {
        await InDatabase(async db =>
        {
            for (var index = 0; index < 5001; index++)
                if (legal) db.LegalEntities.Add(new() { Name = "Fixture", CreatedBy = "fixture" });
                else db.RelatedPersons.Add(new() { FirstName = "Fixture", LastName = "Person", CreatedBy = "fixture" });
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync(legal ? "/api/legal-entities/export" : "/api/related-persons/export")).StatusCode);
    }
}
