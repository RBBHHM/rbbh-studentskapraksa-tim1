namespace RBI.DesignSystem.Components.Navigation;

internal sealed class RbiTabRegistration<TValue>
{
    internal required TValue Value { get; init; }
    internal required string TabId { get; init; }
    internal required string PanelId { get; init; }
    internal bool Disabled { get; set; }
    internal bool Focused { get; set; }
    internal Func<ValueTask>? FocusAsync { get; set; }
}

internal sealed record RbiTabsContext<TValue>(
    Func<TValue, bool, RbiTabRegistration<TValue>> Register,
    Func<TValue, RbiTabRegistration<TValue>> Resolve,
    Func<TValue, bool> IsSelected,
    Func<TValue, bool> IsFocusable,
    Func<TValue, Task> SelectAsync,
    Func<TValue, string, Task> HandleKeyAsync);
