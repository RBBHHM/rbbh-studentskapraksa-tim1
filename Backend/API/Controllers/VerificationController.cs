using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.LegalEntity;
using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.Helpers.Constants;
using System.Security.Claims;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// API za verifikaciju povezanih fizičkih i pravnih lica.
/// </summary>
[RBBH.ConnectedParties.Helpers.RequireActorEmail]
[ApiController]
[Route("api/verification")]
[Authorize]
[Produces("application/json")]
public class VerificationController : BaseResuItController
{
    private readonly IRelatedPersonService _relatedPersonService;
    private readonly ILegalEntityService _legalEntityService;
    private readonly IAuditService _auditService;

    public VerificationController(
        IRelatedPersonService relatedPersonService,
        ILegalEntityService legalEntityService,
        IAuditService auditService)
    {
        _relatedPersonService = relatedPersonService;
        _legalEntityService = legalEntityService;
        _auditService = auditService;
    }

    /// <summary>
    /// Verifikuje povezano fizičko lice.
    /// </summary>
    [HttpPost("physical-person/{id:guid}")]
    [Authorize(Policy = ApplicationPolicies.PhysicalPersonsEdit)]
    [ProducesResponseType(typeof(RelatedPersonResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RelatedPersonResponseDTO>> VerifyPhysicalPerson([FromRoute] Guid id)
    {
        var korisnik = CurrentUser();
        var result = await _relatedPersonService.Verify(id, korisnik);

        return HandleResult(result);
    }

    /// <summary>
    /// Verifikuje povezano pravno lice.
    /// </summary>
    [HttpPost("legal-person/{id:guid}")]
    [Authorize(Policy = ApplicationPolicies.LegalPersonsEdit)]
    [ProducesResponseType(typeof(LegalEntityDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LegalEntityDTO>> VerifyLegalPerson([FromRoute] Guid id)
    {
        var korisnik = CurrentUser();
        var verified = await _legalEntityService.VerifyAsync(id, korisnik);

        return Ok(verified);
    }

    private string CurrentUser()
    {
        return RBBH.ConnectedParties.Helpers.AuthenticatedActor.Email(User);
    }
}
