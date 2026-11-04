using RBBH.ConnectedParties.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController]
[Route("api/email-log")]
[Authorize(Policy = ApplicationPolicies.CapitalRead)]
public class EmailLogController(
    EmailLogStore store,
    IConfiguration configuration,
    IHostEnvironment? environment = null) : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromQuery] string? audience = null)
    {
        var english = Request.Headers.AcceptLanguage.ToString().StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var provider = configuration["Email:Provider"] ?? "demo";

        IReadOnlyList<RBBH.ConnectedParties.BL.Services.EmailLogEntry> emails = [];
        // Raw mail preview is an isolated Development facility, never a production inbox.
        if (environment?.IsDevelopment() == true && provider.Equals("demo", StringComparison.OrdinalIgnoreCase))
        {
            emails = string.IsNullOrWhiteSpace(audience)
                ? store.GetAll()
                : store.GetForAudience(audience);
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            emails = emails.Where(entry => !string.IsNullOrWhiteSpace(email) &&
                string.Equals(entry.To, email, StringComparison.OrdinalIgnoreCase)).ToList();
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
                    "broadcast" => english ? "Period lock/unlock notice to all users" : "Obavijest svim korisnicima o zaključavanju/otključavanju perioda",
                    _ => english ? "Business notification" : "Poslovna obavijest"
                },
                deliveryStatus = english ? "Recorded" : "Evidentirano",
                requestId = e.RequestId
            })
        });
    }
}
