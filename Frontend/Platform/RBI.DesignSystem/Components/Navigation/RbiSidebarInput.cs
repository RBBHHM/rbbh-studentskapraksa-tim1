using RBI.DesignSystem.Components.Forms;

namespace RBI.DesignSystem.Components.Navigation;

/// <summary>Sidebar-sized string input; retains the existing native EditForm/InputBase contract.</summary>
public sealed class RbiSidebarInput : RbiInput
{
    protected override void OnParametersSet()
    {
        var classes = RbiFormUtilities.Attribute(AdditionalAttributes, "class");
        if (classes?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("rbi-sidebar-input", StringComparer.Ordinal) != true)
        {
            var attributes = AdditionalAttributes is null ? new Dictionary<string, object>() : new(AdditionalAttributes);
            attributes["class"] = $"rbi-sidebar-input {classes}".TrimEnd(); AdditionalAttributes = attributes;
        }
        base.OnParametersSet();
    }
}
