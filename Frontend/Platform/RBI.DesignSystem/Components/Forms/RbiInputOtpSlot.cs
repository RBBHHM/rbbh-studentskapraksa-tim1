using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Forms;

/// <summary>A noninteractive visual OTP slot backed by a root-provided immutable state.</summary>
public sealed class RbiInputOtpSlot : ComponentBase
{
    /// <summary>The root-provided indexed character, active and caret state.</summary>
    [Parameter] public RbiOtpSlot Slot { get; set; }
    /// <summary>Additional caller styling.</summary>
    [Parameter] public string? Class { get; set; }
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", $"rbi-otp__slot {Class}".TrimEnd());
        builder.AddAttribute(2, "aria-hidden", "true");
        builder.AddAttribute(3, "data-rbi-otp-slot", Slot.Index);
        builder.AddAttribute(4, "data-active", Slot.Active ? "true" : null);
        builder.AddContent(5, Slot.Character?.ToString());
        if (Slot.HasCaret)
        {
            builder.OpenElement(6, "span");
            builder.AddAttribute(7, "class", "rbi-otp__caret");
            builder.CloseElement();
        }
        builder.CloseElement();
    }
}
