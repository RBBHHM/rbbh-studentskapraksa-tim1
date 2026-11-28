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
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(); var id = body.GetProperty("id").GetInt64();
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
    [InlineData("ambiguous", 409)]
    public async Task InvalidOrUnavailableGccDoesNotCreateARecord(string input, int status)
    {
        var gcc = input is "unavailable" or "invalidrecord" or "ambiguous" ? "123" : input;
        if (input == "unavailable") _source.Setup(source => source.Group("123", It.IsAny<CancellationToken>())).ThrowsAsync(new BusinessRuleException(503, "bank_reference.unavailable", "Referentni izvor nije dostupan."));
        if (input == "invalidrecord") _source.Setup(source => source.Group("123", It.IsAny<CancellationToken>())).ReturnsAsync(new BankGroup("123", new string('x', 201)));
        if (input == "ambiguous") _source.Setup(source => source.Group("123", It.IsAny<CancellationToken>())).ThrowsAsync(new BusinessRuleException(409, "bank_reference.ambiguous"));
        var response = await Client.PostAsJsonAsync("/api/legal-entities", Legal(gcc));
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        if (input == "ambiguous")
            Assert.Equal("bank_reference.ambiguous", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await InDatabase(async db => { Assert.Empty(await db.LegalEntities.ToListAsync()); Assert.Empty(await db.AuditLogs.ToListAsync()); });
    }
    [Theory]
    [InlineData("Fixture company", "00042", HttpStatusCode.Created, null)]
    [InlineData("Other company", "00042", HttpStatusCode.BadRequest, "registry.bank_name_mismatch")]
    [InlineData("Fixture company", "00043", HttpStatusCode.BadRequest, "registry.bank_customer_not_found")]
    public async Task VerifiesRegistrationNumberAndNameBeforeSaving(string name, string maticniBroj, HttpStatusCode expected, string? error)
    {
        _source.SetupGet(source => source.IsConfigured).Returns(true);
        _source.Setup(source => source.Customer("00042", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankCustomer("1234567890125", "00042", "Fixture company"));
        _source.Setup(source => source.Customer("00043", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BankCustomer?)null);
        var response = await Client.PostAsJsonAsync("/api/legal-entities", new
        {
            name, maticniBroj, isResident = false, fbaId = "12345", basisOfConnection = "VL",
            connectedWithBank = true, dateFrom = "2026-01-01", gccNumber = "123"
        });

        Assert.Equal(expected, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (error is null) Assert.Equal("00042", body.GetProperty("maticniBroj").GetString());
        else Assert.Equal(error, body.GetProperty("code").GetString());
        await InDatabase(async db => Assert.Equal(error is null ? 1 : 0, await db.LegalEntities.CountAsync()));
    }

    [Fact]
    public async Task BlankCustomerSearchReturnsAnEmptyPageWhileTheSourceIsDisabled()
    {
        var response = await Client.GetAsync("/api/bank-reference/customers?search=%20%20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(body.GetProperty("items").EnumerateArray());
        _source.Verify(source => source.Customers(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(HttpStatusCode.ServiceUnavailable,
            (await Client.GetAsync("/api/bank-reference/customers?search=Fixture")).StatusCode);
    }

    [Fact]
    public async Task ConfiguredBankSourceRequiresGccOnNewRecords()
    {
        _source.SetupGet(source => source.IsConfigured).Returns(true);
        var page = await Client.GetFromJsonAsync<JsonElement>("/api/legal-entities");
        Assert.True(page.GetProperty("requiresGcc").GetBoolean());
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
