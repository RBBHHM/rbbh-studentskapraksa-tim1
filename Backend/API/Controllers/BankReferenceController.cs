using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController, Route("api/bank-reference"), Authorize(Policy = ApplicationPolicies.LegalPersonsRead)]
public sealed class BankReferenceController(IBankReferenceSource source) : ControllerBase
{
    [HttpGet("customers")]
    public Task<ReferencePage<BankCustomer>> Customers(CancellationToken ct, string? search = null, int page = 1, int pageSize = 20) => source.Customers(search, page, pageSize, ct);
    [HttpGet("gcc")]
    public Task<ReferencePage<BankGroup>> Groups(CancellationToken ct, string? search = null, int page = 1, int pageSize = 20) => source.Groups(search, page, pageSize, ct);
}
