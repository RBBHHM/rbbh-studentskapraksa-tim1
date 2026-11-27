using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Sifarnici;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

public partial class CodeListController
{
    [HttpPost("categories/{name}/values"), Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public Task<IActionResult> CreateCells(string name, CodebookCellsRequest request, CancellationToken ct) =>
        SaveCells(name, null, request, ct);

    [HttpPut("categories/{name}/values/{id:int}"), Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public Task<IActionResult> UpdateCells(string name, int id, CodebookCellsRequest request, CancellationToken ct) =>
        SaveCells(name, id, request, ct);

    private async Task<IActionResult> SaveCells(string name, int? id, CodebookCellsRequest request, CancellationToken ct)
    {
        var definition = await db.CodeListDefinitions.SingleOrDefaultAsync(item => item.Name == name, ct);
        if (definition is null) return NotFound();
        if (CodebookDefinitions.UsesReferenceFields(definition) || !definition.IsActive || definition.ModifiedAt != request.ExpectedModifiedAt)
            return Conflict(new { code = "codes.concurrent" });

        var columns = CodebookCsv.Columns(db, definition).ToDictionary(column => column.Slot);
        var cells = new Dictionary<int, object?>();
        if (request.Values is null || !request.Values.Keys.All(columns.ContainsKey))
            return BadRequest(new { code = "codes.schema_invalid" });
        foreach (var column in columns.Values)
        {
            if (!CodebookCsv.TryValue(CodebookCsv.Slot(column.Slot, column.Name), request.Values.GetValueOrDefault(column.Slot) ?? "", out var value))
                return BadRequest(new { code = "codes.cell_invalid", column = column.Name });
            cells.Add(column.Slot, value);
        }
        if (!cells.Values.Any(value => value is not null)) return BadRequest(new { code = "codes.cell_required" });

        var actor = GetKorisnik();
        CodeList row;
        if (id.HasValue)
        {
            var stored = await db.CodeLists.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.ID == id && item.DefinitionId == definition.Id, ct);
            if (stored is null) return NotFound();
            row = stored;
            row.IzmijenjenDatum = DateTime.UtcNow;
            row.IzmijenioKorisnik = actor;
        }
        else
        {
            row = new CodeList { Definition = definition, Kategorija = definition.Name, Kod = "",
                Naziv = "", KreiraoKorisnik = actor, KreiranDatum = DateTime.UtcNow };
            db.CodeLists.Add(row);
        }

        var nextActive = request.Active;
        if (id.HasValue && row.Aktivan && !nextActive && definition.IsSystemRequired)
            return Conflict(new { code = "codes.value_in_use" });

        var before = id.HasValue ? Snapshot() : null;
        row.Aktivan = nextActive;
        CodebookCsv.WriteSlots(db, row, cells);
        // Touch the definition even when only owned text slots change so concurrent schema edits cannot commit with the row.
        definition.ModifiedAt = DateTime.UtcNow;
        definition.ModifiedBy = actor;
        db.AuditLogs.Add(BusinessAudit.Create("CodeList", id.HasValue ? "CODELIST_UPDATE" : "CODELIST_ADD",
            actor, null, before, Snapshot()));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (exception is DbUpdateConcurrencyException ||
            exception.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new { code = "codes.concurrent" });
        }
        if (id.HasValue) return Ok(new { row.ID });
        return CreatedAtAction(nameof(GetByID), new { id = row.ID }, new { row.ID });

        Dictionary<string, object?> Snapshot() =>
            columns.Values.ToDictionary(column => $"ID{column.Slot}: {column.Name}", column => CodebookCsv.ReadValue(db, row, column.Slot));
    }
}
