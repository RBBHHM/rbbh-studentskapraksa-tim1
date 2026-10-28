using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

public sealed class LimitDraft
{
    public Guid? LegalEntityId { get; set; }
    public string? TipLimita { get; set; }
    public decimal? OdobreniLimit { get; set; }
    public decimal? MaksimalnaOcekivanaUtilizacija { get; set; }
    public DateTime? RokOcekivaneUtilizacije { get; set; }
    public string? Komentar { get; set; }
    public static LimitDraft From(LimitRecord record) => new()
    {
        LegalEntityId = record.LegalEntityId,
        TipLimita = record.TipLimita,
        OdobreniLimit = record.OdobreniLimit,
        MaksimalnaOcekivanaUtilizacija = record.MaksimalnaOcekivanaUtilizacija,
        RokOcekivaneUtilizacije = record.RokOcekivaneUtilizacije,
        Komentar = record.Komentar
    };
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (LegalEntityId is null || LegalEntityId == Guid.Empty) errors[nameof(LegalEntityId)] = ["validation.required"];
        if (string.IsNullOrWhiteSpace(TipLimita)) errors[nameof(TipLimita)] = ["validation.required"];
        if (OdobreniLimit < 0) errors[nameof(OdobreniLimit)] = ["limit.nonnegative"];
        if (MaksimalnaOcekivanaUtilizacija < 0) errors[nameof(MaksimalnaOcekivanaUtilizacija)] = ["limit.nonnegative"];
        return errors;
    }
    public LimitRequest ToRequest()
    {
        if (Validate().Count > 0) throw new InvalidOperationException("Resolve field validation before creating the request.");
        return new(LegalEntityId!.Value, TipLimita!, OdobreniLimit, MaksimalnaOcekivanaUtilizacija, RokOcekivaneUtilizacije, Komentar);
    }
}
