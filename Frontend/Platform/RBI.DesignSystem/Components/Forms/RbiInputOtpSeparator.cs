using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using RBI.DesignSystem.Components.Icons;

namespace RBI.DesignSystem.Components.Forms;

/// <summary>A decorative separator between source-backed OTP groups, with no input semantics.</summary>
public sealed class RbiInputOtpSeparator : ComponentBase
{
    /// <summary>Optional caller-owned decoration; defaults to the source Minus icon.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional caller styling.</summary>
    [Parameter] public string? Class { get; set; }
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", $"rbi-otp__separator {Class}".TrimEnd());
        builder.AddAttribute(2, "aria-hidden", "true");
        if (ChildContent is not null) builder.AddContent(3, ChildContent);
        else { builder.OpenComponent<RbiIcon>(4); builder.AddAttribute(5, nameof(RbiIcon.Name), RbiIconName.Minus); builder.CloseComponent(); }
        builder.CloseElement();
    }
}
