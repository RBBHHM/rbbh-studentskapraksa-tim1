using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

namespace RBI.DesignSystem.Components.Forms;

internal static class RbiFormUtilities
{
    internal static string Id(string prefix) => $"rbi-{prefix}-{Guid.NewGuid():N}";

    internal static string? Attribute(
        IReadOnlyDictionary<string, object>? attributes,
        string name) =>
        attributes?.TryGetValue(name, out var value) == true ? value?.ToString() : null;

    internal static IReadOnlyDictionary<string, object>? SafeAttributes(
        IReadOnlyDictionary<string, object>? attributes,
        params string[] reserved)
    {
        if (attributes is null) return null;
        var blocked = new HashSet<string>(reserved, StringComparer.OrdinalIgnoreCase);
        return attributes
            .Where(pair => !blocked.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    internal static string? JoinIds(params string?[] ids)
    {
        var values = ids.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct();
        var joined = string.Join(' ', values);
        return joined.Length == 0 ? null : joined;
    }

    internal static FieldIdentifier? FieldFor<TValue>(Expression<Func<TValue>>? expression) =>
        expression is null ? null : FieldIdentifier.Create(expression);
}
