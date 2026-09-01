namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public interface IEmailService
{
    /// <summary>Notifikacija adminu: korisnik podnio zahtjev za otključavanje.</summary>
    Task SendUnlockRequestAsync(
        string adminEmail, string requestedBy, int year, int month, string reason);

    /// <summary>Notifikacija korisniku: admin tražio više informacija.</summary>
    Task SendNeedsInfoNotificationAsync(
        string userEmail, string requestedBy, int year, int month, string adminNote, Guid requestId);

    /// <summary>Notifikacija adminu: korisnik odgovorio na zahtjev za informacijama.</summary>
    Task SendUserResponseAsync(
        string adminEmail, string requestedBy, int year, int month, string userMessage);

    /// <summary>Notifikacija korisniku: period otključan.</summary>
    Task SendUnlockConfirmationAsync(
        string userEmail, string requestedBy, int year, int month);

    /// <summary>Notifikacija HR-u: novo fizičko lice dodano u registar.</summary>
    Task SendHrNewPhysicalPersonAsync(
        string hrEmail, string personName, string createdBy,
        string relationBasis, DateTime? dateFrom, DateTime? dateTo);

    /// <summary>Notifikacija HR-u: osnov povezanosti fizičkog lica je istekao ili promijenjen na prošli datum.</summary>
    Task SendHrPhysicalPersonExpiredAsync(
        string hrEmail, string personName, string updatedBy,
        string relationBasis, DateTime dateTo);
}
