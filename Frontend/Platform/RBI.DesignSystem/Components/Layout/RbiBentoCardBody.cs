using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Layout;

/// <summary>Native source-backed BentoCardBody composition.</summary>
public sealed class RbiBentoCardBody : ComponentBase
{
    /// <summary>Application-owned title.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    /// <summary>Optional editorial eyebrow.</summary>
    [Parameter] public string? Eyebrow { get; set; }
    /// <summary>Optional supporting text.</summary>
    [Parameter] public string? Body { get; set; }
    /// <summary>Semantic title level, two through four.</summary>
    [Parameter] public int HeadingLevel { get; set; } = 3;
    /// <summary>Optional actions or supporting content.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }
    /// <summary>Additional wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional wrapper attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
        if (HeadingLevel is < 2 or > 4) throw new ArgumentOutOfRangeException(nameof(HeadingLevel));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-bento-body {Class}".TrimEnd());
        if (!string.IsNullOrWhiteSpace(Eyebrow))
        {
            builder.OpenElement(3, "p");
            builder.AddAttribute(4, "class", "rbi-bento-body__eyebrow");
            builder.AddContent(5, Eyebrow);
            builder.CloseElement();
        }
        builder.OpenElement(6, $"h{HeadingLevel}");
        builder.AddAttribute(7, "class", "rbi-bento-body__title");
        builder.AddContent(8, Title);
        builder.CloseElement();
        if (!string.IsNullOrWhiteSpace(Body))
        {
            builder.OpenElement(9, "p");
            builder.AddAttribute(10, "class", "rbi-bento-body__text");
            builder.AddContent(11, Body);
            builder.CloseElement();
        }
        if (Footer is not null)
        {
            builder.OpenElement(12, "div");
            builder.AddAttribute(13, "class", "rbi-bento-body__footer");
            builder.AddContent(14, Footer);
            builder.CloseElement();
        }
        builder.CloseElement();
    }
}
