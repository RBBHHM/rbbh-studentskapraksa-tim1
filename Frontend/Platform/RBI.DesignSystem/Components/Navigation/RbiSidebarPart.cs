using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RBI.DesignSystem.Components.Forms;

namespace RBI.DesignSystem.Components.Navigation;

/// <summary>Semantic structural slots shared by a sidebar's header/footer, groups and nested lists.</summary>
public sealed class RbiSidebarPart : ComponentBase
{
    [Parameter] public RbiSidebarPartKind Kind { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
    protected override void OnParametersSet() { if (!Enum.IsDefined(Kind)) throw new ArgumentOutOfRangeException(nameof(Kind)); }
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var tag = Kind switch { RbiSidebarPartKind.Menu or RbiSidebarPartKind.SubMenu => "ul", RbiSidebarPartKind.MenuItem or RbiSidebarPartKind.SubMenuItem => "li", RbiSidebarPartKind.Separator => "hr", _ => "div" };
        var name = Kind switch { RbiSidebarPartKind.GroupLabel => "group-label", RbiSidebarPartKind.GroupContent => "group-content", RbiSidebarPartKind.MenuItem => "menu-item", RbiSidebarPartKind.SubMenu => "menu-sub", RbiSidebarPartKind.SubMenuItem => "menu-sub-item", _ => Kind.ToString().ToLowerInvariant() };
        builder.OpenElement(0, tag); builder.AddMultipleAttributes(1, RbiFormUtilities.SafeAttributes(AdditionalAttributes, "class"));
        builder.AddAttribute(2, "class", $"rbi-sidebar-{name} {Class}".TrimEnd());
        builder.AddContent(3, ChildContent); builder.CloseElement();
    }
}
