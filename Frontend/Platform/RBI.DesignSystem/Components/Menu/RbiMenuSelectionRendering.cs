using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RBI.DesignSystem.Components.Icons;

namespace RBI.DesignSystem.Components.Menu;

internal static class RbiMenuSelectionRendering
{
    internal static void Render(RenderTreeBuilder builder, IReadOnlyDictionary<string, object>? attributes, string label, string? cssClass, bool disabled, bool? selected, bool radio, EventCallback activation)
    {
        builder.OpenElement(0, "button");
        builder.AddMultipleAttributes(1, attributes);
        builder.AddAttribute(2, "type", "button");
        builder.AddAttribute(3, "role", radio ? "menuitemradio" : "menuitemcheckbox");
        builder.AddAttribute(4, "class", $"rbi-menu-item rbi-menu-selection-item {(radio ? "rbi-menu-selection-item--radio" : "")} {cssClass}".TrimEnd());
        builder.AddAttribute(5, "tabindex", -1);
        builder.AddAttribute(6, "aria-disabled", disabled ? "true" : null);
        builder.AddAttribute(7, "aria-checked", selected is null ? "mixed" : selected.Value ? "true" : "false");
        builder.AddAttribute(8, "data-rbi-menu-item", "");
        builder.AddAttribute(9, "data-rbi-menu-text", label);
        builder.AddAttribute(10, "onclick", activation);
        builder.OpenElement(11, "span");
        builder.AddAttribute(12, "class", "rbi-menu-selection-item__indicator");
        builder.AddAttribute(13, "aria-hidden", "true");
        if (selected != false)
        {
            builder.OpenComponent<RbiIcon>(14);
            builder.AddAttribute(15, nameof(RbiIcon.Name), radio ? RbiIconName.Circle : selected is null ? RbiIconName.Minus : RbiIconName.Check);
            builder.CloseComponent();
        }
        builder.CloseElement();
        builder.AddContent(16, label);
        builder.CloseElement();
    }
}
