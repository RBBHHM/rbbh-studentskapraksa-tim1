namespace RBI.DesignSystem.Components.Brand;

/// <summary>Application-owned signature artwork, retaining its actual intrinsic dimensions.</summary>
/// <param name="Source">Actual approved image URL; no text or generated replacement.</param>
/// <param name="Width">Intrinsic pixel width.</param>
/// <param name="Height">Intrinsic pixel height.</param>
public sealed record RbiSignatureAsset(string Source, int Width, int Height);
