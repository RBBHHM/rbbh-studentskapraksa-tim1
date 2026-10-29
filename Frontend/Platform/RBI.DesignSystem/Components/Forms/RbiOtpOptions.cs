namespace RBI.DesignSystem.Components.Forms;

/// <summary>Source-supported ASCII code character sets.</summary>
public enum RbiOtpCharacterSet
{
    /// <summary>No character restriction, matching an unspecified source pattern.</summary>
    Any,
    /// <summary>ASCII digits.</summary>
    Digits,
    /// <summary>ASCII letters.</summary>
    Letters,
    /// <summary>ASCII letters and digits.</summary>
    Alphanumeric,
}

/// <summary>Immutable visual slot state for a consumer-supplied template.</summary>
/// <param name="Index">Zero-based visual slot index.</param>
/// <param name="Character">Current code character, or null for an empty slot.</param>
/// <param name="Active">Whether native selection includes this slot.</param>
/// <param name="HasCaret">Whether the active slot is empty.</param>
public readonly record struct RbiOtpSlot(int Index, char? Character, bool Active, bool HasCaret);
