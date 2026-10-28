namespace RBI.DesignSystem.Components.Selection;

internal sealed record RbiToggleGroupContext<TValue>(TValue? Value, bool Disabled, Func<TValue, Task> SelectAsync);
