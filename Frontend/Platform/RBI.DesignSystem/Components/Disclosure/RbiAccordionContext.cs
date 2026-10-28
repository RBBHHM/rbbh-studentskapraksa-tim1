namespace RBI.DesignSystem.Components.Disclosure;

internal sealed record RbiAccordionContext(
    Func<string, bool> IsExpanded,
    Func<string, bool, Task> ToggleAsync,
    Action<string, bool, Func<ValueTask>> Register,
    Func<string, string, Task> HandleKeyAsync);
