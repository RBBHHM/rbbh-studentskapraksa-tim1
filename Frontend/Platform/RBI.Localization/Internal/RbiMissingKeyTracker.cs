using System.Collections.Concurrent;

namespace RBI.Localization.Internal;

internal sealed class RbiMissingKeyTracker
{
    private readonly ConcurrentDictionary<string, byte> _seen = new(StringComparer.Ordinal);

    internal bool MarkFirst(string cultureName, string key) => _seen.TryAdd($"{cultureName}\n{key}", 0);
}
