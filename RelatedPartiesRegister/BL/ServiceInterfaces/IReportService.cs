// File: src/RBBH.ConnectedParties/BL/ServiceInterfaces/IReportService.cs

using RBBH.ConnectedParties.DL.DTO.Report;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

/// <summary>
/// Servis za generisanje izvještaja i Excel export podataka o pravnim licima.
/// </summary>
public interface IReportService
{
    /// <summary>Kreira dnevni izvještaj za trenutni datum.</summary>
    Task<ReportDTO> GenerateDailyReportAsync(string createdBy);

    /// <summary>Kreira mjesečni izvještaj za zadani mjesec.</summary>
    Task<ReportDTO> GenerateMonthlyReportAsync(int year, int month, string createdBy);

    /// <summary>Vraća paginiranu listu dnevnih izvještaja.</summary>
    Task<ReportListDTO> GetDailyReportsAsync(int page, int pageSize);

    /// <summary>Vraća paginiranu listu mjesečnih izvještaja.</summary>
    Task<ReportListDTO> GetMonthlyReportsAsync(int page, int pageSize);

    /// <summary>
    /// Excel export jednog klijenta po poreznom broju ili FBA ID-u.
    /// Vraća byte[] sadržaj Excel fajla.
    /// </summary>
    Task<byte[]> ExportClientByIdAsync(string identifier);

    /// <summary>
    /// Excel export svih klijenata s limitima.
    /// Vraća byte[] sadržaj Excel fajla.
    /// </summary>
    Task<byte[]> ExportAllClientsWithLimitsAsync();
    Task<byte[]> ExportGeneratedReportAsync(Guid reportId);
}
