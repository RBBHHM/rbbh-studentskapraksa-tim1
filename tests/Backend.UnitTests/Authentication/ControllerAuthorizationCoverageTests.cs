using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using RBBH.ConnectedParties.API.Controllers;

namespace UnitTests.Authentication;

/// <summary>
/// Regression guard: new API controllers must opt in to authentication
/// explicitly, and no action may silently introduce AllowAnonymous.
/// Read-only health probes use mapped endpoints, not these controllers.
/// </summary>
public sealed class ControllerAuthorizationCoverageTests
{
    [Fact]
    public void EveryApiControllerActionRequiresExplicitAuthorization()
    {
        var controllers = typeof(ModulesController).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract
                && typeof(ControllerBase).IsAssignableFrom(type)
                && type.IsDefined(typeof(ApiControllerAttribute), inherit: true))
            .ToArray();

        Assert.True(controllers.Length >= 15,
            $"Expected the application API controller assembly; found {controllers.Length}.");

        foreach (var controller in controllers)
        {
            Assert.False(controller.IsDefined(typeof(AllowAnonymousAttribute), inherit: true),
                $"Public access must not be enabled on {controller.FullName}.");

            var controllerAuthorized = controller.IsDefined(typeof(AuthorizeAttribute), inherit: true);
            var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => !method.IsSpecialName &&
                    method.GetCustomAttributes(inherit: true).OfType<HttpMethodAttribute>().Any())
                .ToArray();

            Assert.NotEmpty(actions);

            foreach (var action in actions)
            {
                Assert.False(action.IsDefined(typeof(AllowAnonymousAttribute), inherit: true),
                    $"Unexpected anonymous route: {controller.Name}.{action.Name}.");

                Assert.True(controllerAuthorized ||
                    action.IsDefined(typeof(AuthorizeAttribute), inherit: true),
                    $"Missing authorization: {controller.Name}.{action.Name}.");
            }
        }
    }
}
