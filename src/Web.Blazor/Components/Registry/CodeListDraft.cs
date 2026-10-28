using System.Globalization;
using System.Text.RegularExpressions;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace RBBH.RelatedPartiesRegister.Web.Components.Registry;

public sealed class CodeListDraft
{
    public bool Definition { get; init; }
    public int? Id { get; init; }
    public string Category { get; init; } = "";
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public string Order { get; set; } = "";
    public bool Active { get; set; } = true;
    public int? ParsedOrder => string.IsNullOrWhiteSpace(Order) ? null : int.TryParse(Order, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > (Definition ? 100 : 200) || (Definition && Name.Trim().Length < 2)) errors[nameof(Name)] = [Definition ? "codes.definition-range" : "codes.name-range"];
        if (Description.Length > 500) errors[nameof(Description)] = ["codes.description-range"];
        if (!Definition)
        {
            if (Id is null && (Code.Length is < 1 or > 50 || !Regex.IsMatch(Code, "^[A-Z0-9_-]+$", RegexOptions.CultureInvariant))) errors[nameof(Code)] = ["codes.code-range"];
            if (!string.IsNullOrWhiteSpace(Order) && ParsedOrder is null) errors[nameof(Order)] = ["codes.order-range"];
        }
        return errors;
    }
    public static CodeListDraft From(CodeValue value) => new() { Id = value.Id, Category = value.Category, Code = value.Code, Name = value.Value, Description = value.Description ?? "", Order = value.DisplayOrder?.ToString(CultureInfo.InvariantCulture) ?? "", Active = value.IsActive };
    public CreateCodeValue CreateRequest() => new(Category, Code, Name.Trim(), NullDescription, ParsedOrder, Active);
    public UpdateCodeValue UpdateRequest() => new(Name.Trim(), NullDescription, ParsedOrder, Active);
    public string? NullDescription => string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
}
