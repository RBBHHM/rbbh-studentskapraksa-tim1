using Microsoft.AspNetCore.Components;

namespace RBI.DesignSystem.Components.Navigation;

/// <summary>A named navigation action or disclosure group; application routes stay external.</summary>
public sealed record RbiSideNavItem(string Id, string Label)
{
    /// <summary>Optional decorative artwork.</summary>
    public RenderFragment? Icon { get; init; }
    /// <summary>Optional visible count or status.</summary>
    public string? Badge { get; init; }
    /// <summary>Nested actions; nonempty children turn the item into a disclosure group.</summary>
    public IReadOnlyList<RbiSideNavItem> Items { get; init; } = [];
}

/// <summary>A section of navigation actions with an optional visible heading.</summary>
public sealed record RbiSideNavSection(string Id, IReadOnlyList<RbiSideNavItem> Items)
{
    /// <summary>Optional application-owned section wording.</summary>
    public string? Label { get; init; }
}
