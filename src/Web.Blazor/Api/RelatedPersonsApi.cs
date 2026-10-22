using RBI.ApiCore;

namespace RBBH.RelatedPartiesRegister.Web.Api;

public sealed record RelatedPerson(Guid Id, string FirstName, string LastName, int Residency,
    string? Jmbg, string? PassportNumber, string? FbaId, string? RelationBasis, string? RelationDescription,
    string? SpecialRelationBasis, DateTime? DateFrom, DateTime? DateTo, bool IsIdentifiedStaff,
    bool DeclarationNoFamilyMembers, bool ConnectedWithBank, bool SpecialRelationshipWithBank,
    bool SpecialContract, bool MalusClawback, Guid? RelatedToPersonId, string? RelatedToPersonName,
    int Status, string StatusLabel, int FamilyMemberCount, DateTime CreatedAt = default, string? CreatedBy = null,
    DateTime? ModifiedAt = null, string? ModifiedBy = null, string? VerifiedBy = null, DateTime? VerifiedAt = null,
    string? PersonType = null, string? PersonTypeLabel = null, string? ResidencyLabel = null)
{
    public bool Matches(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        // Same scalar response values as the existing client-side search; no
        // generic object/envelope normalization or server-side query rewrite.
        object?[] values = [Id, FirstName, LastName, Residency, ResidencyLabel, Jmbg, PassportNumber, FbaId,
            RelationBasis, RelationDescription, SpecialRelationBasis, DateFrom?.ToString("O"), DateTo?.ToString("O"),
            IsIdentifiedStaff, DeclarationNoFamilyMembers, ConnectedWithBank, SpecialRelationshipWithBank,
            SpecialContract, MalusClawback, RelatedToPersonId, RelatedToPersonName, Status, StatusLabel,
            CreatedAt.ToString("O"), CreatedBy, ModifiedAt?.ToString("O"), ModifiedBy, VerifiedBy, VerifiedAt?.ToString("O"),
            FamilyMemberCount, PersonType, PersonTypeLabel];
        return values.Any(value => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
    }
}

public sealed record PersonRequest(string FirstName, string LastName, int Residency,
    string? Jmbg, string? PassportNumber, string? FbaId, string RelationBasis, string RelationDescription,
    string SpecialRelationBasis, DateTime? DateFrom, DateTime? DateTo, bool IsIdentifiedStaff,
    bool DeclarationNoFamilyMembers, bool ConnectedWithBank, bool SpecialRelationshipWithBank,
    bool SpecialContract, bool MalusClawback, Guid? RelatedToPersonId);

public sealed record PersonTreeNode(Guid Id, string FirstName, string LastName, string PersonType,
    IReadOnlyList<PersonTreeNode> Children);
public sealed record DuplicateIdentity(bool Exists, string? Field, string? Message);
public sealed record ImportResult(int Imported, int Failed, IReadOnlyList<string> Errors);
public sealed record ReferenceOption(string Kod, string Naziv, string? Opis);

public interface IRelatedPersonsApi
{
    Task<IReadOnlyList<RelatedPerson>> ListAsync(CancellationToken token = default);
    Task<IReadOnlyList<PersonTreeNode>> TreeAsync(Guid id, CancellationToken token = default);
    Task<DuplicateIdentity> CheckDuplicateAsync(string field, string value, Guid? excludeId, CancellationToken token = default);
    Task<IReadOnlyList<ReferenceOption>> OptionsAsync(string category, CancellationToken token = default);
    Task<RelatedPerson> SaveAsync(Guid? id, PersonRequest request, CancellationToken token = default);
    Task VerifyAsync(Guid id, CancellationToken token = default);
    Task DeleteAsync(Guid id, CancellationToken token = default);
    Task<RbiApiFile> ExportAsync(CancellationToken token = default);
    Task<ImportResult> ImportAsync(Stream file, string fileName, CancellationToken token = default);
}

public sealed class RelatedPersonsApi(IRbiApiFileTransport transport) : IRelatedPersonsApi
{
    public async Task<IReadOnlyList<RelatedPerson>> ListAsync(CancellationToken token = default) =>
        Value(await transport.GetAsync<List<RelatedPerson>>("api/related-persons/detailed", token), token);
    public async Task<IReadOnlyList<PersonTreeNode>> TreeAsync(Guid id, CancellationToken token = default) =>
        Value(await transport.GetAsync<List<PersonTreeNode>>($"api/related-persons/{id:D}/relationship-tree", token), token);
    public async Task<DuplicateIdentity> CheckDuplicateAsync(string field, string value, Guid? excludeId, CancellationToken token = default)
    {
        if (field is not ("jmbg" or "passportNumber" or "fbaId")) throw new ArgumentException("Unknown identity field.", nameof(field));
        var query = $"{field}={Uri.EscapeDataString(value)}" + (excludeId is { } id ? $"&excludeId={id:D}" : "");
        return Value(await transport.GetAsync<DuplicateIdentity>($"api/related-persons/check-duplicate?{query}", token), token);
    }
    public async Task<IReadOnlyList<ReferenceOption>> OptionsAsync(string category, CancellationToken token = default) =>
        Value(await transport.GetAsync<List<ReferenceOption>>($"api/code-lists/dropdown/{Uri.EscapeDataString(category)}", token), token);
    public async Task<RelatedPerson> SaveAsync(Guid? id, PersonRequest request, CancellationToken token = default) =>
        Value(await transport.SendAsync<RelatedPerson>(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            "api/related-persons" + (id is { } key ? $"/{key:D}" : ""), request, token), token);
    public async Task VerifyAsync(Guid id, CancellationToken token = default) =>
        Value(await transport.SendAsync<RelatedPerson>(HttpMethod.Post, $"api/verification/physical-person/{id:D}", cancellationToken: token), token);
    public async Task DeleteAsync(Guid id, CancellationToken token = default)
    {
        var result = await transport.SendAsync(HttpMethod.Delete, $"api/related-persons/{id:D}", cancellationToken: token);
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
    }
    public async Task<RbiApiFile> ExportAsync(CancellationToken token = default) =>
        Value(await transport.DownloadAsync("api/related-persons/export", cancellationToken: token), token);
    public async Task<ImportResult> ImportAsync(Stream file, string fileName, CancellationToken token = default)
    {
        using var multipart = new MultipartFormDataContent();
        var content = new StreamContent(file);
        content.Headers.ContentType = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        multipart.Add(content, "file", fileName);
        return Value(await transport.PostMultipartAsync<ImportResult>("api/related-persons/import", multipart, token), token);
    }
    private static T Value<T>(RbiApiResult<T> result, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!result.IsSuccess) throw new RegistryApiException(result.Error?.StatusCode);
        return result.Value;
    }
}

internal sealed class UnconfiguredRelatedPersonsApi : IRelatedPersonsApi
{
    private static Task<T> Unavailable<T>() => Task.FromException<T>(new RegistryApiException());
    public Task<IReadOnlyList<RelatedPerson>> ListAsync(CancellationToken token = default) => Unavailable<IReadOnlyList<RelatedPerson>>();
    public Task<IReadOnlyList<PersonTreeNode>> TreeAsync(Guid id, CancellationToken token = default) => Unavailable<IReadOnlyList<PersonTreeNode>>();
    public Task<DuplicateIdentity> CheckDuplicateAsync(string field, string value, Guid? excludeId, CancellationToken token = default) => Unavailable<DuplicateIdentity>();
    public Task<IReadOnlyList<ReferenceOption>> OptionsAsync(string category, CancellationToken token = default) => Unavailable<IReadOnlyList<ReferenceOption>>();
    public Task<RelatedPerson> SaveAsync(Guid? id, PersonRequest request, CancellationToken token = default) => Unavailable<RelatedPerson>();
    public Task VerifyAsync(Guid id, CancellationToken token = default) => Unavailable<object>();
    public Task DeleteAsync(Guid id, CancellationToken token = default) => Unavailable<object>();
    public Task<RbiApiFile> ExportAsync(CancellationToken token = default) => Unavailable<RbiApiFile>();
    public Task<ImportResult> ImportAsync(Stream file, string fileName, CancellationToken token = default) { file.Dispose(); return Unavailable<ImportResult>(); }
}
