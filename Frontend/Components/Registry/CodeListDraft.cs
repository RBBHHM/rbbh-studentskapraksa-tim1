using System.Text.RegularExpressions;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

public sealed class CodeListDraft
{
    public string? SaveError { get; set; }
    public bool Definition { get; init; }
    public int? Id { get; init; }
    public string Category { get; init; } = "";
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Active { get; set; } = true;
    public bool IsLanguagePack => Category.Equals("LANGUAGE_PACKS", StringComparison.OrdinalIgnoreCase);
    public string LanguageCode { get; set; } = "";
    public string ResourceKey { get; set; } = "";
    public string ContextInfo { get; set; } = "";
    public string Helper { get; set; } = "";
    public string Placeholder { get; set; } = "";
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (IsLanguagePack)
        {
            if (string.IsNullOrWhiteSpace(Name) || Name.Length > 4000) errors[nameof(Name)] = ["codes.translation-required"];
            if (string.IsNullOrWhiteSpace(LanguageCode)) errors[nameof(LanguageCode)] = ["validation.required"];
            if (string.IsNullOrWhiteSpace(ResourceKey)) errors[nameof(ResourceKey)] = ["validation.required"];
            return errors;
        }
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > (Definition ? 100 : 200) || (Definition && Name.Trim().Length < 2)) errors[nameof(Name)] = [Definition ? "codes.definition-range" : "codes.name-range"];
        if (Description.Length > 500) errors[nameof(Description)] = ["codes.description-range"];
        if (!Definition)
        {
            if (Id is null && (Code.Length is < 1 or > 50 || !Regex.IsMatch(Code, "^[A-Z0-9_-]+$", RegexOptions.CultureInvariant))) errors[nameof(Code)] = ["codes.code-range"];
        }
        return errors;
    }
    public static CodeListDraft From(CodeValue value) => new()
    {
        Id = value.Id, Category = value.Category, Code = value.Code, Name = value.Category.Equals("LANGUAGE_PACKS", StringComparison.OrdinalIgnoreCase) ? value.Localization?.ResourceLabel ?? "" : value.Value,
        Description = value.Description ?? "", Active = value.IsActive, ResourceKey = value.Localization?.ResourceKey ?? "",
        LanguageCode = value.Localization?.LanguageCode ?? "", Helper = value.Localization?.ResourceHelper ?? "",
        Placeholder = value.Localization?.ResourcePlaceholder ?? "", ContextInfo = value.Localization?.ContextInfo ?? ""
    };
    public CreateCodeValue CreateRequest() => new(Category, Code, Name.Trim(), NullDescription, Active);
    public UpdateCodeValue UpdateRequest() => new(Name.Trim(), NullDescription, Active);
    public string? NullDescription => string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
}
