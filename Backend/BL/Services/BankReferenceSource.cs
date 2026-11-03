using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.Exceptions;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class BankReferenceOptions
{
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "";
    public string CustomerTable { get; set; } = "kod.dbo.komitent";
    // The actual tax/name columns require bank-owner confirmation; no guessed mapping.
    public string CustomerTaxColumn { get; set; } = "";
    public string CustomerNameColumn { get; set; } = "";
    public string CustomerMatbrojColumn { get; set; } = "matbroj";
    public string GccTable { get; set; } = "glava.dbo.grupekom";
    public string GccNumberColumn { get; set; } = "brojg";
    public string GccNameColumn { get; set; } = "naziv_grupe";
}
public sealed record BankCustomer(string TaxNumber, string Matbroj, string Name);
public sealed record BankGroup(string Number, string Name);
public sealed record ReferencePage<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public interface IBankReferenceSource
{
    bool IsConfigured { get; }
    Task<ReferencePage<BankCustomer>> Customers(string? search, int page, int pageSize, CancellationToken ct);
    Task<ReferencePage<BankGroup>> Groups(string? search, int page, int pageSize, CancellationToken ct);
    Task<BankGroup?> Group(string number, CancellationToken ct);
}

/// <summary>Parameterized read-only adapter; production must use a SELECT-only bank account.</summary>
public sealed class SqlBankReferenceSource(IOptions<BankReferenceOptions> options, ILogger<SqlBankReferenceSource> logger) : IBankReferenceSource
{
    private BankReferenceOptions Settings => options.Value;
    public bool IsConfigured => Settings.Enabled && !string.IsNullOrWhiteSpace(Settings.ConnectionString);
    private static string Identifier(string value)
    {
        var parts = value.Split('.');
        if (parts.Length is < 1 or > 3 || parts.Any(part => !Regex.IsMatch(part, "^[A-Za-z_][A-Za-z0-9_]{0,127}$", RegexOptions.CultureInvariant)))
            throw Unavailable();
        return string.Join('.', parts.Select(part => "[" + part + "]"));
    }
    private static BusinessRuleException Unavailable() => new(503, "bank_reference.unavailable", "Bankovni referentni podaci trenutno nisu dostupni.");
    private async Task<ReferencePage<T>> Read<T>(string table, string[] columns, string? search, int page, int size,
        bool exact, Func<SqlDataReader, T> map, CancellationToken ct)
    {
        if (!IsConfigured) throw Unavailable();
        if (search?.Length > 100) throw new BusinessRuleException(400, "bank_reference.invalid_search", "Pretraga smije imati najviše 100 znakova.");
        page = Math.Clamp(page, 1, 1_000_000); size = Math.Clamp(size, 1, 100);
        var source = Identifier(table); var fields = columns.Select(Identifier).ToArray();
        var projected = fields.Select(field => $"CONVERT(nvarchar(4000),{field})").ToArray();
        var predicate = exact ? $"{projected[0]} = @term" : string.Join(" OR ", projected.Select(field => $"{field} COLLATE Latin1_General_100_CI_AS LIKE @term ESCAPE '~'"));
        var term = search?.Trim() ?? "";
        if (!exact) term = "%" + term.Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~[") + "%";
        try
        {
            var connectionOptions = new SqlConnectionStringBuilder(Settings.ConnectionString) { ConnectTimeout = 5 };
            await using var connection = new SqlConnection(connectionOptions.ConnectionString); await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand(); command.CommandTimeout = 10;
            command.CommandText = $"SELECT COUNT(*) FROM {source} WHERE {predicate}; SELECT {string.Join(',', projected)} FROM {source} WHERE {predicate} ORDER BY {string.Join(',', projected)} OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;";
            command.Parameters.Add("@term", SqlDbType.NVarChar, 500).Value = term;
            command.Parameters.Add("@offset", SqlDbType.Int).Value = (page - 1) * size;
            command.Parameters.Add("@take", SqlDbType.Int).Value = size;
            await using var reader = await command.ExecuteReaderAsync(ct); await reader.ReadAsync(ct); var total = reader.GetInt32(0);
            await reader.NextResultAsync(ct); var rows = new List<T>();
            while (await reader.ReadAsync(ct)) rows.Add(map(reader));
            return new(rows, total, page, size);
        }
        catch (Exception error) when (error is SqlException or InvalidOperationException or ArgumentException)
        {
            logger.LogWarning("Bank reference lookup unavailable. FailureType {FailureType}", error.GetType().Name); throw Unavailable();
        }
    }
    private static string Text(SqlDataReader reader, int index) => reader.IsDBNull(index) ? "" : reader.GetString(index).Trim();
    public Task<ReferencePage<BankCustomer>> Customers(string? search, int page, int pageSize, CancellationToken ct) =>
        Read(Settings.CustomerTable, [Settings.CustomerTaxColumn, Settings.CustomerMatbrojColumn, Settings.CustomerNameColumn], search, page, pageSize, false,
            reader => new BankCustomer(Text(reader, 0), Text(reader, 1), Text(reader, 2)), ct);
    public Task<ReferencePage<BankGroup>> Groups(string? search, int page, int pageSize, CancellationToken ct) =>
        Read(Settings.GccTable, [Settings.GccNumberColumn, Settings.GccNameColumn], search, page, pageSize, false,
            reader => new BankGroup(Text(reader, 0), Text(reader, 1)), ct);
    public async Task<BankGroup?> Group(string number, CancellationToken ct)
    {
        var result = await Read(Settings.GccTable, [Settings.GccNumberColumn, Settings.GccNameColumn], number, 1, 2, true,
            reader => new BankGroup(Text(reader, 0), Text(reader, 1)), ct);
        if (result.Total > 1) throw new BusinessRuleException(409, "bank_reference.ambiguous", "Referentni podatak zahtijeva provjeru izvora.");
        return result.Items.SingleOrDefault();
    }
}
