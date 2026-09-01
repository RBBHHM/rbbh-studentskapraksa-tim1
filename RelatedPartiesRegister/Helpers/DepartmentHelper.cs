namespace RBBH.ConnectedParties.Helpers;

public static class DepartmentHelper
{
    public static string? ExtractDepartment(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            return null;

        var dotIndex = username.IndexOf('.');
        if (dotIndex <= 0) return null;

        return username[..dotIndex].ToLowerInvariant();
    }
}
