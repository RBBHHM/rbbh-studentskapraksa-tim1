namespace RBI.DesignSystem.Components.Navigation;

public sealed record RbiBreadcrumbEntry(string Label, string? Href = null, bool Current = false)
{
    /// <summary>Renders a collapsed-items marker instead of a link; <see cref="Label"/> is its visually hidden text.</summary>
    public bool Ellipsis { get; init; }
}
