using RBI.DesignSystem.Components.Icons;

namespace RBI.DesignSystem.Components.Forms;

/// <summary>An application-owned typed command, with visual/filter metadata but no business service.</summary>
public sealed record RbiCommandOption<TValue>(TValue Value, string Label)
{
    /// <summary>Optional searchable value distinct from the display label and typed action value.</summary>
    public string? SearchText { get; init; }
    /// <summary>Localized optional group label.</summary>
    public string? Group { get; init; }
    /// <summary>Additional search aliases.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();
    /// <summary>Decorative shortcut hint, not an automatically registered global hotkey.</summary>
    public string? Shortcut { get; init; }
    /// <summary>Optional source icon.</summary>
    public RbiIconName? Icon { get; init; }
    /// <summary>Whether execution/navigation is unavailable.</summary>
    public bool Disabled { get; init; }
}
