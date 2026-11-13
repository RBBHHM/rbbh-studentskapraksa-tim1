using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>Authenticated, read-only release notes from the project's public GitHub releases.</summary>
[ApiController]
[Route("api/release-notes")]
[Authorize(Policy = ApplicationPolicies.ApplicationAccess)]
public sealed class ReleaseNotesController(GitHubReleaseNotesService releases) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ReleaseNotesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get([FromQuery] string environment = "uat", CancellationToken ct = default)
    {
        var normalized = environment.Trim().ToLowerInvariant();
        if (normalized is not ("uat" or "prod"))
            return BadRequest(new { code = "release_notes.invalid_environment" });

        return Ok(await releases.GetAsync(normalized, ct));
    }
}
