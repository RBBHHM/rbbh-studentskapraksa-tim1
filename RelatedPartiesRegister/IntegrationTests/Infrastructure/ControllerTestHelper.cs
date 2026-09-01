using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationTests.Infrastructure
{
    /// <summary>
    /// Pomoćne metode za postavljanje <see cref="ControllerContext"/> (HttpContext + korisnik)
    /// kod direktnog instanciranja kontrolera u integration testovima.
    /// </summary>
    public static class ControllerTestHelper
    {
        public static T WithHttpContext<T>(this T controller, string userName = "integration-tester")
            where T : ControllerBase
        {
            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, userName) }, "TestAuth");

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            };
            httpContext.Request.Path = "/api/test";
            httpContext.Request.Method = "POST";

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }
    }
}
