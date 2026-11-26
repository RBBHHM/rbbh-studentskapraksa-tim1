using RBBH.ConnectedParties.DL.DTO.Sifarnici;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.BL.Services;

internal static class CodebookDefinitions
{
    internal static bool ValidColumns(CodebookColumnInput[] columns) =>
        columns.Length is >= 1 and <= 20 &&
        columns.All(column => column is not null && column.Slot is >= 1 and <= 20 &&
            column.Position is >= 1 and <= 20 && column.Name?.Trim() is { Length: >= 1 and <= 128 } name &&
            name[0] is not ('=' or '+' or '-' or '@') && !name.Any(char.IsControl) &&
            column.Description is null or { Length: <= 4000 }) &&
        columns.Select(column => column.Slot).Distinct().Count() == columns.Length &&
        columns.Select(column => column.Position).Distinct().Count() == columns.Length &&
        columns.Select(column => column.Name!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == columns.Length;

    internal static void Apply(ConnectedPartiesDbContext db, CodeListDefinition definition, CodebookColumnInput[] columns)
    {
        definition.UsesCustomColumns = true;
        var entry = db.Entry(definition);
        var bySlot = columns.ToDictionary(column => column.Slot);
        for (var slot = 1; slot <= 20; slot++)
        {
            var column = bySlot.GetValueOrDefault(slot);
            entry.Property($"ColumnName{slot}").CurrentValue = column?.Name?.Trim();
            entry.Property($"ColumnDescription{slot}").CurrentValue = column?.Description?.Trim();
            entry.Property($"ColumnPosition{slot}").CurrentValue = column?.Position;
        }
    }
}
