using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.DL.Persistence;

public partial class ConnectedPartiesDbContext
{
    private static readonly Dictionary<string, string> ColumnAliases = new(StringComparer.Ordinal)
    {
        ["AppUsers.KeycloakSubjectConfirmed"] = "LOGIN_CONFIRMED",
        ["AuditLogs.Username"] = "USER",
        ["Capitals.CapitalDate"] = "VALID_FROM",
        ["Limits.TipLimita"] = "LIMIT_TYPE",
        ["Limits.OdobreniLimit"] = "APPROVED_LIMIT",
        ["Limits.RokOcekivaneUtilizacije"] = "EXPECTED_UTILIZATION_DATE",
        ["Limits.MaksimalnaOcekivanaUtilizacija"] = "MAXIMUM_EXPECTED_UTILIZATION",
        ["Limits.Komentar"] = "COMMENT"
    };

    private static readonly Dictionary<string, string[]> ColumnOrder = new(StringComparer.Ordinal)
    {
        ["AppUsers"] = ["ID", "KEYCLOAK_ID", "USERNAME", "FIRST_NAME", "LAST_NAME", "EMAIL", "IS_ACTIVE",
            "LOGIN_CONFIRMED", "CREATED_AT", "CREATED_BY", "MODIFIED_AT", "MODIFIED_BY"],
        ["AuditLogs"] = ["ID", "ACTION", "USER", "TABLE_NAME", "OLD_VALUES", "NEW_VALUES", "TIMESTAMP"],
        ["Capitals"] = ["ID", "IS_ACTIVE", "VALID_TILL", "REGULATORY_CAPITAL", "CORE_CAPITAL", "SUPPLEMENTARY_CAPITAL",
            "CREATED_BY", "CREATED_AT", "MODIFIED_BY", "MODIFIED_AT", "VALID_FROM"],
        ["Limits"] = ["ID", "LIMIT_TYPE", "APPROVED_LIMIT", "CAPITAL_ID", "LEGAL_ENTITY_ID",
            "EXPECTED_UTILIZATION_DATE", "MAXIMUM_EXPECTED_UTILIZATION", "COMMENT", "CREATED_BY", "CREATED_AT",
            "MODIFIED_BY", "MODIFIED_AT"]
    };

    private static string SqlName(string name)
    {
        var acronymBoundaries = Regex.Replace(name, "([A-Z])([A-Z][a-z])", "$1_$2");
        return Regex.Replace(acronymBoundaries, "([a-z0-9])([A-Z])", "$1_$2").ToUpperInvariant();
    }

    private static void ApplyDatabaseNames(ModelBuilder builder)
    {
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is null) continue;

            foreach (var property in entity.GetDeclaredProperties())
            {
                var current = property.FindAnnotation("Relational:ColumnName")?.Value as string ?? property.Name;
                var column = ColumnAliases.TryGetValue($"{table}.{current}", out var alias) ? alias : SqlName(current);
                property.SetColumnName(column);
                if (ColumnOrder.TryGetValue(table, out var order) && Array.IndexOf(order, column) is var index && index >= 0)
                    property.SetColumnOrder(index);
            }

            var sqlTable = table == "CodeListDefinitions" ? "CODELIST_DEFINITIONS" : SqlName(table);
            if (table != sqlTable) entity.SetTableName(sqlTable);
        }
    }
}
