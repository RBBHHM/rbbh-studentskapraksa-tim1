using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Forms;

/// <summary>A decorative group of OTP slots. Unequal groups retain the root's single native input.</summary>
public sealed class RbiInputOtpGroup : ComponentBase
{
    /// <summary>Decorative typed slot composition.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Additional caller styling.</summary>
    [Parameter] public string? Class { get; set; }
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", $"rbi-otp__group {Class}".TrimEnd());
        builder.AddAttribute(2, "aria-hidden", "true");
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
