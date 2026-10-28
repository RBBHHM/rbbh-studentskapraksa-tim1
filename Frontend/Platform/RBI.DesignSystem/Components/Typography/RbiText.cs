using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Typography;

public sealed class RbiText : ComponentBase
{
    [Parameter] public RbiTextElement Element { get; set; } = RbiTextElement.Paragraph;
    [Parameter] public RbiTextSize Size { get; set; } = RbiTextSize.Medium;
    [Parameter] public RbiTextTone Tone { get; set; } = RbiTextTone.Inherit;
    [Parameter] public RbiTextWeight Weight { get; set; } = RbiTextWeight.Inherit;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, ElementName);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass);
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }

    private string ElementName => Element switch
    {
        RbiTextElement.Span => "span",
        RbiTextElement.Div => "div",
        _ => "p",
    };

    private string CssClass => string.Join(' ', new[]
    {
        "rbi-text",
        $"rbi-text--{Size.ToString().ToLowerInvariant()}",
        Tone == RbiTextTone.Inherit ? null : $"rbi-text--tone-{Tone.ToString().ToLowerInvariant()}",
        Weight == RbiTextWeight.Inherit ? null : $"rbi-text--weight-{Weight.ToString().ToLowerInvariant()}",
        Class,
    }.Where(value => !string.IsNullOrWhiteSpace(value)));
}
