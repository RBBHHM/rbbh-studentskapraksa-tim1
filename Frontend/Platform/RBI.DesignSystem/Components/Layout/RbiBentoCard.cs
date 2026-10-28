using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Layout;

/// <summary>Native source-backed BentoCard composition.</summary>
public sealed class RbiBentoCard : ComponentBase
{
    /// <summary>Desktop span out of twelve; full-width on mobile.</summary>
    [Parameter] public RbiBentoSpan Span { get; set; } = RbiBentoSpan.Four;
    /// <summary>Desktop row span.</summary>
    [Parameter] public RbiBentoRows Rows { get; set; } = RbiBentoRows.One;
    /// <summary>Semantic surface context.</summary>
    [Parameter] public RbiBentoTone Tone { get; set; } = RbiBentoTone.Default;
    /// <summary>Semantic wrapper.</summary>
    [Parameter] public RbiBentoElement Element { get; set; } = RbiBentoElement.Div;
    /// <summary>Includes the source yellow top rule.</summary>
    [Parameter] public bool Accent { get; set; } = false;
    /// <summary>Composed child content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional wrapper attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Span)) throw new ArgumentOutOfRangeException(nameof(Span));
        if (!Enum.IsDefined(Rows)) throw new ArgumentOutOfRangeException(nameof(Rows));
        if (!Enum.IsDefined(Tone)) throw new ArgumentOutOfRangeException(nameof(Tone));
        if (!Enum.IsDefined(Element)) throw new ArgumentOutOfRangeException(nameof(Element));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element == RbiBentoElement.Article ? "article" : Element == RbiBentoElement.Section ? "section" : "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-bento-card rbi-bento-card--{Tone.ToString().ToLowerInvariant()} {Class}".TrimEnd());
        builder.AddAttribute(3, "data-surface", Tone == RbiBentoTone.Default ? null : Tone.ToString().ToLowerInvariant());
        builder.AddAttribute(4, "style", $"--rbi-bento-span:{(int)Span};--rbi-bento-rows:{(int)Rows}");
        if (Accent)
        {
            builder.OpenElement(5, "span");
            builder.AddAttribute(6, "class", "rbi-bento-card__accent");
            builder.AddAttribute(7, "aria-hidden", "true");
            builder.CloseElement();
        }
        builder.AddContent(8, ChildContent);
        builder.CloseElement();
    }
}
