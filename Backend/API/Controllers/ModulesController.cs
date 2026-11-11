using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// Lists modules from the effective application permissions, not role names.
/// Roles and grants are managed in the database; provider-supplied roles never
/// enable a module. The same permission codes protect the API endpoints.
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize(Policy = ApplicationPolicies.ApplicationAccess)]
[Produces("application/json")]
public sealed class ModulesController : ControllerBase
{
    private static readonly ModuleDefinition[] Modules =
    [
        new("physical-persons", "Fizička lica", "/app/physical-persons", "person",
            ApplicationPermissions.PhysicalPersonsView),
        new("legal-persons", "Pravna lica", "/app/legal-persons", "business",
            ApplicationPermissions.LegalPersonsView),
        new("limits", "Limiti", "/app/limits", "balance",
            ApplicationPermissions.LimitsView),
        new("regulatory-reporting", "Regulatorna izvještavanja", "/app/reports", "chart",
            ApplicationPermissions.RegulatoryReportingView)
    ];

    [HttpGet("modules")]
    [ProducesResponseType(typeof(ModulesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GetModules()
    {
        // Authorization uses exact, database-owned permission codes. A role
        // name or a case-altered permission is not sufficient.
        var permissions = User.FindAll("permission")
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        var modules = Modules.Select(module => new ModuleDto(
            module.Key,
            module.Name,
            module.Route,
            module.Icon,
            permissions.Contains(module.RequiredPermission)
        )).ToList();

        return Ok(new ModulesResponse(modules));
    }
}

public sealed record ModulesResponse(List<ModuleDto> Modules);

public sealed record ModuleDto(string Key, string Name, string Route, string Icon, bool IsEnabled);

internal sealed record ModuleDefinition(string Key, string Name, string Route, string Icon, string RequiredPermission);
