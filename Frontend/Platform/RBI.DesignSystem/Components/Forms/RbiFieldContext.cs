namespace RBI.DesignSystem.Components.Forms;

internal sealed record RbiFieldContext(
    string ControlId,
    string? DescribedBy,
    bool Disabled,
    bool Required,
    bool Invalid);
