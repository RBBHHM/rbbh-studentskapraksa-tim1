using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RBI.DesignSystem.Components.Forms;

namespace RBI.DesignSystem.Components.Layout;

/// <summary>Scroll axes supported by a native scroll viewport.</summary>
public enum RbiScrollAxes
{
    /// <summary>Vertical content scrolling.</summary>
    Vertical = 0,
    /// <summary>Horizontal content scrolling.</summary>
    Horizontal = 1,
    /// <summary>Scrolling in both directions.</summary>
    Both = 2,
}

/// <summary>A named, keyboard-focusable native scroll viewport with source-sized scrollbar styling.</summary>
public sealed class RbiScrollArea : ComponentBase
{
    private IReadOnlyDictionary<string, object>? _safeAttributes;
    /// <summary>Localized accessible name of the scrollable region.</summary>
    [Parameter, EditorRequired] public string AccessibleLabel { get; set; } = string.Empty;
    /// <summary>Content inside the native viewport.</summary>
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }
    /// <summary>Enabled scroll directions.</summary>
    [Parameter] public RbiScrollAxes Axes { get; set; }
    /// <summary>Host classes defining the available size and decoration.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional viewport attributes, excluding owned keyboard/naming semantics.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(AccessibleLabel)) throw new ArgumentException("A scroll region name is required.", nameof(AccessibleLabel));
        if (ChildContent is null) throw new ArgumentException("Scroll content is required.", nameof(ChildContent));
        if (!Enum.IsDefined(Axes)) throw new ArgumentOutOfRangeException(nameof(Axes));
        _safeAttributes = RbiFormUtilities.SafeAttributes(AdditionalAttributes, "class", "role", "tabindex", "aria-label");
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", $"rbi-scroll-area {Class}".TrimEnd());
        builder.OpenElement(2, "div");
        builder.AddMultipleAttributes(3, _safeAttributes);
        builder.AddAttribute(4, "class", $"rbi-scroll-area__viewport rbi-scroll-area__viewport--{Axes.ToString().ToLowerInvariant()}");
        builder.AddAttribute(5, "role", "region");
        builder.AddAttribute(6, "tabindex", 0);
        builder.AddAttribute(7, "aria-label", AccessibleLabel);
        builder.AddContent(8, ChildContent);
        builder.CloseElement();
        builder.CloseElement();
    }
}
