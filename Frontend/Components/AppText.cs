using RBI.Localization;

namespace RBBH.RelatedPartiesRegister.Web.Components;

// Application wording only. Optional RBI localization owns culture/fallback;
// the plain starter does not acquire localization packages or resource files.
public sealed class AppText
{
    private readonly IRbiLocalizer _localizer;
    public AppText(IRbiLocalizer localizer) => _localizer = localizer;
    public string this[string key] => _localizer[key];
    /// <summary>Display name of an application role: the localized "users.role.{name}" entry when one exists, otherwise the role name the backend supplied.</summary>
    public string Role(string role)
    {
        var key = "users.role." + role.ToLowerInvariant();
        var text = _localizer[key];
        return text == key ? role : text;
    }
    public string Format(string key, params object?[] arguments) => _localizer[key, arguments];
}
