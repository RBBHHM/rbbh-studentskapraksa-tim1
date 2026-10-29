using Microsoft.AspNetCore.Components;
using RBI.DesignSystem.Components.Icons;

namespace RBI.DesignSystem.Components.Display;

/// <summary>UI-only series presentation; data and chart geometry remain owned by the consumer.</summary>
public sealed record RbiChartSeries(string Key, string? Label = null)
{
    /// <summary>Optional CSS color or semantic variable for the light/default theme.</summary>
    public string? Color { get; init; }
    /// <summary>Optional dark-theme color; omitted values retain the default color.</summary>
    public string? DarkColor { get; init; }
    /// <summary>Optional typed source icon, replacing the standard color indicator.</summary>
    public RbiIconName? Icon { get; init; }
    /// <summary>Optional rich presentation label, shared by legend and default tooltip rows.</summary>
    public RenderFragment? LabelContent { get; init; }
}

/// <summary>Already formatted UI tooltip entry, never a transport or business DTO.</summary>
public sealed record RbiChartTooltipEntry(string SeriesKey, string? ValueText = null)
{
    /// <summary>Optional label overriding the series label.</summary>
    public string? Label { get; init; }
    /// <summary>Optional indicator color overriding the series color.</summary>
    public string? Color { get; init; }
    /// <summary>Excludes a source-equivalent 'none' payload entry.</summary>
    public bool Hidden { get; init; }
}

/// <summary>Source tooltip indicator presentations.</summary>
public enum RbiChartIndicator
{
    /// <summary>Square dot indicator.</summary>
    Dot,
    /// <summary>Solid vertical line.</summary>
    Line,
    /// <summary>Dashed vertical line.</summary>
    Dashed,
}

/// <summary>Source legend placement and padding.</summary>
public enum RbiChartLegendPosition
{
    /// <summary>Bottom legend with top padding.</summary>
    Bottom,
    /// <summary>Top legend with bottom padding.</summary>
    Top,
}
