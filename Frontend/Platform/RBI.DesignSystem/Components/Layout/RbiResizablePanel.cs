using Microsoft.AspNetCore.Components;

namespace RBI.DesignSystem.Components.Layout;

/// <summary>A typed native panel definition; UI content remains application-owned.</summary>
public sealed record RbiResizablePanel(string Id, string AccessibleLabel, RenderFragment Content)
{
    /// <summary>Initial proportional share before normalization across the group.</summary>
    public double InitialSize { get; init; } = 50;
    /// <summary>Minimum percentage of group space.</summary>
    public double MinSize { get; init; }
    /// <summary>Maximum percentage of group space.</summary>
    public double MaxSize { get; init; } = 100;
}
