namespace RBBH.ConnectedParties.Helpers.Constants;

/// <summary>
/// Jedini poslovni pristupi koje aplikacija dodjeljuje korisnicima.
/// Tehničke Keycloak uloge se ne prikazuju niti mijenjaju kroz ovu aplikaciju.
/// </summary>
public static class ApplicationAccessRoles
{
    public const string PhysicalPersons = "physical-persons";
    public const string LegalPersons = "legal-persons";
    public const string Limits = "limits";
    public const string RegulatoryReporting = "regulatory-reporting";

    public static readonly string[] All =
    [
        PhysicalPersons,
        LegalPersons,
        Limits,
        RegulatoryReporting
    ];
}
