using RBBH.ConnectedParties.DL.DTO.Report;
using RBBH.ConnectedParties.Exceptions.Validations;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

/// <summary>
/// Modul Kapital — jedan globalni (nije po pravnom licu) zapis regulatornog/osnovnog/dopunskog kapitala.
/// </summary>
public interface ICapitalService
{
    /// <summary>Vraća globalni zapis kapitala, ili null ako još nije postavljen.</summary>
    Task<Result<CapitalDTO?>> Get();

    /// <summary>Kreira ili ažurira globalni zapis kapitala.</summary>
    Task<Result<CapitalDTO>> Upsert(UpdateCapitalDTO dto, string korisnik);
}
