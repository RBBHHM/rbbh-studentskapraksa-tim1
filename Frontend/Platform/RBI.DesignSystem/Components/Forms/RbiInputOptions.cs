namespace RBI.DesignSystem.Components.Forms;

public enum RbiInputType { Text, Email, Password, Number, Search, Tel, Url }

/// <summary>The native browser event that updates a bound text value.</summary>
public enum RbiInputUpdateMode
{
    /// <summary>Commit on change/blur, preserving the original API default.</summary>
    Change,
    /// <summary>Update on each native input event.</summary>
    Input,
}
