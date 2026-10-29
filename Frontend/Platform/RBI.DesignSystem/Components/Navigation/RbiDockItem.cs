using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace RBI.DesignSystem.Components.Navigation;

/// <summary>Native link or view-selection button within a labelled Dock.</summary>
public sealed class RbiDockItem : ComponentBase
{
    /// <summary>Navigation destination; null renders a native selection button.</summary>
    [Parameter] public string? Href { get; set; }
    /// <summary>Whether this item represents the current view.</summary>
    [Parameter] public bool Active { get; set; }
    /// <summary>Application-owned activation callback; navigation stays native for links.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    /// <summary>Visible, named link content, including optional decorative icons.</summary>
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional item classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional interactive element attributes, such as accessible names.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ChildContent is null) throw new InvalidOperationException($"{nameof(ChildContent)} is required.");
        if (Href is not null && string.IsNullOrWhiteSpace(Href)) throw new ArgumentException("A destination cannot be blank.", nameof(Href));
    }

    /// <inheritdoc />
    private static readonly HashSet<string> CurrentTokens = new(StringComparer.OrdinalIgnoreCase) { "page", "step", "location", "date", "time", "true", "false" };

    // A caller-supplied valid aria-current token wins (the source anchor jumper uses "true"); otherwise the active item is the current page.
    private string? CurrentToken()
    {
        if (AdditionalAttributes is not null
            && AdditionalAttributes.TryGetValue("aria-current", out var supplied)
            && supplied?.ToString()?.Trim() is { Length: > 0 } token
            && CurrentTokens.Contains(token))
            return token.ToLowerInvariant();
        return Active ? "page" : null;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "li");
        builder.OpenElement(1, Href is null ? "button" : "a");
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttribute(3, "class", $"rbi-dock-item{(Active ? " rbi-dock-item--active" : string.Empty)} {Class}".TrimEnd());
        builder.AddAttribute(4, "href", Href);
        builder.AddAttribute(5, "type", Href is null ? "button" : null);
        builder.AddAttribute(6, "aria-current", CurrentToken());
        builder.AddAttribute(7, "onclick", OnClick);
        builder.AddContent(8, ChildContent);
        builder.CloseElement();
        builder.CloseElement();
    }
}
