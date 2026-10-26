using System.Security.Claims;
using RBBH.ConnectedParties.Helpers;

namespace UnitTests.Authentication;

public sealed class AuthenticatedActorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingEmailCannotFallBackToNameOrSubject(string? email)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "not-an-actor-email"), new("sub", "fixture-subject") };
        if (email is not null) claims.Add(new(ClaimTypes.Email, email));
        Assert.Throws<MissingActorIdentityException>(() => AuthenticatedActor.Email(new(new ClaimsIdentity(claims, "Fixture"))));
    }

    [Fact]
    public void ActualAuthenticatedEmailWinsOverDisplayNameAndSubject()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new(ClaimTypes.Email, " actual@example.test "), new(ClaimTypes.Name, "Display name"), new("sub", "fixture-subject")], "Fixture"));
        Assert.Equal("actual@example.test", AuthenticatedActor.Email(principal));
    }

    [Fact]
    public void UnauthenticatedEmailIsNotAnActor()
    {
        Assert.Throws<MissingActorIdentityException>(() => AuthenticatedActor.Email(
            new(new ClaimsIdentity([new Claim(ClaimTypes.Email, "anonymous@example.test")]))));
    }
}
