namespace RBI.Authentication.Internal;

internal sealed class RbiUserAccessTokenProvider(
    IRbiAuthenticationSessionContext sessionContext,
    IRbiTokenSessionStore sessionStore,
    IRbiRefreshTokenClient refreshClient,
    RbiRefreshCoordinator refreshCoordinator,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    internal async ValueTask<RbiTokenAcquisitionResult> GetAccessTokenAsync(
        CancellationToken cancellationToken)
    {
        var sessionId = await sessionContext.GetSessionIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(sessionId))
            return RbiTokenAcquisitionResult.ReauthenticationRequired;

        var session = await sessionStore.GetAsync(sessionId, cancellationToken);
        if (session is null || session.SessionExpiresAt <= timeProvider.GetUtcNow())
        {
            await RemoveSessionAsync(sessionId, cancellationToken);
            return RbiTokenAcquisitionResult.ReauthenticationRequired;
        }

        if (session.AccessTokenExpiresAt > timeProvider.GetUtcNow() + RefreshSkew)
            return RbiTokenAcquisitionResult.Success(session.AccessToken);

        try
        {
            return await refreshCoordinator.RunAsync(
                sessionId,
                token => RefreshAsync(sessionId, token),
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Host shutdown is not evidence of an invalid identity session.
            return refreshCoordinator.IsStopping
                ? RbiTokenAcquisitionResult.TemporarilyUnavailable
                : RbiTokenAcquisitionResult.ReauthenticationRequired;
        }
    }

    private async ValueTask<RbiTokenAcquisitionResult> RefreshAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var session = await sessionStore.GetAsync(sessionId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (session is null || session.SessionExpiresAt <= now)
        {
            await RemoveSessionAsync(sessionId, cancellationToken);
            return RbiTokenAcquisitionResult.ReauthenticationRequired;
        }

        if (session.AccessTokenExpiresAt > now + RefreshSkew)
            return RbiTokenAcquisitionResult.Success(session.AccessToken);

        var refreshed = await refreshClient.RefreshAsync(session.RefreshToken, cancellationToken);
        if (refreshed.Status == RbiRefreshStatus.ReauthenticationRequired)
        {
            await RemoveSessionAsync(sessionId, cancellationToken);
            return RbiTokenAcquisitionResult.ReauthenticationRequired;
        }

        if (refreshed.Status == RbiRefreshStatus.TemporarilyUnavailable)
            return RbiTokenAcquisitionResult.TemporarilyUnavailable;

        var updated = session with
        {
            AccessToken = refreshed.AccessToken!,
            RefreshToken = refreshed.RefreshToken ?? session.RefreshToken,
            AccessTokenExpiresAt = now + refreshed.ExpiresIn!.Value,
        };
        // Within this process, logout cancels this flight before removing the session. Refuse to write after
        // cancellation, and undo a write that raced past the check. The coordinator is process-local: a refresh
        // running in another instance is not cancelled by this logout. The undo is unconditional because a session
        // id is random, single-use and written only by this session's single in-process flight.
        cancellationToken.ThrowIfCancellationRequested();
        await sessionStore.SetAsync(sessionId, updated, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            // Shutdown alone must not delete a successfully persisted rotated token.
            // Logout still wins, including when it races host shutdown.
            if (refreshCoordinator.IsSessionInvalidated(sessionId))
                await sessionStore.RemoveAsync(sessionId, CancellationToken.None);
            cancellationToken.ThrowIfCancellationRequested();
        }
        return RbiTokenAcquisitionResult.Success(updated.AccessToken);
    }

    private async ValueTask RemoveSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        await sessionStore.RemoveAsync(sessionId, cancellationToken);
        refreshCoordinator.Forget(sessionId);
    }
}
