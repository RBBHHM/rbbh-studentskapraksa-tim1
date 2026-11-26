using System.Text.RegularExpressions;

namespace RBBH.ConnectedParties.DL.Entities.Sifarnici;

internal static class CodebookName
{
    internal static string Canonical(string name)
    {
        var acronym = Regex.Replace(name, "([A-Z])([A-Z][a-z])", "$1_$2");
        var words = Regex.Replace(acronym, "([a-z0-9])([A-Z])", "$1_$2");
        return Regex.Replace(words, "[^A-Za-z0-9]+", "_").Trim('_').ToUpperInvariant();
    }
}
