using RBBH.ConnectedParties.DL.DTO.Report;
using RBBH.ConnectedParties.Exceptions.Validations;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

/// <summary>
/// Modul Kapital — globalni kapital banke po datumu važenja, s nepromjenjivim revizijama.
/// </summary>
public interface ICapitalService
{
    /// <summary>Vraća poznati kapital koji važi danas, ili null ako nije poznat.</summary>
    Task<Result<CapitalDTO?>> Get();
    Task<Result<CapitalDTO?>> AsOf(DateTime date, CancellationToken ct);
    Task<BL.Services.ReferencePage<CapitalHistoryDTO>> History(int page, int pageSize, CancellationToken ct);

    /// <summary>Dodaje novu nepromjenjivu reviziju kapitala.</summary>
    Task<Result<CapitalDTO>> Upsert(UpdateCapitalDTO dto, string korisnik);
}
