using System.Security.Claims;
using RBBH.ConnectedParties.Helpers;

namespace UnitTests.Authentication;

public sealed class AuthenticatedActorTests
{
    [Fact]
    public void SnapshotPreservesRawSubjectPrecedenceAndTrustedLocalId()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new("sub", " raw-subject "), new(ClaimTypes.NameIdentifier, "mapped-subject"),
            new(ClaimTypes.Email, " actor@example.test "), new(AuthenticatedActor.ApplicationUserIdClaim, id.ToString())], "Fixture"));
        Assert.Equal(new ActorIdentity(" raw-subject ", "actor@example.test", id), AuthenticatedActor.Capture(principal));
    }

    [Fact]
    public void SnapshotFallsBackToMappedSubjectWithoutInventingLocalId()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new(ClaimTypes.NameIdentifier, "mapped-subject"), new(ClaimTypes.Email, "actor@example.test"),
            new(AuthenticatedActor.ApplicationUserIdClaim, "invalid")], "Fixture"));
        Assert.Equal(new ActorIdentity("mapped-subject", "actor@example.test", null), AuthenticatedActor.Capture(principal));
    }

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
