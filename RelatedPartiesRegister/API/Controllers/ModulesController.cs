// File: src/RBBH.ConnectedParties/API/Controllers/ModulesController.cs
// PL-19: Kreirati API endpoint koji vraća listu modula s isEnabled flagom prema roli korisnika
//
// GET /api/auth/modules
// Response prema API kontraktu:
// {
//   "modules": [
//     { "key": "connected-parties", "name": "Povezana lica", "route": "/connected-parties",
//       "icon": "people", "isEnabled": true },
//     ...
//   ]
// }

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RBBH.ConnectedParties.API.Controllers
{
    /// <summary>
    /// Endpoint za dohvat liste modula aplikacije s flagom dostupnosti prema roli korisnika.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Authorize]
    [Produces("application/json")]
    public class ModulesController : ControllerBase
    {
        // Mapiranje rola na dostupne module.
        // Ključ = naziv modula, Vrijednost = skup rola koje imaju pristup.
        private static readonly Dictionary<string, HashSet<string>> _moduleRoles = new()
        {
            ["physical-persons"] = new(StringComparer.OrdinalIgnoreCase) { "physical-persons" },
            ["legal-persons"] = new(StringComparer.OrdinalIgnoreCase) { "legal-persons" },
            ["limits"] = new(StringComparer.OrdinalIgnoreCase) { "limits" },
            ["regulatory-reporting"] = new(StringComparer.OrdinalIgnoreCase) { "regulatory-reporting" },
        };

        // Statička lista svih modula s metapodacima
        private static readonly List<ModuleDefinition> _allModules = new()
        {
            new("physical-persons", "Fizička lica", "/app/physical-persons", "person"),
            new("legal-persons", "Pravna lica", "/app/legal-persons", "business"),
            new("limits", "Limiti", "/app/limits", "balance"),
            new("regulatory-reporting", "Regulatorna izvještavanja", "/app/reports", "chart"),
        };

        /// <summary>
        /// Vraća listu svih modula s flagom isEnabled prema roli prijavljenog korisnika.
        /// isEnabled: false znači da modul ne treba biti prikazan u navigaciji.
        /// </summary>
        /// <returns>Lista modula s isEnabled flagom.</returns>
        /// <response code="200">Lista modula uspješno vraćena.</response>
        /// <response code="401">Korisnik nije prijavljen ili token je istekao.</response>
        [HttpGet("modules")]
        [ProducesResponseType(typeof(ModulesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetModules()
        {
            var userRoles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet();

            var modules = _allModules.Select(m => new ModuleDto(
                Key: m.Key,
                Name: m.Name,
                Route: m.Route,
                Icon: m.Icon,
                IsEnabled: _moduleRoles.TryGetValue(m.Key, out var allowedRoles)
                           && allowedRoles.Overlaps(userRoles)
            )).ToList();

            return Ok(new ModulesResponse(modules));
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    /// <summary>Response wrapper za listu modula.</summary>
    public record ModulesResponse(List<ModuleDto> Modules);

    /// <summary>Podaci o jednom modulu aplikacije.</summary>
    public record ModuleDto(
        string Key,
        string Name,
        string Route,
        string Icon,
        bool IsEnabled);

    /// <summary>Interna definicija modula (statički metapodaci).</summary>
    internal record ModuleDefinition(string Key, string Name, string Route, string Icon);
}
