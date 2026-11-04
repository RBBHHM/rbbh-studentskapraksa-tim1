using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController, Route("api/localization"), Authorize(Policy = ApplicationPolicies.ApplicationAccess)]
public sealed class LocalizationController(RuntimeLocalizationCatalog catalog) : ControllerBase
{
    [HttpGet("languages")]
    public Task<object> Languages(CancellationToken ct) => catalog.Languages(ct);

    [HttpGet("resources/{language}")]
    public async Task<RuntimeResourcePack> Resources(string language, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-cache";
        return await catalog.Resources(language, ct);
    }

    [HttpPut("resources/{language}/{resourceKey}"), Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public async Task<IActionResult> Put(string language, string resourceKey, PutRuntimeResource input, CancellationToken ct)
    {
        var actor = User.FindFirst("preferred_username")?.Value ?? AuthenticatedActor.RequireSubject(User);
        await catalog.Put(language, resourceKey, input, actor, ct);
        return NoContent();
    }
}
