using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Typography;

public sealed class RbiHeading : ComponentBase
{
    [Parameter] public int Level { get; set; } = 2;
    [Parameter] public int? VisualLevel { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void OnParametersSet()
    {
        ValidateLevel(Level, nameof(Level));
        if (VisualLevel is { } visualLevel) ValidateLevel(visualLevel, nameof(VisualLevel));
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, $"h{Level}");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", CssClass);
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }

    private string CssClass => $"rbi-heading rbi-heading--{VisualLevel ?? Level} {Class}".TrimEnd();

    private static void ValidateLevel(int level, string parameterName)
    {
        if (level is < 1 or > 6)
            throw new ArgumentOutOfRangeException(parameterName, level, "Heading levels must be between 1 and 6.");
    }
}
