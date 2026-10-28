using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Navigation;

/// <summary>Content-sized, horizontally scrollable pill navigation.</summary>
public sealed class RbiDock : ComponentBase
{
    /// <summary>Localized accessible navigation name.</summary>
    [Parameter, EditorRequired] public string AccessibleLabel { get; set; } = string.Empty;
    /// <summary>Composed RbiDockItem children.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional outer positioning classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional navigation attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() => ArgumentException.ThrowIfNullOrWhiteSpace(AccessibleLabel);

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", $"rbi-dock-position {Class}".TrimEnd());
        builder.OpenElement(2, "nav");
        builder.AddMultipleAttributes(3, AdditionalAttributes);
        builder.AddAttribute(4, "class", "rbi-dock");
        builder.AddAttribute(5, "aria-label", AccessibleLabel);
        builder.OpenElement(6, "ul");
        builder.AddContent(7, ChildContent);
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }
}
