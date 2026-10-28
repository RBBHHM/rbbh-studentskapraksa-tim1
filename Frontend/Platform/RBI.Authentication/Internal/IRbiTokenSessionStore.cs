namespace RBI.Authentication.Internal;

internal interface IRbiTokenSessionStore
{
    ValueTask<RbiTokenSession?> GetAsync(string sessionId, CancellationToken cancellationToken);

    ValueTask SetAsync(string sessionId, RbiTokenSession session, CancellationToken cancellationToken);

    ValueTask RemoveAsync(string sessionId, CancellationToken cancellationToken);
}
