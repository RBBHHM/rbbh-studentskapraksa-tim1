using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Typography;

/// <summary>Long-form source-backed documentation typography.</summary>
public sealed class RbiProse : ComponentBase
{
    /// <summary>Composed text or documentation.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional wrapper attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-prose {Class}".TrimEnd());
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
