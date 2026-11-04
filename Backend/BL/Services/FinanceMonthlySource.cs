using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.Exceptions;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class FinanceMonthlyOptions
{
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "";
    // Exact approved period->table names; never guess a date suffix from user input.
    public Dictionary<string, string> MonthlyTables { get; set; } = new();
    public string CustomerKeyColumn { get; set; } = "";
    public string BalanceColumn { get; set; } = "Bilans_exponiranost_bez_Ulaganja_sredstava_poreza";
    public string OffBalanceColumn { get; set; } = "VB_Ukupno";
    // Source currency/conversion must be confirmed; the existing global capital model is BAM-only.
    public string Currency { get; set; } = "";
}
public sealed record FinanceExposure(string CustomerKey, decimal BalanceExposure, decimal OffBalanceExposure)
{
    public decimal TotalExposure => checked(BalanceExposure + OffBalanceExposure);
}
public interface IFinanceMonthlySource
{
    bool Enabled { get; }
    Task<IReadOnlyList<FinanceExposure>> Read(int year, int month, IReadOnlyCollection<string> customers, CancellationToken ct);
}

/// <summary>Only the two PDF-documented components are mapped. Missing/ambiguous data is never treated as zero.</summary>
public sealed class SqlFinanceMonthlySource(IOptions<FinanceMonthlyOptions> options, ILogger<SqlFinanceMonthlySource> logger) : IFinanceMonthlySource
{
    private FinanceMonthlyOptions Settings => options.Value;
    public bool Enabled => Settings.Enabled;
    private static BusinessRuleException Unavailable() => new(503, "finance.source_unavailable");
    private static string Identifier(string value)
    {
        var parts = value.Split('.');
        if (parts.Length is < 1 or > 3 || parts.Any(part => !Regex.IsMatch(part, "^[A-Za-z_][A-Za-z0-9_]{0,127}$", RegexOptions.CultureInvariant))) throw Unavailable();
        return string.Join('.', parts.Select(part => "[" + part + "]"));
    }
    public async Task<IReadOnlyList<FinanceExposure>> Read(int year, int month, IReadOnlyCollection<string> customers, CancellationToken ct)
    {
        if (year is < 2000 or > 9999 || month is < 1 or > 12) throw new BusinessRuleException(400, "report.invalid_period");
        if (!Enabled || string.IsNullOrWhiteSpace(Settings.ConnectionString) || Settings.Currency != "BAM" ||
            !Settings.MonthlyTables.TryGetValue($"{year:D4}-{month:D2}", out var configuredTable)) throw Unavailable();
        var table = Identifier(configuredTable); var key = Identifier(Settings.CustomerKeyColumn);
        var balance = Identifier(Settings.BalanceColumn); var offBalance = Identifier(Settings.OffBalanceColumn);
        if (customers.Count > 20_000 || customers.Any(customer => string.IsNullOrWhiteSpace(customer) || customer.Length > 50 || customer.Any(character => character is < '0' or > '9')))
            throw Unavailable();
        var rows = new Dictionary<string, FinanceExposure>(StringComparer.Ordinal); var keys = customers.Distinct(StringComparer.Ordinal).ToArray();
        try
        {
            var connectionOptions = new SqlConnectionStringBuilder(Settings.ConnectionString) { ConnectTimeout = 5 };
            await using var connection = new SqlConnection(connectionOptions.ConnectionString); await connection.OpenAsync(ct);
            foreach (var batch in keys.Chunk(500))
            {
                await using var command = connection.CreateCommand(); command.CommandTimeout = 15;
                var names = batch.Select((_, index) => "@customer" + index).ToArray();
                command.CommandText = $"SELECT CONVERT(nvarchar(50),{key}),{balance},{offBalance} FROM {table} WHERE CONVERT(nvarchar(50),{key}) IN ({string.Join(',', names)});";
                for (var index = 0; index < batch.Length; index++) command.Parameters.Add(names[index], SqlDbType.NVarChar, 50).Value = batch[index];
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2)) throw Unavailable();
                    var customer = reader.GetString(0).Trim();
                    var row = new FinanceExposure(customer, reader.GetDecimal(1), reader.GetDecimal(2));
                    if (decimal.Round(row.BalanceExposure, 2) != row.BalanceExposure || decimal.Round(row.OffBalanceExposure, 2) != row.OffBalanceExposure)
                        throw Unavailable(); // No unapproved rounding policy for bank source values.
                    _ = row.TotalExposure;
                    if (!rows.TryAdd(customer, row)) throw new BusinessRuleException(409, "finance.ambiguous_customer");
                }
            }
            if (keys.Any(customer => !rows.ContainsKey(customer))) throw Unavailable();
            return keys.Select(customer => rows[customer]).ToArray();
        }
        catch (Exception error) when (error is SqlException or InvalidOperationException or ArgumentException or InvalidCastException or OverflowException)
        {
            logger.LogWarning("Finance monthly source unavailable. FailureType {FailureType}", error.GetType().Name); throw Unavailable();
        }
    }
}
