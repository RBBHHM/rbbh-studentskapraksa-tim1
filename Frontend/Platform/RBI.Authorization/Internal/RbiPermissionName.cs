using System.Text.RegularExpressions;

namespace RBI.Authorization;

internal static partial class RbiPermissionName
{
    internal const string FullAccess = "*";

    internal static string Normalize(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        var normalized = permission.Trim().ToLowerInvariant();
        if (!PermissionPattern().IsMatch(normalized))
            throw new ArgumentException(
                "Permission names must use semantic dot notation, for example 'customers.view'.",
                nameof(permission));
        return normalized;
    }

    internal static string NormalizeGrant(string permission) =>
        string.Equals(permission?.Trim(), FullAccess, StringComparison.Ordinal)
            ? FullAccess
            : Normalize(permission!);

    [GeneratedRegex("^[a-z][a-z0-9]*(?:\\.[a-z][a-z0-9-]*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex PermissionPattern();
}
