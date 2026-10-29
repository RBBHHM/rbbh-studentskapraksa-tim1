using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Typography;

/// <summary>Amalia Black editorial display, retaining semantic heading independence.</summary>
public sealed class RbiDisplay : ComponentBase
{
    /// <summary>Responsive visual size.</summary>
    [Parameter] public RbiDisplaySize Size { get; set; } = RbiDisplaySize.Large;
    /// <summary>Semantic wrapper; does not select type size.</summary>
    [Parameter] public RbiDisplayElement Element { get; set; }
    /// <summary>Composed statement.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional wrapper attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (!Enum.IsDefined(Element)) throw new ArgumentOutOfRangeException(nameof(Element));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var element = Element switch
        {
            RbiDisplayElement.Heading2 => "h2",
            RbiDisplayElement.Heading3 => "h3",
            RbiDisplayElement.Paragraph => "p",
            RbiDisplayElement.Span => "span",
            RbiDisplayElement.Div => "div",
            _ => "h1",
        };
        builder.OpenElement(0, element);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-display rbi-display--{Size.ToString().ToLowerInvariant()} {Class}".TrimEnd());
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
