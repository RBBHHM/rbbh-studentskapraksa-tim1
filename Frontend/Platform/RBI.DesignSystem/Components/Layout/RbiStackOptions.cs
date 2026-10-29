namespace RBI.DesignSystem.Components.Layout;

public enum RbiStackDirection { Column, Row }
public enum RbiStackGap
{
    /// <summary>No gap.</summary>
    None,
    /// <summary>4px.</summary>
    XSmall,
    /// <summary>8px.</summary>
    Small,
    /// <summary>16px, the default.</summary>
    Medium,
    /// <summary>24px.</summary>
    Large,
    /// <summary>32px.</summary>
    XLarge,
    /// <summary>48px.</summary>
    XXLarge,
    /// <summary>12px, between Small and Medium; appended so the existing values keep their numbers.</summary>
    Snug,
}
public enum RbiStackAlign { Start, Center, End, Stretch, Baseline }
public enum RbiStackJustify { Start, Center, End, Between }
