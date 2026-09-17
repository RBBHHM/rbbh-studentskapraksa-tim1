using ClosedXML.Excel;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Report;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.DL.Entities.Report;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace RBBH.ConnectedParties.BL.Services;

public class ReportService : IReportService
{
    private readonly ConnectedPartiesDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ConnectedPartiesDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ReportDTO> GenerateDailyReportAsync(string createdBy)
    {
        var today = DateTime.UtcNow.Date;
        var limits = await _context.Limiti.AsNoTracking().Include(l => l.LegalEntity).ToListAsync();
        var capital = await CapitalAt(today);

        var report = new Report
        {
            ReportType = "DAILY",
            ReportDate = today,
            TotalClients = limits.Select(l => l.Naziv).Distinct().Count(),
            ClientsWithBreachedLimit = limits.Count(l => l.MaksimalnoOcekivanaUtilizacija > (l.KorigovaniLimit ?? l.IznosLimita)),
            TotalExposure = limits.Sum(l => l.MaksimalnoOcekivanaUtilizacija),
            DataSnapshot = JsonSerializer.Serialize(limits.Select(l => new
            {
                l.Id, l.Naziv, l.TipLimita,
                l.IznosLimita, l.MaksimalnoOcekivanaUtilizacija,
                l.RokUtilizacije, l.Komentar,
                RegulatorniKapital = capital?.RegulatorniKapital ?? 0,
                OsnovniKapital = capital?.OsnovniKapital ?? 0,
                DatumKapitala = capital?.DatumKapitala,
                l.LegalEntityId,
                LegalEntity = l.LegalEntity == null ? null : new
                {
                    l.LegalEntity.IsResident, l.LegalEntity.FbaId, l.LegalEntity.TaxNumber,
                    l.LegalEntity.Matbroj, l.LegalEntity.MaticniBroj
                },
                l.ModifiedAt, l.CreatedAt, l.ModifiedBy
            })),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        return MapReportToDto(report);
    }

    public async Task<ReportDTO> GenerateMonthlyReportAsync(int year, int month, string createdBy)
    {
        var reportDate = new DateTime(year, month, 1);
        var limits = await _context.Limiti.AsNoTracking().Include(l => l.LegalEntity).ToListAsync();
        var capital = await CapitalAt(reportDate.AddMonths(1).AddTicks(-1));

        var report = new Report
        {
            ReportType = "MONTHLY",
            ReportDate = reportDate,
            TotalClients = limits.Select(l => l.Naziv).Distinct().Count(),
            ClientsWithBreachedLimit = limits.Count(l => l.MaksimalnoOcekivanaUtilizacija > (l.KorigovaniLimit ?? l.IznosLimita)),
            TotalExposure = limits.Sum(l => l.MaksimalnoOcekivanaUtilizacija),
            DataSnapshot = JsonSerializer.Serialize(limits.Select(l => new
            {
                l.Id, l.Naziv, l.TipLimita,
                l.IznosLimita, l.MaksimalnoOcekivanaUtilizacija,
                l.RokUtilizacije, l.Komentar,
                RegulatorniKapital = capital?.RegulatorniKapital ?? 0,
                OsnovniKapital = capital?.OsnovniKapital ?? 0,
                DatumKapitala = capital?.DatumKapitala,
                l.LegalEntityId,
                LegalEntity = l.LegalEntity == null ? null : new
                {
                    l.LegalEntity.IsResident, l.LegalEntity.FbaId, l.LegalEntity.TaxNumber,
                    l.LegalEntity.Matbroj, l.LegalEntity.MaticniBroj
                },
                l.ModifiedAt, l.CreatedAt, l.ModifiedBy
            })),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        return MapReportToDto(report);
    }

    public async Task<ReportListDTO> GetDailyReportsAsync(int page, int pageSize)
    {
        var query = _context.Reports.AsNoTracking()
            .Where(r => r.ReportType == "DAILY")
            .OrderByDescending(r => r.ReportDate);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => MapReportToDto(r))
            .ToListAsync();

        return new ReportListDTO { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<ReportListDTO> GetMonthlyReportsAsync(int page, int pageSize)
    {
        var query = _context.Reports.AsNoTracking()
            .Where(r => r.ReportType == "MONTHLY")
            .OrderByDescending(r => r.ReportDate);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => MapReportToDto(r))
            .ToListAsync();

        return new ReportListDTO { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<byte[]> ExportClientByIdAsync(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ValidationException("identifier", "Identifikator je obavezan.");

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
                $"Pravno lice s identifikatorom '{identifier.Trim()}' nije pronađeno.");

        var limits = await _context.Limiti.AsNoTracking().Include(l => l.LegalEntity)
            .Where(l => l.LegalEntityId == legalEntity.Id).OrderBy(l => l.TipLimita).ToListAsync();
        // Legacy records without a foreign key are matched by client name.
        if (limits.Count == 0)
        {
            limits = await _context.Limiti.AsNoTracking()
                .Include(limit => limit.LegalEntity)
                .Where(limit => limit.Naziv.ToLower() == legalEntity.Name.ToLower())
                .OrderBy(limit => limit.TipLimita)
                .ToListAsync();
        }

        // Older reporting installations may only have ClientLimits.
        if (limits.Count == 0)
        {
            var clientLimits = await _context.ClientLimits.AsNoTracking()
                .Where(l => l.LegalEntityId == legalEntity.Id && l.IsActive).ToListAsync();
            limits = clientLimits.Select(l => new Limit
            {
                Naziv = legalEntity.Name, TipLimita = "REG", IznosLimita = l.ExposureLimit,
                MaksimalnoOcekivanaUtilizacija = l.CurrentExposure, CreatedBy = l.CreatedBy,
                LegalEntityId = legalEntity.Id, LegalEntity = legalEntity
            }).ToList();
        }

        if (limits.Count == 0)
            throw new ValidationException("identifier",
                $"Klijent '{legalEntity.Name}' nema definisanih limita.");

        return GenerateExcel(limits, $"Klijent — {legalEntity.Name}", await CapitalAt(DateTime.UtcNow.Date));
    }

    public async Task<byte[]> ExportAllClientsWithLimitsAsync()
    {
        var limits = await _context.Limiti.AsNoTracking()
            .Include(l => l.LegalEntity)
            .OrderBy(l => l.Naziv)
            .ToListAsync();

        return GenerateExcel(limits, "Svi klijenti s limitima", await CapitalAt(DateTime.UtcNow.Date));
    }

    public async Task<byte[]> ExportGeneratedReportAsync(Guid reportId)
    {
        var report = await _context.Reports.AsNoTracking().FirstOrDefaultAsync(item => item.Id == reportId && item.IsActive)
            ?? throw new ValidationException("reportId", "Izvještaj nije pronađen.");
        var limits = string.IsNullOrWhiteSpace(report.DataSnapshot)
            ? []
            : JsonSerializer.Deserialize<List<Limit>>(report.DataSnapshot) ?? [];
        return GenerateExcel(limits, $"{(report.ReportType == "DAILY" ? "Dnevni" : "Mjesečni")} izvještaj — {report.ReportDate:dd.MM.yyyy}", includeSnapshotCapital: true);
    }

    private Task<RBBH.ConnectedParties.DL.Entities.Capital.Capital?> CapitalAt(DateTime date) => _context.Capitals.AsNoTracking()
        .Where(x => x.DatumKapitala <= date).OrderByDescending(x => x.DatumKapitala).ThenByDescending(x => x.Id).FirstOrDefaultAsync();

    private static byte[] GenerateExcel(List<Limit> limits, string sheetTitle, RBBH.ConnectedParties.DL.Entities.Capital.Capital? capital = null, bool includeSnapshotCapital = false)
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
        ws.Range(1, 1, 1, 16).Merge();

        ws.Cell(2, 1).Value = $"Generisano: {DateTime.Now:dd.MM.yyyy HH:mm}";
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Font.FontName = "Amalia";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromArgb(0x55, 0x55, 0x55);
        ws.Range(2, 1, 2, 16).Merge();

        string[] headers =
        [
            "Redni broj", "Rezidentnost", "FBA_ID", "Porez_broj", "Matbroj/JMBG", "Naziv",
            "Tip limita", "Odobreni limit", "Maksimalna očekivana utilizacija",
            "Rok očekivane utilizacije", "Komentar", "Regulatorni kapital", "Osnovni kapital",
            "Datum kapitala", "Datum izmjene", "user_verified"
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

        for (int i = 0; i < limits.Count; i++)
        {
            var l = limits[i];
            var row = 4 + i;
            var bg = i % 2 == 0 ? XLColor.White : XLColor.FromArgb(0xF5, 0xF5, 0xF5);
            var breachedBg = XLColor.FromArgb(0xFF, 0xEB, 0xEB);
            bool breached = l.MaksimalnoOcekivanaUtilizacija > (l.KorigovaniLimit ?? l.IznosLimita);

            var client = l.LegalEntity;
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = client is null ? "" : client.IsResident ? "Rezident" : "Nerezident";
            ws.Cell(row, 3).Value = client?.FbaId ?? "";
            ws.Cell(row, 4).Value = client?.TaxNumber ?? "";
            ws.Cell(row, 5).Value = client?.Matbroj ?? client?.MaticniBroj ?? "";
            ws.Cell(row, 6).Value = client?.Name ?? l.Naziv;
            ws.Cell(row, 7).Value = l.TipLimita;
            SetNum(ws.Cell(row, 8), l.IznosLimita, breached ? breachedBg : bg);
            SetNum(ws.Cell(row, 9), l.MaksimalnoOcekivanaUtilizacija, breached ? breachedBg : bg);
            if (l.RokUtilizacije.HasValue) ws.Cell(row, 10).Value = l.RokUtilizacije.Value;
            ws.Cell(row, 11).Value = l.Komentar ?? "";
            if (capital is not null || (includeSnapshotCapital && l.DatumKapitala.HasValue))
            {
                SetNum(ws.Cell(row, 12), capital?.RegulatorniKapital ?? l.RegulatorniKapital, bg);
                SetNum(ws.Cell(row, 13), capital?.OsnovniKapital ?? l.OsnovniKapital, bg);
                ws.Cell(row, 14).Value = capital?.DatumKapitala ?? l.DatumKapitala!.Value;
            }
            ws.Cell(row, 15).Value = l.ModifiedAt ?? l.CreatedAt;
            ws.Cell(row, 16).Value = l.ModifiedBy ?? "";

            for (int c = 1; c <= 16; c++)
            {
                ws.Cell(row, c).Style.Font.FontName = "Amalia";
                ws.Cell(row, c).Style.Fill.BackgroundColor = c is >= 8 and <= 13
                    ? ws.Cell(row, c).Style.Fill.BackgroundColor
                    : (breached ? breachedBg : bg);
            }
        }

        var tableRange = ws.Range(3, 1, Math.Max(3, 3 + limits.Count), 16);
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        tableRange.Style.Border.InsideBorderColor = XLColor.FromArgb(0xDD, 0xDD, 0xDD);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.SetAutoFilter();
        ws.Columns().AdjustToContents();
        ws.Columns(1, 16).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Column(6).Width = Math.Max(ws.Column(6).Width, 28);
        ws.Column(10).Style.DateFormat.Format = "dd.MM.yyyy";
        ws.Columns(14, 15).Style.DateFormat.Format = "dd.MM.yyyy";
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
        ReportType = r.ReportType,
        ReportDate = r.ReportDate,
        TotalClients = r.TotalClients,
        ClientsWithBreachedLimit = r.ClientsWithBreachedLimit,
        TotalExposure = r.TotalExposure,
        CreatedBy = r.CreatedBy,
        CreatedAt = r.CreatedAt
    };
}
