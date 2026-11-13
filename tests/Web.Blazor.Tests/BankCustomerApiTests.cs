using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class BankCustomerApiTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(respond(request));
    }
    private static ServiceProvider Client(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRbiApiClient<ILegalEntitiesApi, LegalEntitiesApi>("Legal",
            options => options.BaseUrl = new("https://api.example.test/"))
            .ConfigurePrimaryHttpMessageHandler(() => new Handler(respond));
        return services.BuildServiceProvider();
    }
    [Fact]
    public async Task BankCustomerLookupUsesEncodedSearchAndPagedServerResults()
    {
        string? query = null;
        await using var provider = Client(request =>
        {
            query = request.RequestUri!.PathAndQuery;
            return new(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"items\":[{\"taxNumber\":\"1234567890125\",\"matbroj\":\"123456\",\"name\":\"Bank Fixture\"}],\"total\":1,\"page\":1,\"pageSize\":20}", Encoding.UTF8, "application/json")
            };
        });
        var response = await provider.GetRequiredService<ILegalEntitiesApi>().CustomersAsync("A&B");
        Assert.Equal("/api/bank-reference/customers?search=A%26B&page=1&pageSize=20", query);
        var item = Assert.Single(response.Items);
        Assert.Equal("123456", item.Matbroj); Assert.Equal("Bank Fixture", item.Name);
    }
    [Fact]
    public async Task UnavailableBankSourceNeverReturnsSyntheticCustomers()
    {
        await using var provider = Client(_ => new(HttpStatusCode.ServiceUnavailable));
        var exception = await Assert.ThrowsAsync<RegistryApiException>(() =>
            provider.GetRequiredService<ILegalEntitiesApi>().CustomersAsync("Fixture"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }
}
