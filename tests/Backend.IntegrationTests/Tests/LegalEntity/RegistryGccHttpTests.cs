using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.Exceptions;

namespace IntegrationTests.Tests.LegalEntity;

public sealed class RegistryGccHttpTests : AuthenticatedApiFixture
{
    private readonly Mock<IBankReferenceSource> _source = new();
    protected override void ConfigureServices(IServiceCollection services)
    {
        _source.Setup(source => source.Group("123", It.IsAny<CancellationToken>())).ReturnsAsync(new BankGroup("123", "Authoritative fixture group"));
        services.AddSingleton(_source.Object);
    }
    private static object Legal(string? gcc, bool? exposure = true) => new
    {
        name = "Fixture company", isResident = false, fbaId = "12345", basisOfConnection = "VL",
        connectionDescription = "Fixture relationship", connectedWithBank = true, dateFrom = "2026-01-01",
        gccNumber = gcc, gccName = "Forged client name", isLargeExposure = exposure
    };
    [Fact]
    public async Task GccNameIsServerResolvedAndOmittedFieldsPreserveStoredValues()
    {
        var response = await Client.PostAsJsonAsync("/api/legal-entities", Legal("123")); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); var id = body.GetProperty("id").GetGuid();
        Assert.Equal("Authoritative fixture group", body.GetProperty("gccName").GetString()); Assert.True(body.GetProperty("isLargeExposure").GetBoolean());
        var update = await Client.PutAsJsonAsync($"/api/legal-entities/{id}", Legal(null, null)); Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        body = await update.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("123", body.GetProperty("gccNumber").GetString()); Assert.True(body.GetProperty("isLargeExposure").GetBoolean());
        _source.Verify(source => source.Group("123", It.IsAny<CancellationToken>()), Times.Once);
    }
    [Theory]
    [InlineData("unknown", 400)]
    [InlineData("999", 400)]
    [InlineData("unavailable", 503)]
    [InlineData("invalidrecord", 503)]
    public async Task InvalidOrUnavailableGccDoesNotCreateARecord(string input, int status)
    {
        var gcc = input is "unavailable" or "invalidrecord" ? "123" : input;
        if (input == "unavailable") _source.Setup(source => source.Group("123", It.IsAny<CancellationToken>())).ThrowsAsync(new BusinessRuleException(503, "bank_reference.unavailable", "Referentni izvor nije dostupan."));
        if (input == "invalidrecord") _source.Setup(source => source.Group("123", It.IsAny<CancellationToken>())).ReturnsAsync(new BankGroup("123", new string('x', 201)));
        Assert.Equal((HttpStatusCode)status, (await Client.PostAsJsonAsync("/api/legal-entities", Legal(gcc))).StatusCode);
        await InDatabase(async db => { Assert.Empty(await db.LegalEntities.ToListAsync()); Assert.Empty(await db.AuditLogs.ToListAsync()); });
    }
    [Fact]
    public async Task ConfiguredBankSourceRequiresGccOnNewRecords()
    {
        _source.SetupGet(source => source.IsConfigured).Returns(true);
        var response = await Client.PostAsJsonAsync("/api/legal-entities", Legal(null)); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("registry.gcc_required", await response.Content.ReadAsStringAsync());
        await InDatabase(async db => { Assert.Empty(db.LegalEntities); Assert.Empty(db.AuditLogs); });
    }

    [Fact]
    public async Task LegacyCreateWithoutGccDoesNotInventSourceValuesAndPagedLookupIsStable()
    {
        var response = await Client.PostAsJsonAsync("/api/legal-entities", Legal(null, null)); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(JsonValueKind.Null, body.GetProperty("gccName").ValueKind); Assert.False(body.GetProperty("isLargeExposure").GetBoolean());
        await InDatabase(async db =>
        {
            for (var index = 0; index < 25; index++) db.LegalEntities.Add(new() { Name = "Lookup " + index.ToString("D2"), CreatedBy = "fixture" });
            await db.SaveChangesAsync();
        });
        var page = await Client.GetFromJsonAsync<JsonElement>("/api/legal-entities/limits/lookup?search=LOOKUP&page=2&pageSize=20");
        Assert.Equal(25, page.GetProperty("total").GetInt32()); Assert.Equal(5, page.GetProperty("items").GetArrayLength());
        Assert.Equal("Lookup 20", page.GetProperty("items")[0].GetProperty("name").GetString());
    }
}
