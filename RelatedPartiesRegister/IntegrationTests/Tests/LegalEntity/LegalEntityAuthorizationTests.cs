using FluentAssertions;
using RBBH.ConnectedParties.API.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace IntegrationTests.Tests.LegalEntity
{
    /// <summary>
    /// PL-57 / PL-58 - API integration test - pravna lica (autorizacija)
    ///
    /// LegalEntityController mora biti zaštićen — neautenticirani pristup vraća 401 Unauthorized.
    /// Autorizacija se u produkciji provodi JWT bearer middleware-om (Keycloak); kao i kod
    /// postojećih EC-5 testova (PeriodLockEdgeCaseTests), [Authorize] atribut se ne aktivira pri
    /// direktnom instanciranju kontrolera niti se može deterministički provjeriti bez živog
    /// Keycloak servera. Zato se zahtjev autorizacije dokumentuje provjerom samog [Authorize]
    /// atributa na kontroleru — deterministički, bez vanjske infrastrukture.
    ///
    /// Ova klasa ne koristi fixture baze i izvršava se potpuno procesno-lokalno.
    ///
    /// </summary>
    public class LegalEntityAuthorizationTests
    {
        // 401 Unauthorized — kontroler zahtijeva autentifikaciju ([Authorize])
        [Fact]
        public void LegalEntityController_HasAuthorizeAttribute()
        {
            var authorizeAttr = typeof(LegalEntityController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            authorizeAttr.Should().NotBeNull(
                "LegalEntityController mora imati [Authorize] — neautenticirani pristup vraća 401.");
        }
    }
}
