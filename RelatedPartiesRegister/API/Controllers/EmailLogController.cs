using RBBH.ConnectedParties.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController]
[Route("api/email-log")]
[Authorize(Roles = "regulatory-reporting")]
public class EmailLogController(
    EmailLogStore store,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromQuery] string? audience = null)
    {
        var english = Request.Headers.AcceptLanguage.ToString().StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var provider = configuration["Email:Provider"] ?? "demo";

        IReadOnlyList<RBBH.ConnectedParties.BL.Services.EmailLogEntry> emails = [];
        if (!provider.Equals("smtp", StringComparison.OrdinalIgnoreCase))
        {
            emails = string.IsNullOrWhiteSpace(audience)
                ? store.GetAll()
                : store.GetForAudience(audience);
        }

        return Ok(new
        {
            provider,
            emails = emails.Select(e => new
            {
                id        = e.Id,
                to        = e.To,
                subject   = e.Subject,
                htmlBody  = e.HtmlBody,
                sentAt    = e.SentAt,
                audience  = e.Audience,
                purpose   = e.Audience switch
                {
                    "administrators" => english ? "Administrator notice about a request or period change" : "Obavijest administratorima o zahtjevu ili promjeni perioda",
                    "requester" => english ? "Response to the request submitter" : "Povratna informacija podnosiocu zahtjeva",
                    _ => english ? "Business notification" : "Poslovna obavijest"
                },
                deliveryStatus = english ? "Recorded" : "Evidentirano",
                requestId = e.RequestId
            })
        });
    }
}
