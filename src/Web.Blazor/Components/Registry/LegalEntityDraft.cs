using System.Text.RegularExpressions;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

// Application form UX only; backend validation and persistence remain authoritative.
public sealed class LegalEntityDraft
{
    public string? Name { get; set; }
    public bool? IsResident { get; set; }
    public string? TaxNumber { get; set; }
    public string? MaticniBroj { get; set; }
    public string? FbaId { get; set; }
    public string? BasisOfConnection { get; set; }
    public string? ConnectionDescription { get; set; }
    public bool? ConnectedWithBank { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    // This is NOT MaticniBroj. It is not an editable field in the source UI.
    // Retain the latest server value on update rather than clearing it silently.
    public string? PreservedMatbroj { get; private set; }
    public static LegalEntityDraft From(LegalEntity entity) => new()
    {
        Name = entity.Name,
        IsResident = entity.IsResident,
        TaxNumber = entity.TaxNumber,
        MaticniBroj = entity.MaticniBroj,
        FbaId = entity.FbaId,
        BasisOfConnection = entity.BasisOfConnection,
        ConnectionDescription = entity.ConnectionDescription,
        ConnectedWithBank = entity.ConnectedWithBank,
        DateFrom = entity.DateFrom,
        DateTo = entity.DateTo,
        PreservedMatbroj = entity.Matbroj
    };
    public void ResidencyChanged()
    {
        if (IsResident == true) FbaId = null;
        else if (IsResident == false) TaxNumber = null;
    }
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        void Error(string field, string key) => errors.TryAdd(field, [key]);
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length > 100) Error(nameof(Name), "legal.validation.name");
        if (IsResident is null) Error(nameof(IsResident), "validation.required");
        if (IsResident == true && !ValidTaxNumber(TaxNumber?.Trim())) Error(nameof(TaxNumber), "legal.validation.tax");
        if (IsResident == false && !Regex.IsMatch(FbaId?.Trim() ?? "", "^[0-9]{1,10}$")) Error(nameof(FbaId), "validation.fba");
        if (!string.IsNullOrEmpty(MaticniBroj) && !Regex.IsMatch(MaticniBroj.Trim(), "^[0-9]{0,50}$")) Error(nameof(MaticniBroj), "legal.validation.registration");
        if (string.IsNullOrWhiteSpace(BasisOfConnection) || BasisOfConnection.Length > 100) Error(nameof(BasisOfConnection), "validation.required");
        if (string.IsNullOrWhiteSpace(ConnectionDescription) || ConnectionDescription.Length > 500) Error(nameof(ConnectionDescription), "validation.required");
        if (ConnectedWithBank is null) Error(nameof(ConnectedWithBank), "validation.required");
        if (DateFrom is null) Error(nameof(DateFrom), "validation.date");
        if (DateFrom.HasValue && DateTo.HasValue && DateTo < DateFrom) Error(nameof(DateTo), "validation.date-order");
        return errors;
    }
    public LegalEntityRequest ToRequest()
    {
        if (Validate().Count > 0) throw new InvalidOperationException("Resolve field validation before creating the request.");
        static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return new(IsResident!.Value, IsResident == true ? Clean(TaxNumber) : null, Clean(MaticniBroj),
            IsResident == false ? Clean(FbaId) : null, Name!.Trim(), PreservedMatbroj, BasisOfConnection!,
            ConnectionDescription, ConnectedWithBank, DateFrom, DateTo);
    }
    public static bool ValidTaxNumber(string? value)
    {
        if (value is null || !Regex.IsMatch(value, "^[0-9]{13}$")) return false;
        int[] weights = [7, 6, 5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        var check = 11 - Enumerable.Range(0, 12).Sum(index => (value[index] - '0') * weights[index]) % 11;
        return (check >= 10 ? 0 : check) == value[12] - '0';
    }
}
