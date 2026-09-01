using System.Security.Claims;
using FluentAssertions;
using RBBH.ConnectedParties.API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UnitTests.Controllers
{
    /// <summary>
    /// Testovi za PL-19: prikaz/sakrivanje modula prema roli korisnika (isEnabled flag).
    /// </summary>
    public class ModulesControllerTests
    {
        private static ModulesController CreateControllerForRoles(params string[] roles)
        {
            var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            return new ModulesController
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = principal }
                }
            };
        }

        private static List<ModuleDto> GetModules(ModulesController controller)
        {
            var actionResult = controller.GetModules();
            var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
            return ok.Value.Should().BeOfType<ModulesResponse>().Subject.Modules;
        }

        [Fact]
        public void GetModules_ForMultipleAccesses_EnablesExactlySelectedModules()
        {
            // Arrange
            var controller = CreateControllerForRoles("physical-persons", "limits");

            // Act
            var modules = GetModules(controller);

            // Assert
            modules.Single(m => m.Key == "physical-persons").IsEnabled.Should().BeTrue();
            modules.Single(m => m.Key == "limits").IsEnabled.Should().BeTrue();
            modules.Single(m => m.Key == "legal-persons").IsEnabled.Should().BeFalse();
            modules.Single(m => m.Key == "regulatory-reporting").IsEnabled.Should().BeFalse();
        }

        [Fact]
        public void GetModules_ForReportingAccess_EnablesOnlyReporting()
        {
            // Arrange
            var controller = CreateControllerForRoles("regulatory-reporting");

            // Act
            var modules = GetModules(controller);

            // Assert
            modules.Single(m => m.Key == "regulatory-reporting").IsEnabled.Should().BeTrue();
            modules.Where(m => m.Key != "regulatory-reporting").Should().OnlyContain(m => !m.IsEnabled);
        }

        [Fact]
        public void GetModules_WithNoRoles_AllModulesDisabled()
        {
            // Arrange
            var controller = CreateControllerForRoles();

            // Act
            var modules = GetModules(controller);

            // Assert
            modules.Should().HaveCount(4);
            modules.Should().OnlyContain(m => m.IsEnabled == false);
        }

        [Fact]
        public void GetModules_AlwaysReturnsAllModuleDefinitions()
        {
            // Arrange
            var controller = CreateControllerForRoles("physical-persons");

            // Act
            var modules = GetModules(controller);

            // Assert — bez obzira na rolu, sve definicije modula su prisutne
            modules.Select(m => m.Key).Should().BeEquivalentTo(
                "physical-persons", "legal-persons", "limits", "regulatory-reporting");
        }
    }
}
