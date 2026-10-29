using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Icons;

/// <summary>Deterministic, currentColor SVG; decorative unless an accessible label is supplied.</summary>
public sealed class RbiIcon : ComponentBase
{
    /// <summary>Approved source geometry to render.</summary>
    [Parameter, EditorRequired] public RbiIconName Name { get; set; }
    /// <summary>Accessible image name; null means decorative. Name icon-only controls on the control.</summary>
    [Parameter] public string? AccessibleLabel { get; set; }
    /// <summary>Additional SVG classes, including consumer sizing.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional SVG attributes; accessibility and geometry are component-owned.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Name)) throw new ArgumentOutOfRangeException(nameof(Name));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var meaningful = !string.IsNullOrWhiteSpace(AccessibleLabel);
        builder.OpenElement(0, "svg");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-icon {Class}".TrimEnd());
        builder.AddAttribute(3, "viewBox", RbiIconGeometry.ViewBox(Name));
        builder.AddAttribute(4, "width", 24);
        builder.AddAttribute(5, "height", 24);
        builder.AddAttribute(6, "fill", RbiIconGeometry.IsFilled(Name) ? "currentColor" : "none");
        builder.AddAttribute(7, "stroke", RbiIconGeometry.IsFilled(Name) ? "none" : "currentColor");
        builder.AddAttribute(8, "stroke-width", 2);
        builder.AddAttribute(9, "stroke-linecap", "round");
        builder.AddAttribute(10, "stroke-linejoin", "round");
        builder.AddAttribute(11, "aria-hidden", meaningful ? null : "true");
        builder.AddAttribute(12, "role", meaningful ? "img" : null);
        builder.AddAttribute(13, "aria-label", meaningful ? AccessibleLabel : null);
        builder.AddAttribute(14, "focusable", "false");
        // Only compiled, trusted source geometry enters this markup path.
        builder.AddMarkupContent(15, RbiIconGeometry.Get(Name));
        builder.CloseElement();
    }
}
