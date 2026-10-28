using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace RBI.Authentication.Internal;

internal sealed class RbiDistributedTokenSessionStore(
    IDistributedCache cache,
    IDataProtectionProvider dataProtectionProvider) : IRbiTokenSessionStore
{
    private const string CacheKeyPrefix = "rbi:authentication:session:";
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector(
        "RBI.Authentication.TokenSession.v1");

    public async ValueTask<RbiTokenSession?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var protectedPayload = await cache.GetAsync(CacheKey(sessionId), cancellationToken);
        if (protectedPayload is null) return null;

        try
        {
            var payload = protector.Unprotect(protectedPayload);
            return JsonSerializer.Deserialize<RbiTokenSession>(payload);
        }
        catch (Exception exception) when (
            exception is System.Security.Cryptography.CryptographicException or JsonException)
        {
            await cache.RemoveAsync(CacheKey(sessionId), cancellationToken);
            return null;
        }
    }

    public async ValueTask SetAsync(
        string sessionId,
        RbiTokenSession session,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(session);
        var protectedPayload = protector.Protect(payload);
        await cache.SetAsync(
            CacheKey(sessionId),
            protectedPayload,
            new DistributedCacheEntryOptions { AbsoluteExpiration = session.SessionExpiresAt },
            cancellationToken);
    }

    public ValueTask RemoveAsync(string sessionId, CancellationToken cancellationToken) =>
        new(cache.RemoveAsync(CacheKey(sessionId), cancellationToken));

    private static string CacheKey(string sessionId) => CacheKeyPrefix + sessionId;
}
