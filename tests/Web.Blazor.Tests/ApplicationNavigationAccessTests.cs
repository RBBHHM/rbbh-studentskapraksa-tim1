using Moq;
using RBI.Authorization;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class ApplicationNavigationAccessTests
{
    [Fact]
    public async Task EvaluatesEveryMenuPermissionOnceAndUsesDatabaseOwnedAdministratorRole()
    {
        var profile = new Mock<IProfileApi>();
        profile.Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationProfile("id", "Fixture", "fixture", "fixture@example.test",
                ["admin"], ["ADMINISTRATION_MANAGE"]));

        var permissions = new Mock<IRbiPermissionService>();
        permissions.Setup(client => client.HasPermissionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string name, CancellationToken _) =>
                ValueTask.FromResult(name is "fl.read" or "reports.read"));

        var access = await ApplicationNavigationAccess.ReadAsync(profile.Object, permissions.Object);

        Assert.True(access.Administrator);
        Assert.True(access.Physical);
        Assert.True(access.Reports);
        Assert.False(access.Legal);
        Assert.False(access.Limits);
        Assert.False(access.Capital);
        profile.Verify(client => client.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
        foreach (var permission in new[] { "reports.read", "fl.read", "pl.read", "limiti.read", "kapital.read" })
            permissions.Verify(client => client.HasPermissionAsync(permission, It.IsAny<CancellationToken>()), Times.Once);
        permissions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IdentityProviderRoleDoesNotCreateAdministratorWithoutEffectiveProfileRole()
    {
        var profile = new Mock<IProfileApi>();
        profile.Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationProfile("id", "Fixture", "fixture", "fixture@example.test",
                ["gost"], ["FL_VIEW"]));
        var permissions = new Mock<IRbiPermissionService>();
        permissions.Setup(client => client.HasPermissionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(false));

        var access = await ApplicationNavigationAccess.ReadAsync(profile.Object, permissions.Object);
        Assert.False(access.Administrator);
        Assert.False(access.Physical);
        Assert.False(access.Legal);
        Assert.False(access.Limits);
        Assert.False(access.Capital);
        Assert.False(access.Reports);
    }

    [Fact]
    public async Task UnavailableProfileNeverProducesAnAppNavigationProjection()
    {
        var profile = new Mock<IProfileApi>();
        profile.Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RegistryApiException());
        var permissions = new Mock<IRbiPermissionService>();
        permissions.Setup(client => client.HasPermissionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(true));
        await Assert.ThrowsAsync<RegistryApiException>(() =>
            ApplicationNavigationAccess.ReadAsync(profile.Object, permissions.Object));
    }
}
