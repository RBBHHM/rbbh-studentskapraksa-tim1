using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.API.Controllers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace UnitTests.Controllers;

public sealed class ModulesControllerTests
{
    private static ModulesController CreateController(params Claim[] claims)
    {
        return new ModulesController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            }
        };
    }

    private static List<ModuleDto> GetModules(ModulesController controller)
    {
        var response = controller.GetModules().Should().BeOfType<OkObjectResult>().Subject;
        return response.Value.Should().BeOfType<ModulesResponse>().Subject.Modules;
    }

    [Theory]
    [InlineData(ApplicationPermissions.PhysicalPersonsView, "physical-persons")]
    [InlineData(ApplicationPermissions.LegalPersonsView, "legal-persons")]
    [InlineData(ApplicationPermissions.LimitsView, "limits")]
    [InlineData(ApplicationPermissions.RegulatoryReportingView, "regulatory-reporting")]
    public void OnlyExplicitEffectiveReadPermissionEnablesItsModule(string grant, string expectedKey)
    {
        var modules = GetModules(CreateController(new Claim("permission", grant)));

        modules.Should().HaveCount(4);
        modules.Single(module => module.Key == expectedKey).IsEnabled.Should().BeTrue();
        modules.Where(module => module.Key != expectedKey).Should()
            .OnlyContain(module => !module.IsEnabled);
    }

    [Theory]
    [InlineData(ApplicationAccessRoles.Guest)]
    [InlineData(ApplicationAccessRoles.PhysicalPersons)]
    [InlineData(ApplicationAccessRoles.LegalPersons)]
    [InlineData(ApplicationAccessRoles.Capital)]
    [InlineData(ApplicationAccessRoles.Limits)]
    [InlineData(ApplicationAccessRoles.Administrator)]
    public void ResolvedRoleGrantsIncludeGuestReadAccess(string role)
    {
        // The production claims transformation adds Guest baseline permissions
        // and reads extra grants from the database for assigned roles.
        var claims = RolePermissionMatrix.Resolve([role])
            .Select(grant => new Claim("permission", grant))
            .ToArray();

        GetModules(CreateController(claims)).Should().OnlyContain(module => module.IsEnabled);
    }

    [Fact]
    public void RoleClaimsAloneNeverEnableModules()
    {
        var modules = GetModules(CreateController(
            new Claim(ClaimTypes.Role, ApplicationAccessRoles.Administrator),
            new Claim(ClaimTypes.Role, ApplicationAccessRoles.Guest)));

        modules.Should().OnlyContain(module => !module.IsEnabled);
    }

    [Fact]
    public void CustomDatabaseRoleUsesItsActualGrants()
    {
        var modules = GetModules(CreateController(
            new Claim(ClaimTypes.Role, "custom-role-from-database"),
            new Claim("permission", ApplicationPermissions.RegulatoryReportingView)));

        modules.Single(module => module.Key == "regulatory-reporting").IsEnabled.Should().BeTrue();
        modules.Where(module => module.Key != "regulatory-reporting").Should()
            .OnlyContain(module => !module.IsEnabled);
    }

    [Fact]
    public void CaseChangedPermissionCannotEnableModule()
    {
        var modules = GetModules(CreateController(
            new Claim("permission", ApplicationPermissions.PhysicalPersonsView.ToLowerInvariant())));

        modules.Should().OnlyContain(module => !module.IsEnabled);
    }

    [Fact]
    public void ModulesAlwaysReturnStableRoutesAndMetadata()
    {
        var modules = GetModules(CreateController());

        modules.Select(module => (module.Key, module.Route)).Should().BeEquivalentTo(
            new[]
            {
                ("physical-persons", "/app/physical-persons"),
                ("legal-persons", "/app/legal-persons"),
                ("limits", "/app/limits"),
                ("regulatory-reporting", "/app/reports")
            });
    }
}
