using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Brand;

/// <summary>
/// Three-part RBI claim; applications supply localized wording. Use editorial
/// display type of at least 28px so the lighter 0.86em middle slot remains large text.
/// </summary>
public sealed class RbiSlogan : ComponentBase
{
    /// <summary>First, prominent word or phrase.</summary>
    [Parameter, EditorRequired] public string Lead { get; set; } = string.Empty;
    /// <summary>Middle, recessive word or phrase.</summary>
    [Parameter, EditorRequired] public string Accent { get; set; } = string.Empty;
    /// <summary>Last, prominent word or phrase.</summary>
    [Parameter, EditorRequired] public string Tail { get; set; } = string.Empty;
    /// <summary>Semantic wrapper; unrelated to visual size.</summary>
    [Parameter] public RbiSloganElement Element { get; set; }
    /// <summary>Includes the print lockup's trailing full stop.</summary>
    [Parameter] public bool WithStop { get; set; }
    /// <summary>Additional wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional wrapper attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Lead);
        ArgumentException.ThrowIfNullOrWhiteSpace(Accent);
        ArgumentException.ThrowIfNullOrWhiteSpace(Tail);
        if (!Enum.IsDefined(Element)) throw new ArgumentOutOfRangeException(nameof(Element));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var element = Element switch
        {
            RbiSloganElement.Paragraph => "p",
            RbiSloganElement.Div => "div",
            RbiSloganElement.Heading1 => "h1",
            RbiSloganElement.Heading2 => "h2",
            _ => "span",
        };
        builder.OpenElement(0, element);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-slogan {Class}".TrimEnd());
        builder.OpenElement(3, "span");
        builder.AddContent(4, Lead);
        builder.CloseElement();
        builder.AddContent(5, " ");
        builder.OpenElement(6, "span");
        builder.AddAttribute(7, "class", "rbi-slogan__accent");
        builder.AddContent(8, Accent);
        builder.CloseElement();
        builder.AddContent(9, " ");
        builder.OpenElement(10, "span");
        builder.AddContent(11, Tail);
        builder.CloseElement();
        if (WithStop) builder.AddContent(12, ".");
        builder.CloseElement();
    }
}
