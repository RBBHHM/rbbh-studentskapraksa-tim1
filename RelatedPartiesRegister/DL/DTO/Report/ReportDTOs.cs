// File: src/RBBH.ConnectedParties/DL/DTO/Report/ReportDTOs.cs

namespace RBBH.ConnectedParties.DL.DTO.Report;

/// <summary>DTO za prikaz jednog izvještaja u listi.</summary>
public class ReportDTO
{
    public Guid Id { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; }
    public int TotalClients { get; set; }
    public int ClientsWithBreachedLimit { get; set; }
    public decimal TotalExposure { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>DTO za paginiranu listu izvještaja.</summary>
public class ReportListDTO
{
    public List<ReportDTO> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>DTO za red u Excel exportu — podaci o pravnom licu i limitu.</summary>
public class ExcelExportRowDTO
{
    // Podaci o pravnom licu
    public string Name { get; set; } = string.Empty;
    public bool IsResident { get; set; }
    public string? TaxNumber { get; set; }
    public string? FbaId { get; set; }
    public string BasisOfConnection { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    // Podaci o limitu
    public decimal ExposureLimit { get; set; }
    public decimal CurrentExposure { get; set; }
    public string Currency { get; set; } = "BAM";
    public bool IsLimitBreached { get; set; }

    // Podaci o kapitalu
    public decimal RegulatoryCapital { get; set; }
    public decimal CoreCapital { get; set; }
}

/// <summary>DTO za globalni (jedinstveni) zapis kapitala — modul Kapital.</summary>
public class CapitalDTO
{
    public Guid Id { get; set; }
    public decimal RegulatoryCapital { get; set; }
    public decimal CoreCapital { get; set; }
    public decimal SupplementaryCapital { get; set; }
    public DateTime CapitalDate { get; set; }
}

/// <summary>Request za ažuriranje kapitala (Regulatorni/Osnovni/Dopunski + datum) — modul Kapital.</summary>
public class UpdateCapitalDTO
{
    public decimal? RegulatoryCapital { get; set; }
    public decimal? CoreCapital { get; set; }
    public decimal? SupplementaryCapital { get; set; }
    public DateTime? CapitalDate { get; set; }
}