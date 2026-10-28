using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RBI.DesignSystem.Components.Icons;

namespace RBI.DesignSystem.Components.Feedback;

/// <summary>Source inline note with a distinct semantic icon.</summary>
public sealed class RbiCallout : ComponentBase
{
    /// <summary>Semantic message tone.</summary>
    [Parameter] public RbiAlertTone Tone { get; set; } = RbiAlertTone.Information;
    /// <summary>Optional emphasized heading.</summary>
    [Parameter] public string? Title { get; set; }
    /// <summary>Composed message body.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional message classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional message attributes; the component owns role and classes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Tone)) throw new ArgumentOutOfRangeException(nameof(Tone));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var icon = Tone switch
        {
            RbiAlertTone.Success => RbiIconName.CheckCircle2,
            RbiAlertTone.Warning => RbiIconName.AlertTriangle,
            RbiAlertTone.Danger => RbiIconName.OctagonAlert,
            _ => RbiIconName.Info,
        };
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-callout rbi-callout--{Tone.ToString().ToLowerInvariant()} {Class}".TrimEnd());
        builder.AddAttribute(3, "role", Tone == RbiAlertTone.Danger ? "alert" : "note");
        builder.OpenComponent<RbiIcon>(4);
        builder.AddAttribute(5, nameof(RbiIcon.Name), icon);
        builder.AddAttribute(6, nameof(RbiIcon.Class), "rbi-callout__icon");
        builder.CloseComponent();
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "rbi-callout__content");
        if (!string.IsNullOrWhiteSpace(Title))
        {
            builder.OpenElement(9, "p");
            builder.AddAttribute(10, "class", "rbi-callout__title");
            builder.AddContent(11, Title);
            builder.CloseElement();
        }
        builder.OpenElement(12, "div");
        builder.AddAttribute(13, "class", "rbi-callout__body");
        builder.AddContent(14, ChildContent);
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }
}
