using System.Globalization;
using System.Text.RegularExpressions;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

// UX model only. The separate backend retains authoritative validation,
// uniqueness, relationship rules, persistence and verification.
public sealed class PhysicalPersonDraft
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? Residency { get; set; }
    public string? Jmbg { get; set; }
    public string? PassportNumber { get; set; }
    public string? FbaId { get; set; }
    public string? RelationBasis { get; set; }
    public string? RelationDescription { get; set; }
    public string? SpecialRelationBasis { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? IsIdentifiedStaff { get; set; }
    public bool? DeclarationNoFamilyMembers { get; set; }
    public bool? ConnectedWithBank { get; set; }
    public bool? SpecialRelationshipWithBank { get; set; }
    public bool? SpecialContract { get; set; }
    public bool? MalusClawback { get; set; }
    public Guid? RelatedToPersonId { get; set; }
    public bool IsFamily => string.Equals(SpecialRelationBasis, "UZA_PORODICA", StringComparison.OrdinalIgnoreCase)
        || string.Equals(SpecialRelationBasis, "Član uže porodice povezanog lica", StringComparison.OrdinalIgnoreCase);

    public static PhysicalPersonDraft From(RelatedPerson person) => new()
    {
        FirstName = person.FirstName, LastName = person.LastName, Residency = person.Residency,
        Jmbg = person.Jmbg, PassportNumber = person.PassportNumber, FbaId = person.FbaId,
        RelationBasis = person.RelationBasis, RelationDescription = person.RelationDescription,
        SpecialRelationBasis = person.SpecialRelationBasis, DateFrom = person.DateFrom,
        DateTo = person.DateTo, IsIdentifiedStaff = person.IsIdentifiedStaff,
        DeclarationNoFamilyMembers = person.DeclarationNoFamilyMembers, ConnectedWithBank = person.ConnectedWithBank,
        SpecialRelationshipWithBank = person.SpecialRelationshipWithBank, SpecialContract = person.SpecialContract,
        MalusClawback = person.MalusClawback, RelatedToPersonId = person.RelatedToPersonId
    };

    public void ResidencyChanged()
    {
        if (Residency == 1) { PassportNumber = null; FbaId = null; }
        else if (Residency == 2) Jmbg = null;
    }

    public void SpecialBasisChanged()
    {
        if (IsFamily)
        {
            IsIdentifiedStaff = false; ConnectedWithBank = true; SpecialRelationshipWithBank = false;
            SpecialContract = false; MalusClawback = false; DeclarationNoFamilyMembers = true;
        }
        else
        {
            RelatedToPersonId = null;
            IsIdentifiedStaff = ConnectedWithBank = SpecialRelationshipWithBank = SpecialContract = MalusClawback = DeclarationNoFamilyMembers = null;
        }
    }

    // Values are localization keys, mapped to application-owned copy by the form.
    public Dictionary<string, string[]> Validate(bool identityOnly = false)
    {
        var errors = new Dictionary<string, string[]>();
        void Error(string field, string key) => errors.TryAdd(field, [key]);
        if (!Regex.IsMatch(FirstName?.Trim() ?? "", @"^[\p{L}][\p{L} '\-]{1,99}$")) Error(nameof(FirstName), "validation.name");
        if (!Regex.IsMatch(LastName?.Trim() ?? "", @"^[\p{L}][\p{L} '\-]{1,99}$")) Error(nameof(LastName), "validation.name");
        if (Residency is not (1 or 2)) Error(nameof(Residency), "validation.required");
        if (Residency == 1 && !ValidJmbg(Jmbg)) Error(nameof(Jmbg), "validation.jmbg");
        if (Residency == 2)
        {
            if (!Regex.IsMatch(PassportNumber?.Trim() ?? "", "^[A-Za-z0-9][A-Za-z0-9-]{4,19}$")) Error(nameof(PassportNumber), "validation.passport");
            if (!Regex.IsMatch(FbaId?.Trim() ?? "", "^[0-9]{1,10}$")) Error(nameof(FbaId), "validation.fba");
        }
        if (identityOnly) return errors;
        if (string.IsNullOrWhiteSpace(RelationBasis)) Error(nameof(RelationBasis), "validation.required");
        if (string.IsNullOrWhiteSpace(RelationDescription)) Error(nameof(RelationDescription), "validation.required");
        if (string.IsNullOrWhiteSpace(SpecialRelationBasis)) Error(nameof(SpecialRelationBasis), "validation.required");
        foreach (var (name, value) in new (string, bool?)[]
        {
            (nameof(IsIdentifiedStaff), IsIdentifiedStaff), (nameof(DeclarationNoFamilyMembers), DeclarationNoFamilyMembers),
            (nameof(ConnectedWithBank), ConnectedWithBank), (nameof(SpecialRelationshipWithBank), SpecialRelationshipWithBank),
            (nameof(SpecialContract), SpecialContract), (nameof(MalusClawback), MalusClawback)
        }) if (value is null) Error(name, "validation.required");
        if (!IsFamily && IsIdentifiedStaff == false) Error(nameof(IsIdentifiedStaff), "validation.staff");
        if (IsFamily && RelatedToPersonId is null) Error(nameof(RelatedToPersonId), "validation.required");
        if (DateFrom is null) Error(nameof(DateFrom), "validation.date");
        if (DateTo is null) Error(nameof(DateTo), "validation.date");
        if (DateFrom.HasValue && DateTo.HasValue && DateTo < DateFrom) Error(nameof(DateTo), "validation.date-order");
        return errors;
    }

    public PersonRequest ToRequest()
    {
        if (Validate().Count > 0) throw new InvalidOperationException("Resolve field validation before creating the request.");
        return new(FirstName!.Trim(), LastName!.Trim(), Residency!.Value, Clean(Jmbg), Clean(PassportNumber), Clean(FbaId),
            RelationBasis!, RelationDescription!, SpecialRelationBasis!, DateFrom, DateTo, IsIdentifiedStaff!.Value,
            DeclarationNoFamilyMembers!.Value, ConnectedWithBank!.Value, SpecialRelationshipWithBank!.Value,
            SpecialContract!.Value, MalusClawback!.Value, RelatedToPersonId);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public static bool ValidJmbg(string? value)
    {
        if (value is null || !Regex.IsMatch(value, "^[0-9]{13}$")) return false;
        var encoded = int.Parse(value[4..7], CultureInfo.InvariantCulture);
        var year = encoded > 900 ? encoded + 1000 : encoded + 2000;
        if (!DateTime.TryParseExact($"{value[..2]}.{value[2..4]}.{year}", "dd.MM.yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) || date > DateTime.Today) return false;
        int[] weights = [7, 6, 5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        var sum = Enumerable.Range(0, 12).Sum(index => (value[index] - '0') * weights[index]);
        var digit = sum % 11 is 0 or 1 ? 0 : 11 - sum % 11;
        return digit == value[12] - '0';
    }
}
