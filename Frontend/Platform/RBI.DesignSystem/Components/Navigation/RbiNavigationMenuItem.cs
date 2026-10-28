using Microsoft.AspNetCore.Components;

namespace RBI.DesignSystem.Components.Navigation;

/// <summary>A navigation link or disclosure of application-owned navigation content.</summary>
public sealed record RbiNavigationMenuItem(string Id, string Label)
{
    /// <summary>Destination for a simple native link.</summary>
    public string? Href { get; init; }
    /// <summary>Rich content in the shared viewport; use ordinary native links.</summary>
    public RenderFragment? Content { get; init; }
    /// <summary>Whether disclosure/link is unavailable.</summary>
    public bool Disabled { get; init; }
    /// <summary>Marks the active native destination.</summary>
    public bool Current { get; init; }
}
