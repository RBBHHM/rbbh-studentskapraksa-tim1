using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.DTO;
using RBBH.ConnectedParties.DL.DTO.LegalEntity;
using RBBH.ConnectedParties.DL.Entities.LegalEntity;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers.Excel;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Isolated workbook schema, import and field parsing. This is the same
/// LegalEntityService and DbContext, not a second database transaction.
/// </summary>
public partial class LegalEntityService
{
    private static readonly ExcelImportSchema.Column[] ImportColumns =
    [
        new("Naziv / Name", "Naziv", "Name"),
        new("Tip / Type", "Tip", "Type", "Rezidentnost", "Residency"),
        new("Porezni broj / Tax number", "Porezni broj", "Tax number"),
        new("FBA ID", "FBA ID"),
        new("Matični broj / Registration number", "Matbroj", "Matični broj", "Registration number"),
        new("Osnov povezanosti / Connection basis", "Osnov povezanosti", "Connection basis"),
        new("Opis povezanosti / Connection description", "Opis povezanosti", "Connection description"),
        new("Povezano lice s Bankom / Related to the Bank", "Pov. lice sa Bankom", "Povezano lice s Bankom", "Related to the Bank"),
        new("Datum od / Date from", "Datum od", "Date from"),
        new("Datum do / Date to", "Datum do", "Date to")
    ];
    private static readonly ExcelImportSchema.Column[] V2Columns =
    [
        .. ImportColumns,
        new("Matični broj registracije", "Matični broj registracije", "Registration number"),
        new("GCC broj", "GCC broj", "GCC number"), new("GCC naziv", "GCC naziv", "GCC name"),
        new("Velika izloženost", "Velika izloženost", "Large exposure"),
        new("Status", "Status"), new("Kreirao", "Kreirao", "Created by"), new("Kreirano", "Kreirano", "Created at"),
        new("Izmijenio", "Izmijenio", "Modified by"), new("Izmijenjeno", "Izmijenjeno", "Modified at")
    ];

    /// <inheritdoc />
    public async Task<ImportResultDTO> ImportFromExcelAsync(Stream stream, string createdBy)
    {
        using var wb = ExcelImportSchema.OpenWorkbook(stream);
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        var result = new ImportResultDTO();
        var isV2 = ws.LastColumnUsed()?.ColumnNumber() == V2Columns.Length;
        var schemaErrors = ExcelImportSchema.Validate(ws, isV2 ? V2Columns : ImportColumns);
        if (schemaErrors.Count > 0)
        {
            result.Failed = Math.Max(lastRow - 1, 1);
            result.Errors.AddRange(schemaErrors);
            return result;
        }
        var choices = await Helpers.CodebookChoices.Load(_context, _actor?.HttpContext?.RequestAborted ?? default, "OsnovPovezanosti");
        // Stream just the identifiers in one SQL query instead of materializing
        // two complete lists with two round-trips. Keep inactive rows included
        // so historical identifiers cannot be silently reused.
        var usedTaxNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedFbaIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cancellation = _actor?.HttpContext?.RequestAborted ?? default;
        await foreach (var item in _context.LegalEntities.IgnoreQueryFilters().AsNoTracking()
            .Where(entity => entity.TaxNumber != null || entity.FbaId != null)
            .Select(entity => new { entity.TaxNumber, entity.FbaId })
            .AsAsyncEnumerable().WithCancellation(cancellation))
        {
            if (!string.IsNullOrEmpty(item.TaxNumber)) usedTaxNumbers.Add(item.TaxNumber);
            if (!string.IsNullOrEmpty(item.FbaId)) usedFbaIds.Add(item.FbaId);
        }

        for (int row = 2; row <= lastRow; row++)
        {
            _actor?.HttpContext?.RequestAborted.ThrowIfCancellationRequested();
            try
            {
                var name           = ExcelImportSchema.CellText(ws.Cell(row, 1));
                var tipStr         = ExcelImportSchema.CellText(ws.Cell(row, 2));
                var taxNumber      = ExcelImportSchema.CellText(ws.Cell(row, 3));
                var fbaId          = ExcelImportSchema.CellText(ws.Cell(row, 4));
                var matbroj        = ExcelImportSchema.CellText(ws.Cell(row, 5));
                var basisOfConn    = ExcelImportSchema.CellText(ws.Cell(row, 6));
                var connDesc       = ExcelImportSchema.CellText(ws.Cell(row, 7));
                var connBankStr    = ExcelImportSchema.CellText(ws.Cell(row, 8));
                var dateFromStr    = ExcelImportSchema.CellText(ws.Cell(row, 9));
                var dateToStr      = ExcelImportSchema.CellText(ws.Cell(row, 10));

                if (ws.Row(row).Cells(1, isV2 ? 14 : ImportColumns.Length).All(cell => string.IsNullOrWhiteSpace(cell.GetString())))
                    continue; // Ignore only entirely empty rows, never an incomplete record.

                if (string.IsNullOrWhiteSpace(basisOfConn))
                    throw new ValidationException(BackendMessages.Message("validation.registry.connection.required"));

                var isResident = tipStr.Equals("Rezident", StringComparison.OrdinalIgnoreCase) ||
                    tipStr.Equals("Resident", StringComparison.OrdinalIgnoreCase);
                var isNonResident = tipStr.Equals("Nerezident", StringComparison.OrdinalIgnoreCase) ||
                    tipStr.Equals("Non-resident", StringComparison.OrdinalIgnoreCase) ||
                    tipStr.Equals("Nonresident", StringComparison.OrdinalIgnoreCase);
                if (!isResident && !isNonResident)
                    throw new ValidationException(BackendMessages.Message("validation.registry.legal.residency.import"));

                bool? connectedWithBank = connBankStr.Equals("DA", StringComparison.OrdinalIgnoreCase) ? true
                    : connBankStr.Equals("YES", StringComparison.OrdinalIgnoreCase) ? true
                    : connBankStr.Equals("NE", StringComparison.OrdinalIgnoreCase) ? false
                    : connBankStr.Equals("NO", StringComparison.OrdinalIgnoreCase) ? false
                    : null;

                if (connectedWithBank is null)
                    throw new ValidationException(BackendMessages.Message("validation.registry.bank.relation.import"));

                DateTime? dateFrom = ParseOptionalDate(dateFromStr, "Datum od");
                DateTime? dateTo   = ParseOptionalDate(dateToStr, "Datum do");

                var dto = new CreateLegalEntityDTO
                {
                    IsResident         = isResident,
                    TaxNumber          = isResident && !string.IsNullOrWhiteSpace(taxNumber) ? taxNumber : null,
                    FbaId              = !isResident && !string.IsNullOrWhiteSpace(fbaId) ? fbaId : null,
                    Name               = name,
                    Matbroj            = string.IsNullOrWhiteSpace(matbroj) ? null : matbroj,
                    BasisOfConnection  = basisOfConn,
                    ConnectionDescription = string.IsNullOrWhiteSpace(connDesc) ? null : connDesc,
                    ConnectedWithBank  = connectedWithBank,
                    DateFrom           = dateFrom,
                    DateTo             = dateTo,
                    MaticniBroj = isV2 ? ExcelImportSchema.CellText(ws.Cell(row, 11)) : null,
                    GccNumber = isV2 ? ExcelImportSchema.CellText(ws.Cell(row, 12)) : null,
                    IsLargeExposure = isV2 ? ParseExposure(ExcelImportSchema.CellText(ws.Cell(row, 14))) : null
                };
                ValidateCreateDto(dto);
                choices.Check("OsnovPovezanosti", dto.BasisOfConnection, "basisOfConnection");
                var gcc = await ResolveGcc(dto.GccNumber);
                if (dto.ConnectedWithBank is null)
                    throw new ValidationException(BackendMessages.Message("validation.registry.bank.relation.import"));
                var identifier = isResident ? taxNumber : fbaId;
                var identifiers = isResident ? usedTaxNumbers : usedFbaIds;
                if (!identifiers.Add(identifier))
                    throw new ValidationException(isResident
                        ? BackendMessages.Message("validation.registry.legal.tax.duplicate")
                        : BackendMessages.Message("validation.registry.legal.fba.duplicate"));

                var entity = new LegalEntity
                {
                    IsResident = dto.IsResident,
                    TaxNumber = dto.IsResident ? dto.TaxNumber : null,
                    FbaId = dto.IsResident ? null : dto.FbaId,
                    Name = dto.Name,
                    Matbroj = dto.Matbroj,
                    MaticniBroj = dto.MaticniBroj,
                    GccNumber = gcc?.Number, GccName = gcc?.Name, IsLargeExposure = dto.IsLargeExposure ?? false,
                    BasisOfConnection = dto.BasisOfConnection,
                    ConnectionDescription = dto.ConnectionDescription,
                    ConnectedWithBank = dto.ConnectedWithBank,
                    DateFrom = dto.DateFrom,
                    DateTo = dto.DateTo,
                    Status             = "Draft",
                    IsActive           = true,
                    CreatedAt          = DateTime.UtcNow,
                    CreatorSubject = Helpers.RegistryVerification.CreatorSubject(_actor),
                    CreatedBy          = createdBy
                };

                _context.LegalEntities.Add(entity);
                result.Imported++;
            }
            catch (ValidationException exception)
            {
                result.Failed++;
                result.Errors.Add(BackendMessages.Message("workbook.row.error", row, exception.ErrorMessage));
                result.Details.Add(new(row, exception.Field ?? "row", "validation.invalid", exception.ErrorMessage));
            }
            catch (BusinessRuleException exception) when (exception.Status < 500)
            {
                result.Failed++; result.Errors.Add(BackendMessages.Message("workbook.row.error", row, exception.Message));
                result.Details.Add(new(row, "gccNumber", exception.Code, exception.Message));
            }
            catch (Exception exception) when (exception is not OperationCanceledException and not OutOfMemoryException and not BusinessRuleException)
            {
                result.Failed++;
                result.Errors.Add(BackendMessages.Message("workbook.row.unreadable", row));
                result.Details.Add(new(row, "row", "import.invalid_row", "Provjerite format i vrijednosti polja."));
                _logger.LogWarning("Legal import row could not be read. Row {Row}. FailureType {FailureType}", row, exception.GetType().Name);
            }
        }

        if (result.Imported > 0)
        {
            _context.AuditLogs.Add(Helpers.BusinessAudit.Create("LegalEntity", "-", "IMPORT", createdBy, _actor, newValues: new { result.Imported, result.Failed }));
            await _context.SaveChangesAsync(_actor?.HttpContext?.RequestAborted ?? default);
        }

        return result;
    }

    private static bool? ParseExposure(string value) => string.IsNullOrWhiteSpace(value) ? null
        : value.Equals("DA", StringComparison.OrdinalIgnoreCase) || value.Equals("YES", StringComparison.OrdinalIgnoreCase) ? true
        : value.Equals("NE", StringComparison.OrdinalIgnoreCase) || value.Equals("NO", StringComparison.OrdinalIgnoreCase) ? false
        : throw new ValidationException("isLargeExposure", BackendMessages.Message("validation.registry.exposure.import"));

    private static DateTime? ParseOptionalDate(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("bs-BA"), DateTimeStyles.None, out var date) ||
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date;
        throw new ValidationException(BackendMessages.Message("workbook.column.date.invalid", field));
    }

}
