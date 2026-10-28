using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Forms;

/// <summary>A visible label associated with a native control; disabled/required presentation does not mutate that control.</summary>
public sealed class RbiLabel : ComponentBase
{
    /// <summary>The associated control's stable HTML id.</summary>
    [Parameter, EditorRequired] public string For { get; set; } = string.Empty;
    /// <summary>Disabled presentation; the associated control must also be disabled by its owner.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>A decorative required marker; the associated control owns required semantics.</summary>
    [Parameter] public bool Required { get; set; }
    /// <summary>Visible label content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional label classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional label attributes, excluding the owned for/class attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(For)) throw new ArgumentException("An associated control id is required.", nameof(For));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "label");
        builder.AddMultipleAttributes(1, RbiFormUtilities.SafeAttributes(AdditionalAttributes, "for", "class"));
        builder.AddAttribute(2, "class", string.Join(' ', new[] { "rbi-label", Disabled ? "rbi-label--disabled" : null, Class }.Where(value => !string.IsNullOrWhiteSpace(value))));
        builder.AddAttribute(3, "for", For);
        builder.AddContent(4, ChildContent);
        if (Required)
        {
            builder.OpenElement(5, "span");
            builder.AddAttribute(6, "class", "rbi-field__required");
            builder.AddAttribute(7, "aria-hidden", "true");
            builder.AddContent(8, "*");
            builder.CloseElement();
        }
        builder.CloseElement();
    }
}
