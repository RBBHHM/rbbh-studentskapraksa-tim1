using System.Net;

namespace RBBH.ConnectedParties.BL.Services;

internal static class RoleRequestMail
{
    public static string Build(string status, string title, string encodedDetails, string? link, string? logo, string language)
    {
        var (badge, action, product, footer) = language switch
        {
            "EN" => (status switch { "approved" => "Approved", "rejected" => "Rejected", _ => "Awaiting review" },
                status == "submitted" ? "Review request" : "View decision", "Related Parties Register", "Automatic message from the Related Parties Register."),
            "DE" => (status switch { "approved" => "Genehmigt", "rejected" => "Abgelehnt", _ => "In Prüfung" },
                status == "submitted" ? "Antrag prüfen" : "Entscheidung ansehen", "Register verbundener Personen", "Automatische Nachricht aus dem Register verbundener Personen."),
            _ => (status switch { "approved" => "Odobreno", "rejected" => "Odbijeno", _ => "Čeka odluku" },
                status == "submitted" ? "Pregledaj zahtjev" : "Pogledaj odluku", "Registar povezanih lica", "Automatska obavijest iz Registra povezanih lica.")
        };
        var badgeBackground = status switch { "approved" => "#e3f4e9", "rejected" => "#fce8e8", _ => "#fff2bd" };
        var brand = logo is null ? "" :
            $"""<img src="{WebUtility.HtmlEncode(logo)}" width="136" alt="" style="display:block;width:136px;height:auto;margin-bottom:9px;border:0">""";
        var button = link is null ? "" : $"""
            <table role="presentation" cellpadding="0" cellspacing="0" style="margin-top:24px">
              <tr><td bgcolor="#fee600" style="border-radius:8px;mso-padding-alt:13px 22px">
                <a href="{WebUtility.HtmlEncode(link)}" style="display:inline-block;padding:13px 22px;color:#202020;font:700 15px Arial,sans-serif;text-decoration:none">{WebUtility.HtmlEncode(action)}</a>
              </td></tr>
            </table>
            """;
        // Inline styles and presentation tables keep the layout usable in desktop Outlook.
        return $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="#f3f4f6" style="font-family:Arial,sans-serif;color:#202020">
              <tr><td align="center" style="padding:26px 12px">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="#ffffff" style="max-width:560px;border:1px solid #e3e5e8;border-radius:14px">
                  <tr><td bgcolor="#fee600" height="6" style="font-size:1px;line-height:6px">&nbsp;</td></tr>
                  <tr><td style="padding:24px 28px 14px;font-size:13px;font-weight:bold;color:#333842">
                    {brand}Raiffeisen Bank BiH
                  </td></tr>
                  <tr><td style="padding:0 28px 28px">
                    <span style="display:inline-block;background:{badgeBackground};padding:6px 10px;border-radius:20px;font-size:12px;font-weight:bold">{WebUtility.HtmlEncode(badge)}</span>
                    <h1 style="margin:14px 0 18px;font-size:25px;line-height:1.2">{WebUtility.HtmlEncode(title)}</h1>
                    <div style="padding:15px 17px;background:#f5f6f8;border-radius:8px;font-size:15px;line-height:1.65;word-wrap:break-word">{encodedDetails}</div>
                    {button}
                  </td></tr>
                  <tr><td style="padding:15px 28px;border-top:1px solid #e3e5e8;font-size:12px;line-height:1.5;color:#505663">
                    <strong>{WebUtility.HtmlEncode(product)}</strong><br>{WebUtility.HtmlEncode(footer)}
                  </td></tr>
                </table>
              </td></tr>
            </table>
            """;
    }
}
