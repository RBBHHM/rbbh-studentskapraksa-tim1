using ClosedXML.Excel;

namespace RBBH.ConnectedParties.Helpers.Excel;

internal static class WorkbookRelationships
{
    internal static string? Key(int kind, string? value) => string.IsNullOrWhiteSpace(value) ? null : $"{kind}:{value.Trim().ToUpperInvariant()}";
    internal static string[] Keys(string? jmbg, string? passport, string? fba) =>
        new[] { Key(0, jmbg), Key(1, passport), Key(2, fba) }.OfType<string>().ToArray();

    // Kahn ordering avoids recursion/stack exhaustion on the 5,000-row input boundary.
    internal static (List<int> Ordered, List<int> Blocked) ParentFirst(IXLWorksheet sheet, int lastRow, ISet<string> existing)
    {
        var rows = Enumerable.Range(2, Math.Max(0, lastRow - 1)).Where(row => sheet.Row(row).Cells(1, 20).Any(cell => !string.IsNullOrWhiteSpace(cell.GetString()))).ToArray();
        var identifiers = rows.SelectMany(row => Keys(sheet.Cell(row, 4).GetString(), sheet.Cell(row, 5).GetString(), sheet.Cell(row, 6).GetString()).Select(key => (key, row)))
            .ToLookup(item => item.key, item => item.row);
        var parents = rows.ToDictionary(row => row, row => Keys(sheet.Cell(row, 18).GetString(), sheet.Cell(row, 19).GetString(), sheet.Cell(row, 20).GetString())
            .Where(key => !existing.Contains(key)).SelectMany(key => identifiers[key]).Distinct().ToHashSet());
        var children = parents.SelectMany(item => item.Value.Select(parent => (parent, child: item.Key))).ToLookup(item => item.parent, item => item.child);
        var queue = new Queue<int>(rows.Where(row => parents[row].Count == 0)); var ordered = new List<int>();
        while (queue.TryDequeue(out var row))
        {
            ordered.Add(row);
            foreach (var child in children[row]) if (parents[child].Remove(row) && parents[child].Count == 0) queue.Enqueue(child);
        }
        var processed = ordered.ToHashSet(); return (ordered, rows.Where(row => !processed.Contains(row)).ToList());
    }
}
