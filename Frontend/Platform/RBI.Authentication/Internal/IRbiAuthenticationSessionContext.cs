namespace RBI.Authentication.Internal;

internal interface IRbiAuthenticationSessionContext
{
    ValueTask<string?> GetSessionIdAsync(CancellationToken cancellationToken);
}
