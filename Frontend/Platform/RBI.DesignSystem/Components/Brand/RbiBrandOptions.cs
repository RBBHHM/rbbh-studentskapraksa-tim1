namespace RBI.DesignSystem.Components.Brand;

/// <summary>Approved artwork variants; never recolor or redraw these lockups.</summary>
public enum RbiLogoVariant
{
    /// <summary>Color endorsed lockup on a light surface.</summary>
    Colour,
    /// <summary>Color endorsed lockup on an inverse surface.</summary>
    ColourInverse,
    /// <summary>Monochrome endorsed lockup on a light surface.</summary>
    Mono,
    /// <summary>Monochrome endorsed lockup on an inverse surface.</summary>
    MonoInverse,
    /// <summary>Yellow endorsed lockup on an inverse surface.</summary>
    YellowInverse,
    /// <summary>Bank lockup on a light surface.</summary>
    BankMono,
    /// <summary>Yellow bank lockup on an inverse surface.</summary>
    BankYellowInverse,
    /// <summary>Square bank mark with its own yellow field.</summary>
    BankMark,
}

/// <summary>Approved optical logo heights.</summary>
public enum RbiLogoSize
{
    /// <summary>20 pixels.</summary>
    XSmall,
    /// <summary>24 pixels.</summary>
    Small,
    /// <summary>36 pixels.</summary>
    Medium,
    /// <summary>48 pixels.</summary>
    Large,
    /// <summary>64 pixels.</summary>
    XLarge,
}

/// <summary>Signature artwork treatment, independent of the theme preference.</summary>
public enum RbiSignatureTone
{
    /// <summary>Original dark ink on a light surface.</summary>
    Default,
    /// <summary>Inverse ink for an inverse surface.</summary>
    Inverse,
}

/// <summary>Semantic elements supported by the brand claim.</summary>
public enum RbiSloganElement
{
    /// <summary>Inline span.</summary>
    Span,
    /// <summary>Paragraph.</summary>
    Paragraph,
    /// <summary>Generic block.</summary>
    Div,
    /// <summary>Level-one heading.</summary>
    Heading1,
    /// <summary>Level-two heading.</summary>
    Heading2,
}
