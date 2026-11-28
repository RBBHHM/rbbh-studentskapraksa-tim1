using Moq;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class ApplicationNavigationAccessTests
{
    [Theory]
    [InlineData("custom-viewer", true, false, "ADMINISTRATION_VIEW")]
    [InlineData("custom-manager", true, true, "ADMINISTRATION_VIEW", "ADMINISTRATION_MANAGE")]
    [InlineData("admin", false, false, "ADMINISTRATION_MANAGE")]
    [InlineData("gost", false, false)]
    public async Task MenuAndActionsFollowEffectiveProfileGrants(string role, bool canRead, bool canManage, params string[] grants)
    {
        var effective = new ApplicationProfile("id", "Fixture", "fixture", "fixture@example.test",
            [role], ["FL_VIEW", .. grants]);
        var profile = new Mock<IProfileApi>();
        profile.Setup(client => client.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(effective);

        var access = await ApplicationNavigationAccess.ReadAsync(profile.Object);

        Assert.Equal(canRead, access.Administration);
        Assert.Equal(canManage, ApplicationAccess.CanManageAdministration(effective));
        Assert.True(access.Physical);
        Assert.True(access.Reports);
        Assert.False(access.Legal);
        Assert.False(access.Limits);
        Assert.False(access.Capital);
        profile.Verify(client => client.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnavailableProfileNeverProducesAnAppNavigationProjection()
    {
        var profile = new Mock<IProfileApi>();
        profile.Setup(client => client.GetAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new RegistryApiException());
        await Assert.ThrowsAsync<RegistryApiException>(() => ApplicationNavigationAccess.ReadAsync(profile.Object));
    }

    [Fact]
    public async Task CancelledRequestDoesNotFetchProfile()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var profile = new Mock<IProfileApi>(MockBehavior.Strict);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ApplicationNavigationAccess.ReadAsync(profile.Object, cancelled.Token));
        profile.VerifyNoOtherCalls();
    }
}
