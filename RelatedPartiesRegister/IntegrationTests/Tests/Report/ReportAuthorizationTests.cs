using FluentAssertions;
using RBBH.ConnectedParties.API.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace IntegrationTests.Tests.Report
{
    /// <summary>
    /// Testiranje autorizacije izvještaja i izvoza.
    ///
    /// ReportController mora biti zaštićen — neautenticirani pristup vraća 401 Unauthorized.
    /// Kao i kod ostalih kontrolera, autorizacija se u produkciji provodi JWT/Keycloak middleware-om
    /// koji se ne aktivira pri direktnom instanciranju; zato se zahtjev dokumentuje provjerom
    /// [Authorize] atributa (deterministički, bez vanjske infrastrukture).
    /// </summary>
    public class ReportAuthorizationTests
    {
        // 401 Unauthorized — kontroler zahtijeva autentifikaciju ([Authorize])
        [Fact]
        public void ReportController_HasAuthorizeAttribute()
        {
            var authorizeAttr = typeof(ReportController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            authorizeAttr.Should().NotBeNull(
                "ReportController mora imati [Authorize] — izvještaji i export nisu javni.");
        }
    }
}
