using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RBI.DesignSystem.Components.Brand;

/// <summary>Reusable signature presentation; applications own the approved artwork and attribution.</summary>
public sealed class RbiAuthorSignature : ComponentBase
{
    /// <summary>Actual application-owned image and intrinsic dimensions.</summary>
    [Parameter, EditorRequired] public RbiSignatureAsset Asset { get; set; } = default!;
    /// <summary>Localized accessible name for the real handwritten signature.</summary>
    [Parameter, EditorRequired] public string AlternativeText { get; set; } = string.Empty;
    /// <summary>Localized attribution, emphasized beside the signature.</summary>
    [Parameter, EditorRequired] public string Credit { get; set; } = string.Empty;
    /// <summary>Localized supporting note, never baked into the image.</summary>
    [Parameter] public string? Note { get; set; }
    /// <summary>Ink treatment suited to the surrounding surface.</summary>
    [Parameter] public RbiSignatureTone Tone { get; set; }
    /// <summary>Additional wrapper classes.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Additional figure attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Asset);
        ArgumentException.ThrowIfNullOrWhiteSpace(Asset.Source);
        if (Asset.Width <= 0 || Asset.Height <= 0) throw new ArgumentOutOfRangeException(nameof(Asset));
        ArgumentException.ThrowIfNullOrWhiteSpace(AlternativeText);
        ArgumentException.ThrowIfNullOrWhiteSpace(Credit);
        if (!Enum.IsDefined(Tone)) throw new ArgumentOutOfRangeException(nameof(Tone));
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "figure");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", $"rbi-signature rbi-signature--{Tone.ToString().ToLowerInvariant()} {Class}".TrimEnd());
        builder.OpenElement(3, "img");
        builder.AddAttribute(4, "src", Asset.Source);
        // Actual IHDR dimensions reserve the artwork's real aspect ratio.
        builder.AddAttribute(5, "width", Asset.Width);
        builder.AddAttribute(6, "height", Asset.Height);
        builder.AddAttribute(7, "loading", "lazy");
        builder.AddAttribute(8, "alt", AlternativeText);
        builder.CloseElement();
        builder.OpenElement(9, "figcaption");
        builder.OpenElement(10, "strong");
        builder.AddContent(11, Credit);
        builder.CloseElement();
        builder.AddContent(12, Note);
        builder.CloseElement();
        builder.CloseElement();
    }
}
