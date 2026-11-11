using ClosedXML.Excel;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Report;
using RBBH.ConnectedParties.DL.Entities.Report;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers.Excel;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace RBBH.ConnectedParties.BL.Services;

public class ReportService : IReportService
{
    private readonly ConnectedPartiesDbContext _context;
    private readonly ILogger<ReportService> _logger;
    private readonly IHttpContextAccessor? _actor;
    private readonly IFinanceMonthlySource? _finance;
    private CancellationToken Cancellation => _actor?.HttpContext?.RequestAborted ?? default;

    public ReportService(ConnectedPartiesDbContext context, ILogger<ReportService> logger, IHttpContextAccessor? actor = null, IFinanceMonthlySource? finance = null)
    {
        _context = context;
        _logger = logger; _actor = actor; _finance = finance;
    }

    public async Task<ReportDTO> GenerateDailyReportAsync(string createdBy)
    {
        var today = DateTime.UtcNow.Date;
        var snapshot = await BuildExposureSnapshotAsync();

        var report = new Report
        {
            ReportType = "DAILY",
            ReportDate = today,
            TotalClients = snapshot.Count,
            ClientsWithBreachedLimit = snapshot.Count(s => s.IsLimitBreached),
            TotalExposure = SnapshotTotal(snapshot),
            DataSnapshot = JsonSerializer.Serialize(snapshot),
            DataSource = "local.clientlimits",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Reports.Add(report);
        _context.AuditLogs.Add(Helpers.BusinessAudit.Create("Report", report.Id.ToString(), "GENERATE", createdBy, _actor,
            newValues: new { report.ReportType, report.ReportDate, report.DataSource, report.TotalClients, report.TotalExposure }));
        await _context.SaveChangesAsync(Cancellation);

        return MapReportToDto(report);
    }

    public async Task<ReportDTO> GenerateMonthlyReportAsync(int year, int month, string createdBy)
    {
        if (year < 2000 || year > DateTime.UtcNow.Year) throw new ValidationException("year", BackendMessages.Message("errors.report.year.invalid"));
        if (month is < 1 or > 12) throw new ValidationException("month", BackendMessages.Message("errors.report.month.range"));
        var reportDate = new DateTime(year, month, 1);
        var snapshot = await BuildExposureSnapshotAsync(year, month);

        var report = new Report
        {
            ReportType = "MONTHLY",
            ReportDate = reportDate,
            TotalClients = snapshot.Count,
            ClientsWithBreachedLimit = snapshot.Count(s => s.IsLimitBreached),
            TotalExposure = SnapshotTotal(snapshot),
            DataSnapshot = JsonSerializer.Serialize(snapshot),
            DataSource = _finance?.Enabled == true ? "finance.monthlyhistory" : "local.clientlimits",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Reports.Add(report);
        _context.AuditLogs.Add(Helpers.BusinessAudit.Create("Report", report.Id.ToString(), "GENERATE", createdBy, _actor,
            newValues: new { report.ReportType, report.ReportDate, report.DataSource, report.TotalClients, report.TotalExposure }));
        await _context.SaveChangesAsync(Cancellation);

        return MapReportToDto(report);
    }

    public Task<ReportListDTO> GetDailyReportsAsync(int page, int pageSize) =>
        ListReportsAsync("DAILY", page, pageSize);

    public Task<ReportListDTO> GetMonthlyReportsAsync(int page, int pageSize) =>
        ListReportsAsync("MONTHLY", page, pageSize);

    // Both report lists use identical filtering, stable ordering and bounded
    // pagination; centralizing their read path avoids diverging contracts.
    private async Task<ReportListDTO> ListReportsAsync(string reportType, int page, int pageSize)
    {
        page = Math.Clamp(page, 1, 1_000_000);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = _context.Reports.AsNoTracking()
            .Where(r => r.ReportType == reportType)
            .OrderByDescending(r => r.ReportDate).ThenByDescending(r => r.CreatedAt).ThenBy(r => r.Id);

        var total = await query.CountAsync(Cancellation);
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => MapReportToDto(r))
            .ToListAsync(Cancellation);

        return new ReportListDTO { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<byte[]> ExportClientByIdAsync(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Length > 50)
            throw new ValidationException("identifier", BackendMessages.Message("validation.report.identifier.required"));

        var id = identifier.Trim().ToLower();

        // Pronađi pravno lice po matičnom broju, poreznom broju ili FBA ID-u
        var legalEntity = await _context.LegalEntities.AsNoTracking()
            .Where(e => e.IsActive &&
                ((e.TaxNumber != null && e.TaxNumber.ToLower() == id) ||
                 (e.FbaId != null && e.FbaId.ToLower() == id) ||
                 (e.Matbroj != null && e.Matbroj.ToLower() == id) ||
                 (e.MaticniBroj != null && e.MaticniBroj.ToLower() == id)))
            .FirstOrDefaultAsync();

        if (legalEntity is null)
            throw new ValidationException("identifier",
                BackendMessages.Message("validation.report.legal.missing", identifier.Trim()));

        var rows = await GetLimitExportRowsAsync(legalEntity.Id);

        if (rows.Count == 0)
            throw new ValidationException("identifier",
                BackendMessages.Message("validation.report.limits.missing", legalEntity.Name));

        return BuildLimitsExcel(rows);
    }

    public async Task<byte[]> ExportAllClientsWithLimitsAsync()
    {
        var rows = await GetLimitExportRowsAsync(legalEntityId: null);
        return BuildLimitsExcel(rows);
    }

    public async Task<byte[]> ExportGeneratedReportAsync(Guid reportId)
    {
        var report = await _context.Reports.AsNoTracking().FirstOrDefaultAsync(item => item.Id == reportId && item.IsActive)
            ?? throw new ValidationException("reportId", BackendMessages.Message("validation.report.missing"));
        var snapshot = string.IsNullOrWhiteSpace(report.DataSnapshot)
            ? []
            : JsonSerializer.Deserialize<List<ExposureSnapshotRow>>(report.DataSnapshot) ?? [];
        return GenerateExposureExcel(snapshot, BackendMessages.Message("workbook.report.title", BackendMessages.Message(report.ReportType == "DAILY" ? "labels.report.daily" : "labels.report.monthly"), report.ReportDate));
    }

    // ─── Privatne metode ────────────────────────────────────────────────────

    private async Task<List<ExposureSnapshotRow>> BuildExposureSnapshotAsync(int? year = null, int? month = null)
    {
        var rows = await (from capital in _context.ClientLimits.AsNoTracking()
            join entity in _context.LegalEntities.AsNoTracking() on capital.LegalEntityId equals entity.Id
            orderby entity.Name, entity.Id
            select new ExposureSnapshotRow { Naziv = entity.Name, Matbroj = entity.Matbroj,
                GccNumber = entity.GccNumber, GccName = entity.GccName, IsLargeExposure = entity.IsLargeExposure,
                ExposureLimit = capital.ExposureLimit,
                CurrentExposure = capital.CurrentExposure, Currency = capital.Currency, IsLimitBreached = capital.IsLimitBreached,
                RegulatoryCapital = capital.RegulatoryCapital, CoreCapital = capital.CoreCapital, CreatedBy = capital.CreatedBy }).Take(20_001).ToListAsync(Cancellation);
        if (rows.Count > 20_000) throw new BusinessRuleException(503, "report.resource_limit");
        if (year.HasValue && month.HasValue && _finance?.Enabled == true)
        {
            if (rows.Any(row => string.IsNullOrWhiteSpace(row.Matbroj) || row.Currency != "BAM") || rows.GroupBy(row => row.Matbroj).Any(group => group.Count() > 1))
                throw new BusinessRuleException(409, "finance.registry_reconciliation");
            var exposures = await _finance.Read(year.Value, month.Value, rows.Select(row => row.Matbroj!).ToArray(), Cancellation);
            var map = exposures.ToDictionary(row => row.CustomerKey, StringComparer.Ordinal);
            if (rows.Any(row => !map.ContainsKey(row.Matbroj!))) throw new BusinessRuleException(503, "finance.incomplete_period");
            var date = new DateTime(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value));
            var capital = await CapitalHistory.AsOf(_context, date, Cancellation);
            if (rows.Count > 0 && capital is null) throw new BusinessRuleException(409, "finance.capital_missing");
            foreach (var row in rows)
            {
                row.CurrentExposure = map[row.Matbroj!].TotalExposure; row.IsLimitBreached = row.CurrentExposure > row.ExposureLimit;
                row.RegulatoryCapital = capital!.RegulatoryCapital; row.CoreCapital = capital.CoreCapital;
            }
        }
        return rows;
    }

    private static decimal SnapshotTotal(IReadOnlyList<ExposureSnapshotRow> rows)
    {
        const decimal maximum = 9_999_999_999_999_999.99m;
        try
        {
            if (rows.Any(row => new[] { row.CurrentExposure, row.ExposureLimit, row.RegulatoryCapital, row.CoreCapital }
                    .Any(value => Math.Abs(value) > maximum || decimal.Round(value, 2) != value)))
                throw new OverflowException();
            var total = rows.Sum(row => row.CurrentExposure);
            if (Math.Abs(total) > maximum) throw new OverflowException();
            return total;
        }
        catch (OverflowException)
        {
            throw new BusinessRuleException(409, "report.amount_out_of_range");
        }
    }

    private async Task<List<LimitExportRow>> GetLimitExportRowsAsync(Guid? legalEntityId)
    {
        var baseQuery =
            from limit in _context.Limiti.AsNoTracking()
            join entity in _context.LegalEntities.AsNoTracking() on limit.LegalEntityId equals entity.Id
            join capital in _context.ClientLimits.AsNoTracking() on limit.LegalEntityId equals capital.LegalEntityId into capitalGroup
            from capital in capitalGroup.DefaultIfEmpty()
            select new { limit, entity, capital };

        if (legalEntityId.HasValue)
            baseQuery = baseQuery.Where(x => x.limit.LegalEntityId == legalEntityId.Value);

        return await baseQuery
            .OrderBy(x => x.entity.Name)
            .Select(x => new LimitExportRow
            {
                Naziv = x.entity.Name,
                IsResident = x.entity.IsResident,
                FbaId = x.entity.FbaId,
                TaxNumber = x.entity.TaxNumber,
                MaticniBroj = x.entity.MaticniBroj,
                Matbroj = x.entity.Matbroj,
                TipLimita = x.limit.TipLimita,
                OdobreniLimit = x.limit.OdobreniLimit,
                MaksimalnaOcekivanaUtilizacija = x.limit.MaksimalnaOcekivanaUtilizacija,
                RokOcekivaneUtilizacije = x.limit.RokOcekivaneUtilizacije,
                Komentar = x.limit.Komentar,
                RegulatorniKapital = x.capital != null ? x.capital.RegulatoryCapital : (decimal?)null,
                OsnovniKapital = x.capital != null ? x.capital.CoreCapital : (decimal?)null,
            })
            .ToListAsync(Cancellation);
    }

    private static byte[] BuildLimitsExcel(List<LimitExportRow> rows) =>
        RegistryExcelExporter.Create(
            "Limiti",
            ["Naziv", "Rezidentnost", "FBA ID", "Porezni broj", "Matični broj", "Matbroj", "Tip limita", "Odobreni limit", "Maksimalna očekivana utilizacija", "Rok očekivane utilizacije", "Komentar", "Regulatorni kapital", "Osnovni kapital"],
            rows.Select(r => (IReadOnlyList<object?>)
            [r.Naziv, r.IsResident ? "Rezident" : "Nerezident", r.FbaId, r.TaxNumber, r.MaticniBroj, r.Matbroj, r.TipLimita, r.OdobreniLimit, r.MaksimalnaOcekivanaUtilizacija, r.RokOcekivaneUtilizacije, r.Komentar, r.RegulatorniKapital, r.OsnovniKapital]));

    private static byte[] GenerateExposureExcel(List<ExposureSnapshotRow> rows, string sheetTitle)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Izvještaj");

        ws.Cell(1, 1).Value = sheetTitle;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontName = "Amalia";
        ws.Cell(1, 1).Style.Font.FontSize = 20;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromArgb(0x18, 0x18, 0x18);
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFF, 0xE6, 0x00);
        ws.Row(1).Height = 34;
        ws.Range(1, 1, 1, 7).Merge();

        ws.Cell(2, 1).Value = BackendMessages.Message("workbook.generated.at", DateTime.Now);
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Font.FontName = "Amalia";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromArgb(0x55, 0x55, 0x55);
        ws.Range(2, 1, 2, 7).Merge();

        string[] headers =
        [
            "Naziv", "Limit izloženosti", "Trenutna izloženost", "Valuta",
            "Regulatorni kapital", "Osnovni kapital", "Kreirao"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(3, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontName = "Amalia";
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x1A, 0x1A, 0x1A);
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        ws.Row(3).Height = 26;

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var row = 4 + i;
            var bg = i % 2 == 0 ? XLColor.White : XLColor.FromArgb(0xF5, 0xF5, 0xF5);
            var breachedBg = XLColor.FromArgb(0xFF, 0xEB, 0xEB);
            var rowBg = r.IsLimitBreached ? breachedBg : bg;

            ws.Cell(row, 1).Value = r.Naziv;
            SetNum(ws.Cell(row, 2), r.ExposureLimit, rowBg);
            SetNum(ws.Cell(row, 3), r.CurrentExposure, rowBg);
            ws.Cell(row, 4).Value = r.Currency;
            SetNum(ws.Cell(row, 5), r.RegulatoryCapital, rowBg);
            SetNum(ws.Cell(row, 6), r.CoreCapital, rowBg);
            ws.Cell(row, 7).Value = r.CreatedBy;

            for (int c = 1; c <= 7; c++)
            {
                ws.Cell(row, c).Style.Font.FontName = "Amalia";
                if (c is not (2 or 3 or 5 or 6))
                    ws.Cell(row, c).Style.Fill.BackgroundColor = rowBg;
            }
        }

        var tableRange = ws.Range(3, 1, Math.Max(3, 3 + rows.Count), 7);
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        tableRange.Style.Border.InsideBorderColor = XLColor.FromArgb(0xDD, 0xDD, 0xDD);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.SetAutoFilter();
        ws.Columns().AdjustToContents();
        ws.Columns(1, 7).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 28);
        ws.SheetView.FreezeRows(3);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetNum(IXLCell cell, decimal value, XLColor bg)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = "#,##0.00";
        cell.Style.Fill.BackgroundColor = bg;
    }

    private static ReportDTO MapReportToDto(Report r) => new()
    {
        Id = r.Id,
        DataSource = r.DataSource ?? "legacy.unspecified",
        ReportType = r.ReportType,
        ReportDate = r.ReportDate,
        TotalClients = r.TotalClients,
        ClientsWithBreachedLimit = r.ClientsWithBreachedLimit,
        TotalExposure = r.TotalExposure,
        CreatedBy = r.CreatedBy,
        CreatedAt = r.CreatedAt
    };

    /// <summary>Snapshot reda izloženosti/kapitala klijenta — koristi se za DataSnapshot izvještaja (Kapital modul).</summary>
    private sealed class ExposureSnapshotRow
    {
        // Registry metadata captured at generation, not a new finance classification rule.
        public string? GccNumber { get; set; }
        public string? GccName { get; set; }
        public bool? IsLargeExposure { get; set; }
        public string? Matbroj { get; set; }
        public string Naziv { get; set; } = string.Empty;
        public decimal ExposureLimit { get; set; }
        public decimal CurrentExposure { get; set; }
        public string Currency { get; set; } = "BAM";
        public bool IsLimitBreached { get; set; }
        public decimal RegulatoryCapital { get; set; }
        public decimal CoreCapital { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>Red za Excel export limita (po vrsti limita, jedan klijent ili svi klijenti).</summary>
    private sealed class LimitExportRow
    {
        public string Naziv { get; set; } = string.Empty;
        public bool IsResident { get; set; }
        public string? FbaId { get; set; }
        public string? TaxNumber { get; set; }
        public string? MaticniBroj { get; set; }
        public string? Matbroj { get; set; }
        public string TipLimita { get; set; } = string.Empty;
        public decimal? OdobreniLimit { get; set; }
        public decimal? MaksimalnaOcekivanaUtilizacija { get; set; }
        public DateTime? RokOcekivaneUtilizacije { get; set; }
        public string? Komentar { get; set; }
        public decimal? RegulatorniKapital { get; set; }
        public decimal? OsnovniKapital { get; set; }
    }
}
