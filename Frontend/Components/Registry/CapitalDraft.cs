using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

public sealed class CapitalDraft
{
    public decimal? RegulatoryCapital { get; set; }
    public decimal? CoreCapital { get; set; }
    public decimal? SupplementaryCapital { get; set; }
    public DateTime? ValidTill { get; set; }
    public long? ExpectedId { get; init; }
    public DateTime? ExpectedModifiedAt { get; init; }
    public static CapitalDraft From(CapitalRecord? record) => record is null ? new() : new()
    {
        RegulatoryCapital = record.RegulatoryCapital,
        CoreCapital = record.CoreCapital,
        SupplementaryCapital = record.SupplementaryCapital,
        ValidTill = record.ValidTill,
        ExpectedId = record.Id,
        ExpectedModifiedAt = record.ModifiedAt
    };
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var (field, value) in new (string, decimal?)[] { (nameof(RegulatoryCapital), RegulatoryCapital), (nameof(CoreCapital), CoreCapital), (nameof(SupplementaryCapital), SupplementaryCapital) })
        {
            if (value is null) errors[field] = ["validation.required"];
            else if (value < 0) errors[field] = ["limit.nonnegative"];
        }
        if (ValidTill is null) errors[nameof(ValidTill)] = ["validation.date"];
        return errors;
    }
    public CapitalRequest ToRequest()
    {
        if (Validate().Count > 0) throw new InvalidOperationException("Resolve field validation before creating the request.");
        return new(RegulatoryCapital!.Value, CoreCapital!.Value, SupplementaryCapital!.Value, ValidTill!.Value, ExpectedId, ExpectedModifiedAt);
    }
}
