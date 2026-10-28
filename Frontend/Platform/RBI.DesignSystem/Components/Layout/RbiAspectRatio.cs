using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Layout;

/// <summary>Native source-backed AspectRatio composition.</summary>
public sealed class RbiAspectRatio : ComponentBase
{
    /// <summary>Positive finite width-to-height ratio.</summary>
    [Parameter] public double Ratio { get; set; } = 1d;
    /// <summary>Composed child content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional wrapper attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!double.IsFinite(Ratio) || Ratio <= 0) throw new ArgumentOutOfRangeException(nameof(Ratio));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-aspect-ratio {Class}".TrimEnd());
        builder.AddAttribute(3, "style", $"--rbi-aspect-ratio:{Ratio.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        builder.OpenElement(4, "div");
        builder.AddAttribute(5, "class", "rbi-aspect-ratio__content");
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
        builder.CloseElement();
    }
}
