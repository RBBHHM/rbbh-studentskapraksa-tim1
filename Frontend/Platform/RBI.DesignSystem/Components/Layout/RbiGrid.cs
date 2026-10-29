using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Layout;

/// <summary>Native source-backed Grid composition.</summary>
public sealed class RbiGrid : ComponentBase
{
    /// <summary>Desktop column configuration.</summary>
    [Parameter] public RbiGridColumns Columns { get; set; } = RbiGridColumns.Three;
    /// <summary>Source grid gap.</summary>
    [Parameter] public RbiGridGap Gap { get; set; } = RbiGridGap.Default;
    /// <summary>Composed child content.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional wrapper attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!Enum.IsDefined(Columns)) throw new ArgumentOutOfRangeException(nameof(Columns));
        if (!Enum.IsDefined(Gap)) throw new ArgumentOutOfRangeException(nameof(Gap));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-grid rbi-grid--{Columns.ToString().ToLowerInvariant()} rbi-grid--gap-{Gap.ToString().ToLowerInvariant()} {Class}".TrimEnd());
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
