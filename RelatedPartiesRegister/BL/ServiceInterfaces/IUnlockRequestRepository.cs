using RBBH.ConnectedParties.DL.Entities.PeriodLock;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public interface IUnlockRequestRepository
{
    Task<UnlockRequest> CreateAsync(UnlockRequest request);

    /// <summary>Lista zahtjeva s opcionim filterom po statusu i paginacijom.</summary>
    Task<(List<UnlockRequest> Items, int Total)> GetPagedAsync(
        string? status, int page, int pageSize);

    /// <summary>Vraća jedan zahtjev po ID-u, ili null ako ne postoji.</summary>
    Task<UnlockRequest?> GetByIdAsync(Guid id);

    /// <summary>
    /// Postavlja sve PENDING zahtjeve za dati period na APPROVED.
    /// Poziva se pri otključavanju perioda da KPI ostanu konzistentni.
    /// </summary>
    Task ApproveAllPendingAsync(int year, int month, string approvedBy);

    /// <summary>Odbija PENDING zahtjev s obaveznim razlogom.</summary>
    Task<bool> RejectAsync(Guid id, string adminNote, string processedBy);

    /// <summary>Označava zahtjev kao NEEDS_INFO i čuva poruku admina.</summary>
    Task<bool> RequestMoreInfoAsync(Guid id, string adminNote, string processedBy);
}
