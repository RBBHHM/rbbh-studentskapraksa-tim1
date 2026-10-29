using Microsoft.AspNetCore.Components;
using RBI.DesignSystem.Components.Icons;

namespace RBI.DesignSystem.Components;

// Internal, fixed SVG geometry for framework-owned controls; not a consumer icon API.
internal static class RbiControlIcon
{
    internal static RenderFragment Render(string name) => builder =>
    {
        if (name == "close")
        {
            builder.OpenComponent<RbiIcon>(0);
            builder.AddAttribute(1, nameof(RbiIcon.Name), RbiIconName.X);
            builder.AddAttribute(2, nameof(RbiIcon.Class), "rbi-control-icon");
            builder.CloseComponent();
            return;
        }
        var paths = name switch
        {
            "previous" => "<path d='m15 18-6-6 6-6'/>",
            "next" => "<path d='m9 18 6-6-6-6'/>",
            "down" => "<path d='m6 9 6 6 6-6'/>",
            "ellipsis" => "<circle cx='5' cy='12' r='1'/><circle cx='12' cy='12' r='1'/><circle cx='19' cy='12' r='1'/>",
            "sun" => "<circle cx='12' cy='12' r='4'/><path d='M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.5 1.5m11 11L19 19M5 19l1.5-1.5m11-11L19 5'/>",
            "moon" => "<path d='M20.8 13A9 9 0 0 1 11 3.2 9 9 0 1 0 20.8 13Z'/>",
            "system" => "<rect x='3' y='4' width='18' height='13' rx='2'/><path d='M8 21h8m-4-4v4'/>",
            "success" => "<path d='m5 12 4 4L19 6'/>",
            "warning" => "<path d='M12 4 2 21h20ZM12 10v5m0 3h.01'/>",
            "brand" => "<path d='m12 3 9 9-9 9-9-9Z'/>",
            _ => "<circle cx='12' cy='12' r='9'/><path d='M12 11v6m0-10h.01'/>",
        };
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", "0 0 24 24");
        builder.AddAttribute(2, "width", "16");
        builder.AddAttribute(3, "height", "16");
        builder.AddAttribute(4, "fill", "none");
        builder.AddAttribute(5, "stroke", "currentColor");
        builder.AddAttribute(6, "stroke-width", "2");
        builder.AddAttribute(7, "stroke-linecap", "round");
        builder.AddAttribute(8, "stroke-linejoin", "round");
        builder.AddAttribute(9, "aria-hidden", "true");
        builder.AddAttribute(10, "focusable", "false");
        builder.AddMarkupContent(11, paths);
        builder.CloseElement();
    };
}
