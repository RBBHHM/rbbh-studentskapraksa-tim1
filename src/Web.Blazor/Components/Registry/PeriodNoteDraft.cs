namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

public enum PeriodNoteKind { RequestUnlock, Reject, RequestInfo, Response }
public sealed class PeriodNoteDraft
{
    public PeriodNoteKind Kind { get; init; }
    public string Text { get; set; } = "";
    public int Maximum => Kind == PeriodNoteKind.Response ? 490 : 500;
    public string? ErrorKey => Text.Trim().Length < 10 || Text.Length > Maximum
        ? Kind == PeriodNoteKind.Response ? "period.response-range" : "period.note-range" : null;
}
