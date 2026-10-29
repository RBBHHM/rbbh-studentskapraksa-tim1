namespace RBI.Localization.Internal;

internal interface IRbiLocalizationProvider
{
    IEnumerable<string> GetValues(string cultureName, string key);
}
