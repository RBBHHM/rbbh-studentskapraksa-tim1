namespace RBBH.ConnectedParties.Helpers.Constants;

/// <summary>
/// Jedini poslovni pristupi koje aplikacija dodjeljuje korisnicima.
/// Tehničke Keycloak uloge se ne prikazuju niti mijenjaju kroz ovu aplikaciju.
/// </summary>
public static class ApplicationAccessRoles
{
    public const string PhysicalPersons = "FL";
    public const string LegalPersons = "PL";
    public const string Capital = "kapital";
    public const string Limits = "limiti";
    public const string Administrator = "admin";
    public const string Guest = "gost";

    public static readonly string[] All =
    [
        PhysicalPersons,
        LegalPersons,
        Capital,
        Limits,
        Administrator,
        Guest
    ];

    public static readonly string[] BusinessModules =
    [PhysicalPersons, LegalPersons, Capital, Limits];

    public static readonly string[] Assignable =
    [Administrator, Capital, LegalPersons, PhysicalPersons, Limits];
}
