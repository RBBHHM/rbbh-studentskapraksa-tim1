using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Security;

namespace Web.Blazor.Tests;

public sealed class UnconfiguredProxyTests
{
    [Fact]
    public async Task BackendUrlAloneDoesNotEnableAnAnonymousDevelopmentProxy()
    {
        var services = new ServiceCollection(); services.AddLogging();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Apis:BaseUrl"] = "http://backend.example.test/" }).Build();
        services.AddAppApi(configuration, Mock.Of<IHostEnvironment>(environment => environment.EnvironmentName == "Development"), new IdentityConfiguration(false));
        await using var provider = services.BuildServiceProvider();
        Assert.Null(provider.GetService<IHttpClientFactory>());
        await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<IProfileApi>().GetAsync());
        await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<IRelatedPersonsApi>().ListAsync());
        await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<ILegalEntitiesApi>().ListAsync());
        await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<ILimitsApi>().ListAsync());
        await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<ICapitalApi>().GetAsync());
        await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<IReportsApi>().ListAsync(ReportKind.Daily));
        await Assert.ThrowsAsync<RegistryApiException>(() => provider.GetRequiredService<INotificationsApi>().GetAsync());
    }
}
