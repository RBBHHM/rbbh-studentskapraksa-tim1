using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RBI.DesignSystem.Components.Forms;

namespace RBI.DesignSystem.Components.Menu;

internal sealed record RbiMenuRadioContext<TValue>(Func<TValue> Value, Func<bool> Disabled, Func<TValue, Task> SelectAsync);

/// <summary>A named, strongly typed controlled exclusive-selection group inside an RBI menu.</summary>
/// <typeparam name="TValue">Consumer-owned selection value type.</typeparam>
public sealed class RbiMenuRadioGroup<TValue> : ComponentBase
{
    private readonly string _labelId = RbiFormUtilities.Id("menu-radio-label");
    private RbiMenuRadioContext<TValue>? _context;
    private IReadOnlyDictionary<string, object>? _safeAttributes;
    /// <summary>Localized group label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    /// <summary>Controlled selected value.</summary>
    [Parameter] public TValue Value { get; set; } = default!;
    /// <summary>Proposes an exclusive selection change.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }
    /// <summary>Whether every item in the group is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Radio menu items composing this group.</summary>
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional scoped CSS classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional group attributes excluding owned naming and role.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Label)) throw new ArgumentException("A group label is required.", nameof(Label));
        if (ChildContent is null) throw new ArgumentException("Radio menu items are required.", nameof(ChildContent));
        _context ??= new(() => Value, () => Disabled, SelectAsync);
        _safeAttributes = RbiFormUtilities.SafeAttributes(AdditionalAttributes, "class", "role", "aria-labelledby");
    }
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, _safeAttributes);
        builder.AddAttribute(2, "class", $"rbi-menu-group {Class}".TrimEnd());
        builder.AddAttribute(3, "role", "group");
        builder.AddAttribute(4, "aria-labelledby", _labelId);
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "id", _labelId);
        builder.AddAttribute(7, "class", "rbi-menu-group__label");
        builder.AddContent(8, Label);
        builder.CloseElement();
        builder.OpenComponent<CascadingValue<RbiMenuRadioContext<TValue>>>(9);
        builder.AddAttribute(10, "Value", _context);
        // Items must receive the group's current controlled value after parent updates.
        builder.AddAttribute(11, "IsFixed", false);
        builder.AddAttribute(12, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }
    private Task SelectAsync(TValue value) => Disabled || EqualityComparer<TValue>.Default.Equals(Value, value) ? Task.CompletedTask : ValueChanged.InvokeAsync(value);
}
