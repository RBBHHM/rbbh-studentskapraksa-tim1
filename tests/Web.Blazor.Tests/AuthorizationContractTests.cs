using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using RBI.ApiCore;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Security;

namespace Web.Blazor.Tests;

public sealed class AuthorizationContractTests
{
    private static ClaimsPrincipal Identity(bool authenticated = true) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.Email, "ordinary@example.test"), new Claim(ClaimTypes.Role, "Admin"),
         new Claim("permission", "KAPITAL_CREATE")], authenticated ? "test-only" : null));

    [Fact]
    public async Task AuthenticatedReportingReadDoesNotDependOnRealmRolesOrProfileAvailability()
    {
        var api = new Mock<IProfileApi>(MockBehavior.Strict);
        Assert.True(await new AppPermissionSource(api.Object).HasPermissionAsync(Identity(), "reports.read"));
        api.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("KAPITAL_CREATE")]
    [InlineData("ADMINISTRATION_MANAGE")]
    [InlineData("FL_EDIT")]
    public async Task RealmAndTokenGrantsNeverSubstituteForBackendPermissions(string permission)
    {
        var api = new Mock<IProfileApi>();
        api.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationProfile("u", "User", "user", "ordinary@example.test", ["gost"], []));
        Assert.False(await new AppPermissionSource(api.Object).HasPermissionAsync(Identity(), permission));
    }

    [Theory]
    [InlineData("kapital")]
    [InlineData("admin")]
    public async Task ActualEffectiveBackendPermissionEnablesGeneration(string role)
    {
        var api = new Mock<IProfileApi>();
        api.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationProfile("u", "User", "user", "user@example.test", [role], ["KAPITAL_CREATE"]));
        Assert.True(await new AppPermissionSource(api.Object).HasPermissionAsync(Identity(), "KAPITAL_CREATE"));
    }

    [Theory]
    [InlineData("reports.read")]
    [InlineData("KAPITAL_CREATE")]
    public async Task AnonymousNeverReceivesGrants(string permission)
    {
        var api = new Mock<IProfileApi>(MockBehavior.Strict);
        Assert.False(await new AppPermissionSource(api.Object).HasPermissionAsync(Identity(false), permission));
        api.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BackendFailureCannotBecomeGuestOrAdminPermissions()
    {
        var api = new Mock<IProfileApi>();
        api.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new RegistryApiException());
        await Assert.ThrowsAsync<RegistryApiException>(async () =>
            await new AppPermissionSource(api.Object).HasPermissionAsync(Identity(), "KAPITAL_CREATE"));
    }

    [Theory]
    [InlineData("Development", false)]
    [InlineData("Production", true)]
    [InlineData("Staging", true)]
    public void EmptyAuthenticationIsOnlyAllowedAsSignedOutLocalDevelopment(string environmentName, bool shouldThrow)
    {
        var environment = Mock.Of<IHostEnvironment>(item => item.EnvironmentName == environmentName);
        var configuration = new ConfigurationBuilder().Build();
        if (shouldThrow) Assert.Throws<InvalidOperationException>(() => IdentityConfiguration.Resolve(configuration, environment));
        else Assert.False(IdentityConfiguration.Resolve(configuration, environment).Enabled);
    }

    [Fact]
    public void PartialConfigurationNeverSilentlyDowngrades()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Authentication:ClientId"] = "configured-client" }).Build();
        var environment = Mock.Of<IHostEnvironment>(item => item.EnvironmentName == "Development");
        Assert.True(IdentityConfiguration.Resolve(configuration, environment).Enabled);
    }

    [Fact]
    public async Task ProfileClientUsesActualEndpointAndRejectsMissingPermissions()
    {
        var transport = new Mock<IRbiApiTransport>();
        transport.Setup(item => item.GetAsync<ApplicationProfile>("api/auth/profile", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RbiApiResult<ApplicationProfile>.Success(new("u", "User", "user", "user@example.test", [], null!)));
        await Assert.ThrowsAsync<RegistryApiException>(() => new ProfileApi(transport.Object).GetAsync());
        transport.Verify(item => item.GetAsync<ApplicationProfile>("api/auth/profile", It.IsAny<CancellationToken>()), Times.Once);
    }
}
