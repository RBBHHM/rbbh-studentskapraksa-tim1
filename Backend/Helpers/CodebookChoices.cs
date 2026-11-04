using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;

namespace RBBH.ConnectedParties.Helpers;

/// <summary>Read configured business catalogs once per operation/import; accept legacy labels alongside stable codes.</summary>
internal sealed class CodebookChoices
{
    private readonly Dictionary<string, HashSet<string>> _values = new(StringComparer.OrdinalIgnoreCase);
    internal static async Task<CodebookChoices> Load(ConnectedPartiesDbContext db, CancellationToken ct, params string[] categories)
    {
        var names = categories.Select(category => category.ToUpperInvariant()).ToArray();
        var definitions = await db.CodeListDefinitions.AsNoTracking().Where(row => names.Contains(row.Name.Trim().ToUpper())).ToListAsync(ct);
        var rows = await db.CodeLists.IgnoreQueryFilters().AsNoTracking().Where(row => names.Contains(row.Kategorija.Trim().ToUpper()))
            .Select(row => new { row.Kategorija, row.Kod, row.Naziv, row.Aktivan }).ToListAsync(ct);
        var result = new CodebookChoices();
        foreach (var category in categories)
        {
            var definition = definitions.SingleOrDefault(row => row.Name.Trim().Equals(category, StringComparison.OrdinalIgnoreCase));
            var entries = rows.Where(row => row.Kategorija.Trim().Equals(category, StringComparison.OrdinalIgnoreCase)).ToArray();
            // Absent legacy catalogs do not silently introduce an incompatible mandatory setup requirement.
            // Once defined (including an empty/inactive catalog), clients cannot submit unknown/inactive choices.
            if (definition is null && entries.Length == 0) continue;
            result._values[category] = entries.Where(row => row.Aktivan && definition?.IsActive != false)
                .SelectMany(row => new[] { row.Kod.Trim(), row.Naziv.Trim() }).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        return result;
    }
    internal void Check(string category, string? value, string field)
    {
        if (!string.IsNullOrWhiteSpace(value) && _values.TryGetValue(category, out var choices) && !choices.Contains(value.Trim()))
            throw new ValidationException(field, BackendMessages.Message("validation.codelist.choice.inactive"));
    }
}
