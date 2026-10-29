using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RBI.DesignSystem.Components.Badge;
using RBI.DesignSystem.Components.Icons;

namespace RBI.DesignSystem.Components.Navigation;

/// <summary>In-flow, sectioned navigation with controlled selection and a labelled icon rail.</summary>
public sealed class RbiSideNav : ComponentBase
{
    private readonly HashSet<string> _expanded = new(StringComparer.Ordinal);
    private string? _lastActiveId;
    private IReadOnlyList<RbiSideNavSection>? _lastSections;
    private readonly HashSet<string> _activeGroups = new(StringComparer.Ordinal);
    private string? _pendingGroupExpansion;

    /// <summary>Unique, stable navigation ID, also prefixing its disclosure relationships.</summary>
    [Parameter, EditorRequired] public string Id { get; set; } = string.Empty;
    /// <summary>Localized accessible navigation name.</summary>
    [Parameter, EditorRequired] public string AccessibleLabel { get; set; } = string.Empty;
    /// <summary>Ordered navigation sections, with stable item IDs.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<RbiSideNavSection> Sections { get; set; } = [];
    /// <summary>Application-owned current action ID.</summary>
    [Parameter] public string ActiveId { get; set; } = string.Empty;
    /// <summary>Proposes an application-owned selection; does not mutate ActiveId.</summary>
    [Parameter] public EventCallback<string> ActiveIdChanged { get; set; }
    /// <summary>Whether the navigation is shown as a narrow icon rail.</summary>
    [Parameter] public bool Collapsed { get; set; }
    /// <summary>Proposes a rail expansion/collapse; the parent remains authoritative.</summary>
    [Parameter] public EventCallback<bool> CollapsedChanged { get; set; }
    /// <summary>Whether to show the rail collapse control.</summary>
    [Parameter] public bool Collapsible { get; set; } = true;
    /// <summary>Localized collapse-control name.</summary>
    [Parameter, EditorRequired] public string CollapseLabel { get; set; } = string.Empty;
    /// <summary>Localized expand-control name.</summary>
    [Parameter, EditorRequired] public string ExpandLabel { get; set; } = string.Empty;
    /// <summary>Optional expanded header content.</summary>
    [Parameter] public RenderFragment? Header { get; set; }
    /// <summary>Optional expanded footer content.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }
    /// <summary>Additional navigation classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional navigation attributes; structural ARIA is component-owned.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id);
        if (Id.Any(char.IsWhiteSpace)) throw new ArgumentException("ID must not contain whitespace.", nameof(Id));
        ArgumentException.ThrowIfNullOrWhiteSpace(AccessibleLabel);
        ArgumentNullException.ThrowIfNull(Sections);
        if (Collapsible)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(CollapseLabel);
            ArgumentException.ThrowIfNullOrWhiteSpace(ExpandLabel);
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var sectionIds = new HashSet<string>(StringComparer.Ordinal);
        var requiredGroups = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in Sections)
        {
            ArgumentNullException.ThrowIfNull(section);
            ArgumentException.ThrowIfNullOrWhiteSpace(section.Id);
            if (!sectionIds.Add(section.Id)) throw new ArgumentException("Section IDs must be unique.", nameof(Sections));
            Scan(section.Items, seen, requiredGroups);
        }
        // A group opened from the collapsed rail is only remembered once the parent has actually expanded the rail.
        if (_pendingGroupExpansion is not null)
        {
            if (!Collapsed && seen.Contains(_pendingGroupExpansion)) _expanded.Add(_pendingGroupExpansion);
            _pendingGroupExpansion = null;
        }
        _activeGroups.Clear();
        _activeGroups.UnionWith(requiredGroups);
        // Preserve user disclosure choices across unrelated parent renders.
        if (_lastActiveId != ActiveId || !ReferenceEquals(_lastSections, Sections))
            _expanded.UnionWith(requiredGroups);
        _expanded.IntersectWith(seen);
        _lastActiveId = ActiveId;
        _lastSections = Sections;
    }

    private bool Scan(IReadOnlyList<RbiSideNavItem> items, HashSet<string> seen, HashSet<string> requiredGroups)
    {
        ArgumentNullException.ThrowIfNull(items);
        var containsActive = false;
        foreach (var item in items)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Label);
            if (!seen.Add(item.Id)) throw new ArgumentException("Item IDs must be unique; cyclic navigation is invalid.", nameof(Sections));
            var childActive = Scan(item.Items, seen, requiredGroups);
            if (childActive) requiredGroups.Add(item.Id);
            containsActive |= childActive || item.Id == ActiveId;
        }
        return containsActive;
    }

    private Task SelectAsync(string id) => ActiveIdChanged.InvokeAsync(id);
    private Task ToggleCollapsedAsync() => CollapsedChanged.InvokeAsync(!Collapsed);

    private async Task ToggleGroupAsync(string id)
    {
        if (Collapsed)
        {
            _pendingGroupExpansion = id;
            await CollapsedChanged.InvokeAsync(false);
        }
        else if (!_expanded.Remove(id)) _expanded.Add(id);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "nav");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "id", Id);
        builder.AddAttribute(3, "aria-label", AccessibleLabel);
        builder.AddAttribute(4, "class", $"rbi-side-nav{(Collapsed ? " rbi-side-nav--collapsed" : string.Empty)} {Class}".TrimEnd());
        if (Header is not null || Collapsible)
        {
            builder.OpenElement(5, "div");
            builder.AddAttribute(6, "class", "rbi-side-nav__header");
            if (!Collapsed)
            {
                builder.OpenElement(7, "div");
                builder.AddAttribute(8, "class", "rbi-side-nav__header-content");
                builder.AddContent(9, Header);
                builder.CloseElement();
            }
            if (Collapsible)
            {
                builder.OpenElement(10, "button");
                builder.AddAttribute(11, "type", "button");
                builder.AddAttribute(12, "class", "rbi-side-nav__collapse");
                builder.AddAttribute(13, "aria-label", Collapsed ? ExpandLabel : CollapseLabel);
                builder.AddAttribute(14, "aria-expanded", (!Collapsed).ToString().ToLowerInvariant());
                builder.AddAttribute(15, "aria-controls", Id);
                builder.AddAttribute(16, "onclick", EventCallback.Factory.Create(this, ToggleCollapsedAsync));
                builder.OpenComponent<RbiIcon>(17);
                builder.AddAttribute(18, nameof(RbiIcon.Name), Collapsed ? RbiIconName.PanelLeftOpen : RbiIconName.PanelLeftClose);
                builder.CloseComponent();
                builder.CloseElement();
            }
            builder.CloseElement();
        }
        builder.OpenElement(19, "div");
        builder.AddAttribute(20, "class", "rbi-side-nav__content");
        foreach (var section in Sections)
        {
            builder.OpenElement(21, "div");
            builder.SetKey(section.Id);
            builder.AddAttribute(22, "class", "rbi-side-nav__section");
            if (!Collapsed && !string.IsNullOrWhiteSpace(section.Label))
            {
                builder.OpenElement(23, "p");
                builder.AddAttribute(24, "class", "rbi-side-nav__section-label");
                builder.AddContent(25, section.Label);
                builder.CloseElement();
            }
            builder.OpenElement(26, "ul");
            builder.OpenRegion(27);
            RenderNodes(builder, section.Items);
            builder.CloseRegion();
            builder.CloseElement();
            builder.CloseElement();
        }
        builder.CloseElement();
        if (!Collapsed && Footer is not null)
        {
            builder.OpenElement(28, "div");
            builder.AddAttribute(29, "class", "rbi-side-nav__footer");
            builder.AddContent(30, Footer);
            builder.CloseElement();
        }
        builder.CloseElement();
    }

    private void RenderNodes(RenderTreeBuilder builder, IReadOnlyList<RbiSideNavItem> items)
    {
        foreach (var item in items)
        {
            var group = item.Items.Count > 0;
            var open = !Collapsed && _expanded.Contains(item.Id);
            var contentId = $"{Id}-group-{Uri.EscapeDataString(item.Id)}";
            builder.OpenElement(0, "li");
            builder.SetKey(item.Id);
            builder.OpenElement(1, "button");
            builder.AddAttribute(2, "type", "button");
            builder.AddAttribute(3, "class", $"rbi-side-nav__item{(!group && ActiveId == item.Id ? " rbi-side-nav__item--active" : string.Empty)}{(group && _activeGroups.Contains(item.Id) ? " rbi-side-nav__item--contains-active" : string.Empty)}");
            builder.AddAttribute(4, "title", Collapsed ? item.Label : null);
            builder.AddAttribute(5, "aria-current", !group && ActiveId == item.Id ? "page" : null);
            builder.AddAttribute(6, "aria-expanded", group ? open.ToString().ToLowerInvariant() : null);
            builder.AddAttribute(7, "aria-controls", group ? contentId : null);
            builder.AddAttribute(8, "onclick", EventCallback.Factory.Create(this, () => group ? ToggleGroupAsync(item.Id) : SelectAsync(item.Id)));
            if (item.Icon is not null)
            {
                builder.OpenElement(9, "span");
                builder.AddAttribute(10, "class", "rbi-side-nav__icon");
                builder.AddAttribute(11, "aria-hidden", "true");
                builder.AddContent(12, item.Icon);
                builder.CloseElement();
            }
            builder.OpenElement(13, "span");
            builder.AddAttribute(14, "class", Collapsed ? "rbi-visually-hidden" : "rbi-side-nav__label");
            builder.AddContent(15, item.Label);
            builder.CloseElement();
            if (!Collapsed && group)
            {
                builder.OpenComponent<RbiIcon>(16);
                builder.AddAttribute(17, nameof(RbiIcon.Name), RbiIconName.ChevronDown);
                builder.AddAttribute(18, nameof(RbiIcon.Class), open ? "rbi-side-nav__chevron rbi-side-nav__chevron--open" : "rbi-side-nav__chevron");
                builder.CloseComponent();
            }
            else if (!Collapsed && !string.IsNullOrWhiteSpace(item.Badge))
            {
                builder.OpenComponent<RbiBadge>(19);
                builder.AddAttribute(20, nameof(RbiBadge.ChildContent), (RenderFragment)(content => content.AddContent(0, item.Badge)));
                builder.CloseComponent();
            }
            builder.CloseElement();
            if (group)
            {
                builder.OpenElement(21, "ul");
                builder.AddAttribute(22, "id", contentId);
                builder.AddAttribute(23, "class", "rbi-side-nav__children");
                builder.AddAttribute(24, "hidden", !open);
                builder.OpenRegion(25);
                RenderNodes(builder, item.Items);
                builder.CloseRegion();
                builder.CloseElement();
            }
            builder.CloseElement();
        }
    }
}
