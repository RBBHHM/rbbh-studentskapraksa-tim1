namespace RBI.DesignSystem.Components.Forms;

internal sealed record RbiRadioGroupContext<TValue>(
    string Name,
    TValue? Value,
    bool Disabled,
    bool Required,
    bool Invalid,
    string? DescribedBy,
    Func<TValue, Task> SelectAsync);
