namespace RBI.Localization;

/// <summary>Resolves text by semantic key for the current UI culture.</summary>
public interface IRbiLocalizer
{
    /// <summary>Gets localized text for a lowercase semantic key.</summary>
    /// <remarks>A missing key returns the key itself and logs once; a malformed key throws <see cref="ArgumentException"/>.</remarks>
    string this[string key] { get; }

    /// <summary>Gets localized text and applies current-culture composite formatting.</summary>
    string this[string key, params object?[] arguments] { get; }
}
