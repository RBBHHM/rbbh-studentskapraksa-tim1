using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RBI.DesignSystem.Components.Forms;

namespace RBI.DesignSystem.Components.Menu;

/// <summary>A typed radio menu command composed inside a matching radio group.</summary>
/// <typeparam name="TValue">The enclosing group's selection value type.</typeparam>
public sealed class RbiMenuRadioItem<TValue> : ComponentBase
{
    [CascadingParameter] private RbiMenuContext? Menu { get; set; }
    [CascadingParameter] private RbiMenuRadioContext<TValue>? Group { get; set; }
    private IReadOnlyDictionary<string, object>? _safeAttributes;
    /// <summary>Localized visible and accessible command label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    /// <summary>The typed value selected by this item.</summary>
    [Parameter, EditorRequired] public TValue Value { get; set; } = default!;
    /// <summary>Whether this item cannot be selected.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Whether selection closes the owning menu and restores trigger focus.</summary>
    [Parameter] public bool CloseOnSelect { get; set; } = true;
    /// <summary>Additional scoped CSS classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional attributes excluding owned command semantics.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Menu is null || Group is null) throw new InvalidOperationException("Radio items require an RBI menu and matching typed radio group.");
        if (string.IsNullOrWhiteSpace(Label)) throw new ArgumentException("A menu item label is required.", nameof(Label));
        _safeAttributes = RbiFormUtilities.SafeAttributes(AdditionalAttributes, "class", "role", "type", "tabindex", "aria-disabled", "aria-checked", "data-rbi-menu-item", "data-rbi-menu-text", "onclick");
    }
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var group = Group!;
        RbiMenuSelectionRendering.Render(builder, _safeAttributes, Label, Class, Disabled || group.Disabled(), EqualityComparer<TValue>.Default.Equals(Value, group.Value()), true, EventCallback.Factory.Create(this, ActivateAsync));
    }
    private async Task ActivateAsync()
    {
        if (Disabled || Group!.Disabled()) return;
        if (CloseOnSelect) await Menu!.CloseAsync(true);
        await Group.SelectAsync(Value);
    }
}
