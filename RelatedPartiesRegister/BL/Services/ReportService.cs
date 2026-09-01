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
        var limits = await _context.Limiti.AsNoTracking().ToListAsync();

        var report = new Report
        {
            ReportType = "DAILY",
            ReportDate = today,
            TotalClients = limits.Select(l => l.Naziv).Distinct().Count(),
            ClientsWithBreachedLimit = limits.Count(l => l.RaspoloziviLimit < 0),
            TotalExposure = limits.Sum(l => l.Utilizacija),
            DataSnapshot = JsonSerializer.Serialize(limits.Select(l => new
            {
                l.Id, l.Naziv, l.TipLimita,
                l.IznosLimita, l.Utilizacija,
                l.RaspoloziviLimit, l.RegulatorniKapital, l.OsnovniKapital
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
        var limits = await _context.Limiti.AsNoTracking().ToListAsync();

        var report = new Report
        {
            ReportType = "MONTHLY",
            ReportDate = reportDate,
            TotalClients = limits.Select(l => l.Naziv).Distinct().Count(),
            ClientsWithBreachedLimit = limits.Count(l => l.RaspoloziviLimit < 0),
            TotalExposure = limits.Sum(l => l.Utilizacija),
            DataSnapshot = JsonSerializer.Serialize(limits.Select(l => new
            {
                l.Id, l.Naziv, l.TipLimita,
                l.IznosLimita, l.Utilizacija,
                l.RaspoloziviLimit, l.RegulatorniKapital, l.OsnovniKapital
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

        var clientLimits = await _context.ClientLimits.AsNoTracking()
            .Where(l => l.LegalEntityId == legalEntity.Id && l.IsActive)
            .ToListAsync();
        var limits = clientLimits.Select(l => new Limit
        {
            Naziv = legalEntity.Name,
            TipLimita = "REG",
            IznosLimita = l.ExposureLimit,
            Utilizacija = l.CurrentExposure,
            RaspoloziviLimit = l.ExposureLimit - l.CurrentExposure,
            RegulatorniKapital = l.RegulatoryCapital,
            OsnovniKapital = l.CoreCapital,
            CreatedBy = l.CreatedBy
        }).ToList();

        // Kompatibilnost sa postojećim podacima: prije uvođenja ClientLimits
        // limiti su bili vezani poslovnim nazivom klijenta.
        if (limits.Count == 0)
        {
            limits = await _context.Limiti.AsNoTracking()
                .Where(limit => limit.Naziv.ToLower() == legalEntity.Name.ToLower())
                .OrderBy(limit => limit.TipLimita)
                .ToListAsync();
        }

        if (limits.Count == 0)
            throw new ValidationException("identifier",
                $"Klijent '{legalEntity.Name}' nema definisanih limita.");

        return GenerateExcel(limits, $"Klijent — {legalEntity.Name}");
    }

    public async Task<byte[]> ExportAllClientsWithLimitsAsync()
    {
        var limits = await _context.Limiti.AsNoTracking()
            .OrderBy(l => l.Naziv)
            .ToListAsync();

        return GenerateExcel(limits, "Svi klijenti s limitima");
    }

    public async Task<byte[]> ExportGeneratedReportAsync(Guid reportId)
    {
        var report = await _context.Reports.AsNoTracking().FirstOrDefaultAsync(item => item.Id == reportId && item.IsActive)
            ?? throw new ValidationException("reportId", "Izvještaj nije pronađen.");
        var limits = string.IsNullOrWhiteSpace(report.DataSnapshot)
            ? []
            : JsonSerializer.Deserialize<List<Limit>>(report.DataSnapshot) ?? [];
        return GenerateExcel(limits, $"{(report.ReportType == "DAILY" ? "Dnevni" : "Mjesečni")} izvještaj — {report.ReportDate:dd.MM.yyyy}");
    }

    private static byte[] GenerateExcel(List<Limit> limits, string sheetTitle)
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
        ws.Range(1, 1, 1, 9).Merge();

        ws.Cell(2, 1).Value = $"Generisano: {DateTime.Now:dd.MM.yyyy HH:mm}";
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Font.FontName = "Amalia";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromArgb(0x55, 0x55, 0x55);
        ws.Range(2, 1, 2, 9).Merge();

        string[] headers =
        [
            "Naziv", "Tip limita", "Iznos limita", "Utilizacija",
            "Korigovani limit", "Raspoloživi limit",
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

        for (int i = 0; i < limits.Count; i++)
        {
            var l = limits[i];
            var row = 4 + i;
            var bg = i % 2 == 0 ? XLColor.White : XLColor.FromArgb(0xF5, 0xF5, 0xF5);
            var breachedBg = XLColor.FromArgb(0xFF, 0xEB, 0xEB);
            bool breached = l.RaspoloziviLimit < 0;

            ws.Cell(row, 1).Value = l.Naziv;
            ws.Cell(row, 2).Value = l.TipLimita;
            SetNum(ws.Cell(row, 3), l.IznosLimita, breached ? breachedBg : bg);
            SetNum(ws.Cell(row, 4), l.Utilizacija, breached ? breachedBg : bg);
            SetNum(ws.Cell(row, 5), l.KorigovaniLimit ?? 0, bg);
            SetNum(ws.Cell(row, 6), l.RaspoloziviLimit, breached ? breachedBg : bg);
            SetNum(ws.Cell(row, 7), l.RegulatorniKapital, bg);
            SetNum(ws.Cell(row, 8), l.OsnovniKapital, bg);
            ws.Cell(row, 9).Value = l.CreatedBy;

            for (int c = 1; c <= 9; c++)
            {
                ws.Cell(row, c).Style.Font.FontName = "Amalia";
                ws.Cell(row, c).Style.Fill.BackgroundColor = c is >= 3 and <= 8
                    ? ws.Cell(row, c).Style.Fill.BackgroundColor
                    : (breached ? breachedBg : bg);
            }
        }

        var tableRange = ws.Range(3, 1, Math.Max(3, 3 + limits.Count), 9);
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        tableRange.Style.Border.InsideBorderColor = XLColor.FromArgb(0xDD, 0xDD, 0xDD);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.SetAutoFilter();
        ws.Columns().AdjustToContents();
        ws.Columns(1, 9).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
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
        ReportType = r.ReportType,
        ReportDate = r.ReportDate,
        TotalClients = r.TotalClients,
        ClientsWithBreachedLimit = r.ClientsWithBreachedLimit,
        TotalExposure = r.TotalExposure,
        CreatedBy = r.CreatedBy,
        CreatedAt = r.CreatedAt
    };
}
