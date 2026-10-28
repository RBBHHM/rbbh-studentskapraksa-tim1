using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RBI.DesignSystem.Components.Forms;

namespace RBI.DesignSystem.Components.Menu;

/// <summary>Controlled checkable menu command; a null checked value represents mixed state.</summary>
public sealed class RbiMenuCheckboxItem : ComponentBase
{
    [CascadingParameter] private RbiMenuContext? Context { get; set; }
    private IReadOnlyDictionary<string, object>? _safeAttributes;
    /// <summary>Localized visible and accessible command label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;
    /// <summary>Controlled selection; null means mixed.</summary>
    [Parameter] public bool? Checked { get; set; } = false;
    /// <summary>Proposes a selection change without mutating the supplied checked state.</summary>
    [Parameter] public EventCallback<bool?> CheckedChanged { get; set; }
    /// <summary>Whether selection is unavailable; remains in the accessibility tree.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Whether activation closes the owning menu and restores trigger focus.</summary>
    [Parameter] public bool CloseOnSelect { get; set; } = true;
    /// <summary>Additional scoped CSS classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional attributes, excluding component-owned semantics and activation.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Context is null) throw new InvalidOperationException("Checkable items require an RBI menu.");
        if (string.IsNullOrWhiteSpace(Label)) throw new ArgumentException("A menu item label is required.", nameof(Label));
        _safeAttributes = RbiFormUtilities.SafeAttributes(AdditionalAttributes, "class", "role", "type", "tabindex", "aria-disabled", "aria-checked", "data-rbi-menu-item", "data-rbi-menu-text", "onclick");
    }
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        RbiMenuSelectionRendering.Render(builder, _safeAttributes, Label, Class, Disabled, Checked, false, EventCallback.Factory.Create(this, ActivateAsync));
    private async Task ActivateAsync()
    {
        if (Disabled) return;
        if (CloseOnSelect) await Context!.CloseAsync(true);
        await CheckedChanged.InvokeAsync(Checked != true);
    }
}
