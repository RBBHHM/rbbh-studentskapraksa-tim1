using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.Exceptions;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController, Route("api/bank-reference"), Authorize(Policy = ApplicationPolicies.LegalPersonsRead)]
public sealed class BankReferenceController(IBankReferenceSource source) : ControllerBase
{
    [HttpGet("customers")]
    public async Task<ActionResult<ReferencePage<BankCustomer>>> Customers(CancellationToken ct, string? search = null, int page = 1, int pageSize = 20)
    {
        var term = search?.Trim() ?? "";
        if (term.Length == 0)
            return Ok(new ReferencePage<BankCustomer>([], 0, Math.Clamp(page, 1, 1_000_000), Math.Clamp(pageSize, 1, 100)));
        if (!source.IsConfigured)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                extensions: new Dictionary<string, object?> { ["code"] = "bank_reference.unavailable" });
        try
        {
            return Ok(await source.Customers(term, page, pageSize, ct));
        }
        catch (BusinessRuleException exception)
        {
            return Problem(statusCode: exception.Status,
                extensions: new Dictionary<string, object?> { ["code"] = exception.Code });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
    }
    [HttpGet("gcc")]
    public Task<ReferencePage<BankGroup>> Groups(CancellationToken ct, string? search = null, int page = 1, int pageSize = 20) => source.Groups(search, page, pageSize, ct);
}
