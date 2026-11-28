using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers.Utils;

namespace RBBH.ConnectedParties.BL.Services;

public sealed record BankCustomer(string TaxNumber, string Matbroj, string Name);
public sealed record BankGroup(string Number, string Name);
public sealed record ReferencePage<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public interface IBankReferenceSource
{
    bool IsConfigured { get; }
    Task<ReferencePage<BankCustomer>> Customers(string? search, int page, int pageSize, CancellationToken ct);
    Task<BankCustomer?> Customer(string identifier, bool byMatbroj, CancellationToken ct);
    Task<ReferencePage<BankGroup>> Groups(string? search, int page, int pageSize, CancellationToken ct);
    Task<BankGroup?> Group(string number, CancellationToken ct);
}

/// <summary>Parameterized read-only adapter; production must use a SELECT-only bank account.</summary>
public sealed class SqlBankReferenceSource(IOptions<BankReferenceOptions> options, IConfiguration configuration, ILogger<SqlBankReferenceSource> logger) : IBankReferenceSource
{
    private BankReferenceOptions Settings => options.Value;
    private bool HasCredentialOverride => !string.IsNullOrWhiteSpace(Settings.User) || !string.IsNullOrWhiteSpace(Settings.Password);

    public bool IsConfigured => Settings.Enabled &&
        !string.IsNullOrWhiteSpace(Settings.ServerName) &&
        !string.IsNullOrWhiteSpace(Settings.Database) &&
        (HasCredentialOverride
            ? !string.IsNullOrWhiteSpace(Settings.User) && !string.IsNullOrWhiteSpace(Settings.Password)
            : configuration.GetValue<bool>("Database:IntegratedSecurity") ||
              (!string.IsNullOrWhiteSpace(configuration["Database:User"]) &&
               !string.IsNullOrWhiteSpace(configuration["Database:Password"])));

    private static string Identifier(string value)
    {
        var parts = value.Split('.');
        if (parts.Length is < 1 or > 3 || parts.Any(part => !Regex.IsMatch(part, "^[A-Za-z_][A-Za-z0-9_]{0,127}$", RegexOptions.CultureInvariant)))
            throw Unavailable();
        return string.Join('.', parts.Select(part => "[" + part + "]"));
    }
    private static BusinessRuleException Unavailable() => new(503, "bank_reference.unavailable");
    private async Task<ReferencePage<T>> Read<T>(string table, string[] columns, string? search, int page, int size,
        int? exactColumn, Func<SqlDataReader, T> map, CancellationToken ct)
    {
        if (!IsConfigured) throw Unavailable();
        if (search?.Length > 100) throw new BusinessRuleException(400, "bank_reference.invalid_search");
        page = Math.Clamp(page, 1, 1_000_000); size = Math.Clamp(size, 1, 100);
        var source = Identifier(table); var fields = columns.Select(Identifier).ToArray();
        var projected = fields.Select(field => $"CONVERT(nvarchar(4000),{field})").ToArray();
        var normalized = exactColumn.HasValue
            ? projected.Select(field => $"COALESCE(LTRIM(RTRIM({field})), N'') COLLATE Latin1_General_100_BIN2").ToArray()
            : projected;
        var predicate = exactColumn is { } column
            ? $"{normalized[column]} = @term"
            : string.Join(" OR ", projected.Select(field => $"{field} COLLATE Latin1_General_100_CI_AS LIKE @term ESCAPE '~'"));
        var term = search?.Trim() ?? "";
        if (exactColumn is null) term = "%" + term.Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~[") + "%";
        try
        {
            var connectionString = ConnectionHelper.BuildConnection(configuration, Settings.ServerName, Settings.Database,
                HasCredentialOverride ? (Settings.User!, Settings.Password!) : null);
            var connectionOptions = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 5 };
            await using var connection = new SqlConnection(connectionOptions.ConnectionString); await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand(); command.CommandTimeout = 10;
            var projection = string.Join(',', normalized);
            if (exactColumn.HasValue)
            {
                // Matching rows are ambiguous only when their mapped values differ.
                command.CommandText = $"SELECT DISTINCT TOP (2) {projection} FROM {source} WHERE {predicate};";
            }
            else
            {
                command.CommandText = $"SELECT COUNT(*) FROM {source} WHERE {predicate}; SELECT {projection} FROM {source} WHERE {predicate} ORDER BY {projection} OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY;";
                command.Parameters.Add("@offset", SqlDbType.Int).Value = (page - 1) * size;
                command.Parameters.Add("@take", SqlDbType.Int).Value = size;
            }
            command.Parameters.Add("@term", SqlDbType.NVarChar, 500).Value = term;
            await using var reader = await command.ExecuteReaderAsync(ct);
            int? total = null;
            if (!exactColumn.HasValue)
            {
                await reader.ReadAsync(ct);
                total = reader.GetInt32(0);
                await reader.NextResultAsync(ct);
            }
            var rows = new List<T>();
            while (await reader.ReadAsync(ct)) rows.Add(map(reader));
            return new(rows, total ?? rows.Count, page, size);
        }
        catch (Exception error) when (error is SqlException or InvalidOperationException or ArgumentException)
        {
            logger.LogWarning("Bank reference lookup unavailable. FailureType {FailureType}", error.GetType().Name); throw Unavailable();
        }
    }
    private static string Text(SqlDataReader reader, int index) => reader.IsDBNull(index) ? "" : reader.GetString(index).Trim();
    private Task<ReferencePage<BankCustomer>> ReadCustomers(string? search, int page, int pageSize, int? exactColumn, CancellationToken ct)
    {
        var mapping = Settings.Customers;
        return Read(mapping.Table, [mapping.Columns.TaxNumber, mapping.Columns.RegistrationNumber, mapping.Columns.Name],
            search, page, pageSize, exactColumn, reader => new BankCustomer(Text(reader, 0), Text(reader, 1), Text(reader, 2)), ct);
    }

    public Task<ReferencePage<BankCustomer>> Customers(string? search, int page, int pageSize, CancellationToken ct) =>
        ReadCustomers(search, page, pageSize, null, ct);

    public async Task<BankCustomer?> Customer(string identifier, bool byMatbroj, CancellationToken ct) =>
        SingleMatch(await ReadCustomers(identifier, 1, 2, byMatbroj ? 1 : 0, ct));

    private Task<ReferencePage<BankGroup>> ReadGroups(string? search, int page, int pageSize, int? exactColumn, CancellationToken ct)
    {
        var mapping = Settings.Gcc;
        return Read(mapping.Table, [mapping.Columns.Number, mapping.Columns.Name], search, page, pageSize, exactColumn,
            reader => new BankGroup(Text(reader, 0), Text(reader, 1)), ct);
    }

    public Task<ReferencePage<BankGroup>> Groups(string? search, int page, int pageSize, CancellationToken ct) =>
        ReadGroups(search, page, pageSize, null, ct);

    public async Task<BankGroup?> Group(string number, CancellationToken ct) =>
        SingleMatch(await ReadGroups(number, 1, 2, 0, ct));

    private static T? SingleMatch<T>(ReferencePage<T> result)
    {
        if (result.Items.Count > 1) throw new BusinessRuleException(409, "bank_reference.ambiguous");
        return result.Items.SingleOrDefault();
    }
}
