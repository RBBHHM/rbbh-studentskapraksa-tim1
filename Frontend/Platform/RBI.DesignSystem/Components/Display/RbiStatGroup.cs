using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Display;

/// <summary>Native source-backed StatGroup composition.</summary>
public sealed class RbiStatGroup : ComponentBase
{
    /// <summary>Optional accessible band name.</summary>
    [Parameter] public string? AccessibleLabel { get; set; }
    /// <summary>Two, three or four desktop columns.</summary>
    [Parameter] public RBI.DesignSystem.Components.Layout.RbiGridColumns Columns { get; set; } = RBI.DesignSystem.Components.Layout.RbiGridColumns.Four;
    /// <summary>Source statistics gap.</summary>
    [Parameter] public RBI.DesignSystem.Components.Layout.RbiGridGap Gap { get; set; } = RBI.DesignSystem.Components.Layout.RbiGridGap.Default;
    /// <summary>Composed child content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional wrapper attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Columns is not (RBI.DesignSystem.Components.Layout.RbiGridColumns.Two or RBI.DesignSystem.Components.Layout.RbiGridColumns.Three or RBI.DesignSystem.Components.Layout.RbiGridColumns.Four)) throw new ArgumentOutOfRangeException(nameof(Columns));
        if (!Enum.IsDefined(Gap)) throw new ArgumentOutOfRangeException(nameof(Gap));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "section");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-grid rbi-grid--{Columns.ToString().ToLowerInvariant()} rbi-stat-group rbi-stat-group--{Gap.ToString().ToLowerInvariant()} {Class}".TrimEnd());
        builder.AddAttribute(3, "aria-label", string.IsNullOrWhiteSpace(AccessibleLabel) ? null : AccessibleLabel);
        builder.AddContent(4, ChildContent);
        builder.CloseElement();
    }
}
