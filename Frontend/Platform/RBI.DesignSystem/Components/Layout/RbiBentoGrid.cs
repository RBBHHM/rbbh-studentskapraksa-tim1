using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Layout;

/// <summary>Native source-backed BentoGrid composition.</summary>
public sealed class RbiBentoGrid : ComponentBase
{
    /// <summary>Bento gap, distinct from ordinary grid gaps.</summary>
    [Parameter] public RbiGridGap Gap { get; set; } = RbiGridGap.Default;
    /// <summary>Desktop minimum row height.</summary>
    [Parameter] public RbiBentoDensity Density { get; set; } = RbiBentoDensity.Uniform;
    /// <summary>Composed child content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional wrapper attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Gap)) throw new ArgumentOutOfRangeException(nameof(Gap));
        if (!Enum.IsDefined(Density)) throw new ArgumentOutOfRangeException(nameof(Density));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-bento-grid rbi-bento-grid--gap-{Gap.ToString().ToLowerInvariant()} rbi-bento-grid--{Density.ToString().ToLowerInvariant()} {Class}".TrimEnd());
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
