using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Limiti;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.DL.Entities.LegalEntity;
using RBBH.ConnectedParties.DL.Entities.Report;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions.Validations;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RBBH.ConnectedParties.DL.DTO;
using RBBH.ConnectedParties.Helpers.Excel;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Implementacija CRUD operacija nad limitima.
/// </summary>
public class LimitService(ConnectedPartiesDbContext dbContext, IHttpContextAccessor? actor = null) : ILimitService
{
    private readonly ConnectedPartiesDbContext _dbContext = dbContext;

    // ─── READ ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<Result<List<LimitResponseDTO>>> GetAll(Guid? legalEntityId = null, string? matbroj = null) =>
        GetAsOf(legalEntityId, matbroj, DateTime.UtcNow.Date, actor?.HttpContext?.RequestAborted ?? default);

    public Task<Result<List<LimitResponseDTO>>> GetAsOf(Guid? legalEntityId, string? matbroj, DateTime asOf, CancellationToken ct) =>
        Read(legalEntityId, matbroj, asOf, ct, null);
    public Task<Result<List<LimitResponseDTO>>> ExportData(Guid? legalEntityId, string? matbroj, DateTime? asOf, CancellationToken ct) =>
        Read(legalEntityId, matbroj, asOf ?? DateTime.UtcNow.Date, ct, 5000);
    private async Task<Result<List<LimitResponseDTO>>> Read(Guid? legalEntityId, string? matbroj, DateTime asOf, CancellationToken ct, int? maximum)
    {
        var query = BaseQuery();

        if (legalEntityId.HasValue)
            query = query.Where(x => x.Limit.LegalEntityId == legalEntityId.Value);
        else if (!string.IsNullOrWhiteSpace(matbroj))
            query = query.Where(x => x.Entity.MaticniBroj == matbroj.Trim() || x.Entity.Matbroj == matbroj.Trim());

        var capital = await GetGlobalCapitalAsync(asOf, ct);

        IQueryable<LimitJoinRow> ordered = query.OrderBy(x => x.Entity.Name).ThenBy(x => x.Limit.Id);
        if (maximum.HasValue) ordered = ordered.Take(maximum.Value + 1);
        var items = await ordered.Select(x => ToResponseDto(x.Limit, x.Entity)).ToListAsync(ct);
        if (maximum.HasValue && items.Count > maximum.Value)
            return Result<List<LimitResponseDTO>>.ValidationError("Dozvoljeno je najviše 5.000 redova po izvozu.");
        foreach (var item in items)
            ApplyCapital(item, capital);

        return Result<List<LimitResponseDTO>>.Success(items);
    }

    public async Task<ReferencePage<LimitResponseDTO>> Page(Guid? legalEntityId, string? search, int page, int pageSize, CancellationToken ct, DateTime? asOf = null)
    {
        if (search?.Length > 200) throw new RBBH.ConnectedParties.Exceptions.BusinessRuleException(400, "registry.invalid_search");
        page = Math.Clamp(page, 1, 1_000_000); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = BaseQuery();
        if (legalEntityId.HasValue) query = query.Where(row => row.Entity.Id == legalEntityId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant(); query = query.Where(row => row.Entity.Name.ToUpper().Contains(term) || row.Limit.TipLimita.ToUpper().Contains(term));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(row => row.Entity.Name).ThenBy(row => row.Limit.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(row => ToResponseDto(row.Limit, row.Entity)).ToListAsync(ct);
        var capital = await GetGlobalCapitalAsync(asOf, ct); foreach (var item in items) ApplyCapital(item, capital);
        return new(items, total, page, pageSize);
    }

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> GetByID(int id)
    {
        if (id < 1)
            return Result<LimitResponseDTO>.ValidationError("ID nije validan.");

        var item = await BaseQuery()
            .Where(x => x.Limit.Id == id)
            .Select(x => ToResponseDto(x.Limit, x.Entity))
            .FirstOrDefaultAsync();

        if (item is null)
            return Result<LimitResponseDTO>.NotFoundError($"Limit s ID={id} nije pronađen.");

        ApplyCapital(item, await GetGlobalCapitalAsync());

        return Result<LimitResponseDTO>.Success(item);
    }

    // ─── CREATE ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> Create(CreateLimitDTO dto, string korisnik)
    {
        var validacija = await ValidateDto(dto.LegalEntityId, dto.TipLimita, dto.OdobreniLimit, dto.MaksimalnaOcekivanaUtilizacija, dto.Komentar);
        if (validacija is not null)
            return validacija;

        var entitet = new Limit
        {
            LegalEntityId = dto.LegalEntityId,
            TipLimita = dto.TipLimita.Trim().ToUpper(),
            OdobreniLimit = dto.OdobreniLimit,
            MaksimalnaOcekivanaUtilizacija = dto.MaksimalnaOcekivanaUtilizacija,
            RokOcekivaneUtilizacije = dto.RokOcekivaneUtilizacije,
            Komentar = dto.Komentar?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = korisnik,
        };

        var ids = await PersistNew([entitet], korisnik, "INSERT", actor?.HttpContext?.RequestAborted ?? default);
        return await GetByID(ids[0]);
    }

    // ─── UPDATE ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> Update(int id, UpdateLimitDTO dto, string korisnik)
    {
        if (id < 1)
            return Result<LimitResponseDTO>.ValidationError("ID nije validan.");

        var validacija = await ValidateDto(dto.LegalEntityId, dto.TipLimita, dto.OdobreniLimit, dto.MaksimalnaOcekivanaUtilizacija, dto.Komentar);
        if (validacija is not null)
            return validacija;

        var entitet = await _dbContext.Limiti
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entitet is null)
            return Result<LimitResponseDTO>.NotFoundError($"Limit s ID={id} nije pronađen.");

        var before = Snapshot(entitet);
        entitet.LegalEntityId = dto.LegalEntityId;
        entitet.TipLimita = dto.TipLimita.Trim().ToUpper();
        entitet.OdobreniLimit = dto.OdobreniLimit;
        entitet.MaksimalnaOcekivanaUtilizacija = dto.MaksimalnaOcekivanaUtilizacija;
        entitet.RokOcekivaneUtilizacije = dto.RokOcekivaneUtilizacije;
        entitet.Komentar = dto.Komentar?.Trim();
        entitet.ModifiedAt = DateTime.UtcNow;
        entitet.ModifiedBy = korisnik;

        if (entitet.ImportFingerprint is not null) entitet.ImportFingerprint = Fingerprint(entitet);
        _dbContext.AuditLogs.Add(Helpers.BusinessAudit.Create("Limit", entitet.Id.ToString(CultureInfo.InvariantCulture), "UPDATE", korisnik, actor, before, Snapshot(entitet)));
        await _dbContext.SaveChangesAsync(actor?.HttpContext?.RequestAborted ?? default);
        return await GetByID(entitet.Id);
    }

    // ─── DELETE ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<Result<bool>> Delete(int id) => Delete(id, actor?.HttpContext is { } context ? Helpers.AuthenticatedActor.Username(context.User) : "");
    public async Task<Result<bool>> Delete(int id, string korisnik)
    {
        if (id < 1)
            return Result<bool>.ValidationError("ID nije validan.");

        var entitet = await _dbContext.Limiti
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entitet is null)
            return Result<bool>.NotFoundError($"Limit s ID={id} nije pronađen.");

        if (string.IsNullOrWhiteSpace(korisnik)) throw new Helpers.MissingActorIdentityException();
        _dbContext.Limiti.Remove(entitet);
        _dbContext.AuditLogs.Add(Helpers.BusinessAudit.Create("Limit", entitet.Id.ToString(CultureInfo.InvariantCulture), "DELETE", korisnik, actor, Snapshot(entitet)));
        await _dbContext.SaveChangesAsync(actor?.HttpContext?.RequestAborted ?? default);

        return Result<bool>.Success(true);
    }

    private static readonly string[] ImportHeaders = ["Naziv", "Rezidentnost", "FBA ID", "Porezni broj", "Matični broj", "Matbroj", "Tip limita", "Odobreni limit", "Maksimalna očekivana utilizacija", "Rok očekivane utilizacije", "Komentar", "Regulatorni kapital", "Osnovni kapital", "Datum kapitala", "Kreirao", "Kreirano", "Izmijenio", "Izmijenjeno"];
    public async Task<ImportResultDTO> Import(Stream stream, string korisnik, CancellationToken ct)
    {
        using var workbook = ExcelImportSchema.OpenWorkbook(stream); var sheet = workbook.Worksheet(1);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1; var count = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var result = new ImportResultDTO();
        var columns = ImportHeaders.Take(count == 14 ? 14 : 18).Select(header => new ExcelImportSchema.Column(header, header)).ToArray();
        var errors = ExcelImportSchema.Validate(sheet, columns);
        if (errors.Count > 0) { result.Failed = Math.Max(1, lastRow - 1); result.Errors.AddRange(errors); return result; }
        var parties = await _dbContext.LegalEntities.AsNoTracking().Select(row => new { row.Id, row.TaxNumber, row.FbaId, row.MaticniBroj, row.Matbroj }).ToListAsync(ct);
        static string[] PartyKeys(string? tax, string? fba, string? registration, string? matbroj) => new[] { tax, fba, registration, matbroj }
            .Select((value, index) => string.IsNullOrWhiteSpace(value) ? null : index + ":" + value.Trim().ToUpperInvariant()).OfType<string>().ToArray();
        var identity = parties.SelectMany(row => PartyKeys(row.TaxNumber, row.FbaId, row.MaticniBroj, row.Matbroj).Select(key => (key, row.Id)))
            .GroupBy(row => row.key).ToDictionary(group => group.Key, group => group.Select(row => row.Id).Distinct().ToArray());
        var types = (await _dbContext.CodeLists.Where(row => row.Kategorija == "VrstaLimita" && (row.Definition == null || row.Definition.IsActive)).Select(row => row.Kod).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pending = new List<(int Row, Limit Value)>(); var fileKeys = new HashSet<RowKey>();
        void Failure(int row, string field, string code, string message)
        { result.Failed++; result.Errors.Add($"Red {row}: {message}"); result.Details.Add(new(row, field, code, message)); }
        for (var row = 2; row <= lastRow; row++)
        {
            ct.ThrowIfCancellationRequested();
            if (sheet.Row(row).Cells(1, 11).All(cell => string.IsNullOrWhiteSpace(cell.GetString()))) continue;
            try
            {
                var keys = PartyKeys(ExcelImportSchema.CellText(sheet.Cell(row, 4)), ExcelImportSchema.CellText(sheet.Cell(row, 3)),
                    ExcelImportSchema.CellText(sheet.Cell(row, 5)), ExcelImportSchema.CellText(sheet.Cell(row, 6)));
                var matches = keys.Where(identity.ContainsKey).SelectMany(key => identity[key]).Distinct().ToArray();
                if (keys.Length == 0 || keys.Any(key => !identity.ContainsKey(key)) || matches.Length != 1)
                    throw new RBBH.ConnectedParties.Exceptions.ValidationException("legalEntityId", "Identifikatori ne odgovaraju jednom aktivnom pravnom licu.");
                var type = ExcelImportSchema.CellText(sheet.Cell(row, 7)).ToUpperInvariant();
                var approved = Amount(sheet.Cell(row, 8), "odobreniLimit"); var utilization = Amount(sheet.Cell(row, 9), "maksimalnaOcekivanaUtilizacija");
                var comment = ExcelImportSchema.CellText(sheet.Cell(row, 11));
                var fieldError = ValidateFields(type, approved, utilization, comment);
                if (fieldError is not null) throw new RBBH.ConnectedParties.Exceptions.ValidationException("row", fieldError);
                if (!types.Contains(type)) throw new RBBH.ConnectedParties.Exceptions.ValidationException("tipLimita", "Vrsta limita nije aktivna u šifrarniku.");
                var due = ExcelImportSchema.CellText(sheet.Cell(row, 10)); DateTime? date = null;
                if (due.Length > 0)
                {
                    if (!DateTime.TryParse(due, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) &&
                        !DateTime.TryParse(due, CultureInfo.GetCultureInfo("bs-BA"), DateTimeStyles.None, out parsed))
                        throw new RBBH.ConnectedParties.Exceptions.ValidationException("rokOcekivaneUtilizacije", "Provjerite format datuma očekivane utilizacije.");
                    date = parsed;
                }
                var value = new Limit { LegalEntityId = matches[0], TipLimita = type, OdobreniLimit = approved, MaksimalnaOcekivanaUtilizacija = utilization,
                    RokOcekivaneUtilizacije = date, Komentar = string.IsNullOrWhiteSpace(comment) ? null : comment, CreatedBy = korisnik, CreatedAt = DateTime.UtcNow };
                value.ImportFingerprint = Fingerprint(value);
                if (!fileKeys.Add(Key(value))) { Failure(row, "row", "import.duplicate", "Isti limit je ponovljen u datoteci."); continue; }
                pending.Add((row, value));
            }
            catch (RBBH.ConnectedParties.Exceptions.ValidationException error) { Failure(row, error.Field ?? "row", "validation.invalid", error.ErrorMessage); }
            catch (Exception error) when (error is FormatException or ArgumentException or OverflowException)
            { Failure(row, "row", "import.invalid_row", "Provjerite format i vrijednosti polja."); }
        }
        var ids = pending.Select(item => item.Value.LegalEntityId).Distinct().ToArray();
        var existing = (await _dbContext.Limiti.AsNoTracking().Where(row => ids.Contains(row.LegalEntityId)).ToListAsync(ct)).Select(Key).ToHashSet();
        var accepted = new List<Limit>();
        foreach (var item in pending)
            if (existing.Contains(Key(item.Value))) Failure(item.Row, "row", "import.duplicate", "Isti limit već postoji za odabrano pravno lice.");
            else accepted.Add(item.Value);
        if (accepted.Count > 0) { await PersistNew(accepted, korisnik, "IMPORT", ct); result.Imported = accepted.Count; }
        return result;
    }
    private static decimal? Amount(ClosedXML.Excel.IXLCell cell, string field)
    {
        if (string.IsNullOrWhiteSpace(cell.GetString())) return null;
        if (cell.DataType == ClosedXML.Excel.XLDataType.Number && cell.TryGetValue<decimal>(out var number)) return number;
        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        var text = cell.GetString().Trim();
        if (decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out number) || decimal.TryParse(text, styles, CultureInfo.GetCultureInfo("bs-BA"), out number)) return number;
        throw new RBBH.ConnectedParties.Exceptions.ValidationException(field, "Unesite ispravan iznos bez separatora hiljada.");
    }

    private static object Snapshot(Limit row) => new { row.LegalEntityId, row.TipLimita, row.OdobreniLimit, row.MaksimalnaOcekivanaUtilizacija, row.RokOcekivaneUtilizacije, row.Komentar };
    private static string? ValidateFields(string? type, decimal? approved, decimal? utilization, string? comment)
    {
        const decimal maximum = 9_999_999_999_999_999.99m;
        if (string.IsNullOrWhiteSpace(type)) return "Tip limita je obavezan.";
        if (type.Length > 100) return "Vrsta limita smije imati najviše 100 znakova.";
        if (approved < 0) return "Odobreni limit ne može biti negativan.";
        if (utilization < 0) return "Maksimalna očekivana utilizacija ne može biti negativna.";
        if (comment?.Length > 1000) return "Komentar smije imati najviše 1.000 znakova.";
        if (new[] { approved, utilization }.Any(value => value.HasValue && (value < 0 || value > maximum || decimal.Round(value.Value, 2) != value.Value)))
            return "Iznosi moraju biti nenegativni, s najviše dvije decimale i do 16 cifara prije decimalnog zareza.";
        return null;
    }
    private sealed record RowKey(Guid Party, string Type, string? Approved, string? Utilization, long? Due, string? Comment);
    private static RowKey Key(Limit row) => new(row.LegalEntityId, row.TipLimita.Trim().ToUpperInvariant(),
        row.OdobreniLimit?.ToString("G29", CultureInfo.InvariantCulture), row.MaksimalnaOcekivanaUtilizacija?.ToString("G29", CultureInfo.InvariantCulture),
        row.RokOcekivaneUtilizacije?.Ticks, string.IsNullOrWhiteSpace(row.Komentar) ? null : row.Komentar.Trim());
    private static string Fingerprint(Limit row) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Key(row)))));
    private static Limit Copy(Limit row) => new() { LegalEntityId = row.LegalEntityId, TipLimita = row.TipLimita, OdobreniLimit = row.OdobreniLimit,
        MaksimalnaOcekivanaUtilizacija = row.MaksimalnaOcekivanaUtilizacija, RokOcekivaneUtilizacije = row.RokOcekivaneUtilizacije, Komentar = row.Komentar,
        CreatedAt = row.CreatedAt, CreatedBy = row.CreatedBy, ImportFingerprint = row.ImportFingerprint };
    private async Task<int[]> PersistNew(IReadOnlyList<Limit> templates, string username, string action, CancellationToken ct)
    {
        var auditId = Guid.NewGuid();
        return await _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // If commit acknowledgement was lost, retry returns its existing IDs instead of writing duplicates.
            var previous = await _dbContext.AuditLogs.AsNoTracking().SingleOrDefaultAsync(row => row.Id == auditId, ct);
            if (previous is not null)
            {
                using var saved = JsonDocument.Parse(previous.NewValues!);
                return saved.RootElement.GetProperty("ids").EnumerateArray().Select(value => value.GetInt32()).ToArray();
            }
            var rows = templates.Select(Copy).ToArray();
            RBBH.ConnectedParties.DL.Entities.Audit.AuditLog? audit = null;
            await using var transaction = _dbContext.Database.IsRelational() ? await _dbContext.Database.BeginTransactionAsync(ct) : null;
            try
            {
                _dbContext.Limiti.AddRange(rows);
                if (_dbContext.Database.IsRelational()) await _dbContext.SaveChangesAsync(ct);
                var ids = rows.Select(row => row.Id).ToArray();
                audit = Helpers.BusinessAudit.Create("Limit", action == "INSERT" ? ids[0].ToString(CultureInfo.InvariantCulture) : "-", action, username, actor,
                    newValues: new { ids, values = rows.Select(Snapshot).ToArray() });
                audit.Id = auditId; _dbContext.AuditLogs.Add(audit);
                await _dbContext.SaveChangesAsync(ct);
                if (transaction is not null) await transaction.CommitAsync(ct);
                return ids;
            }
            catch
            {
                foreach (var row in rows) _dbContext.Entry(row).State = EntityState.Detached;
                if (audit is not null) _dbContext.Entry(audit).State = EntityState.Detached;
                throw;
            }
        });
    }

    // ─── Privatne metode ────────────────────────────────────────────────────

    private sealed class LimitJoinRow
    {
        public required Limit Limit { get; init; }
        public required LegalEntity Entity { get; init; }
    }

    private IQueryable<LimitJoinRow> BaseQuery() =>
        from limit in _dbContext.Limiti.AsNoTracking()
        join entity in _dbContext.LegalEntities.AsNoTracking() on limit.LegalEntityId equals entity.Id
        select new LimitJoinRow { Limit = limit, Entity = entity };

    /// <summary>Kapital je jedan globalni zapis — čita se odvojeno i primjenjuje na svaki Limit red.</summary>
    private Task<DL.DTO.Report.CapitalDTO?> GetGlobalCapitalAsync(DateTime? asOf = null, CancellationToken ct = default) =>
        CapitalHistory.AsOf(_dbContext, asOf ?? DateTime.UtcNow.Date, ct);

    private static void ApplyCapital(LimitResponseDTO dto, DL.DTO.Report.CapitalDTO? capital)
    {
        dto.RegulatorniKapital = capital?.RegulatoryCapital;
        dto.OsnovniKapital = capital?.CoreCapital;
        dto.DatumKapitala = capital?.CapitalDate;
    }

    private static LimitResponseDTO ToResponseDto(Limit limit, LegalEntity entity) => new()
    {
        Id = limit.Id,
        LegalEntityId = limit.LegalEntityId,
        Naziv = entity.Name,
        IsResident = entity.IsResident,
        FbaId = entity.FbaId,
        TaxNumber = entity.TaxNumber,
        MaticniBroj = entity.MaticniBroj,
        Matbroj = entity.Matbroj,
        TipLimita = limit.TipLimita,
        OdobreniLimit = limit.OdobreniLimit,
        MaksimalnaOcekivanaUtilizacija = limit.MaksimalnaOcekivanaUtilizacija,
        RokOcekivaneUtilizacije = limit.RokOcekivaneUtilizacije,
        Komentar = limit.Komentar,
        CreatedAt = limit.CreatedAt,
        CreatedBy = limit.CreatedBy,
        ModifiedAt = limit.ModifiedAt,
        ModifiedBy = limit.ModifiedBy,
    };

    /// <summary>
    /// Validacije: pravno lice mora postojati, tip limita je obavezan i mora postojati
    /// kao aktivna vrijednost šifarnika VrstaLimita, iznosi ne mogu biti negativni.
    /// </summary>
    private async Task<Result<LimitResponseDTO>?> ValidateDto(
        Guid legalEntityId,
        string tipLimita,
        decimal? odobreniLimit,
        decimal? maksimalnaOcekivanaUtilizacija, string? komentar)
    {
        if (legalEntityId == Guid.Empty)
            return Result<LimitResponseDTO>.ValidationError("Pravno lice je obavezno.");

        var legalEntityExists = await _dbContext.LegalEntities.AsNoTracking()
            .AnyAsync(e => e.Id == legalEntityId && e.IsActive);
        if (!legalEntityExists)
            return Result<LimitResponseDTO>.ValidationError("Odabrano pravno lice ne postoji.");

        var fieldsError = ValidateFields(tipLimita, odobreniLimit, maksimalnaOcekivanaUtilizacija, komentar);
        if (fieldsError is not null) return Result<LimitResponseDTO>.ValidationError(fieldsError);
        var kod = tipLimita.Trim().ToUpper();
        var tipLimitaPostoji = await _dbContext.CodeLists.AsNoTracking()
            .AnyAsync(c => c.Kategorija.ToUpper() == "VRSTALIMITA" && c.Kod.ToUpper() == kod && c.Aktivan && (c.Definition == null || c.Definition.IsActive));
        if (!tipLimitaPostoji)
            return Result<LimitResponseDTO>.ValidationError($"Tip limita '{tipLimita}' ne postoji u šifarniku vrsta limita.");

        return null;
    }
}
