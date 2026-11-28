using System.Net;

namespace RBBH.ConnectedParties.BL.Services;

internal static class RoleRequestMail
{
    public static string Build(string status, string title, string encodedDetails, string? link, string? origin, string language, string? logoUrl = null)
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
        if (status == "hr")
            (badge, action) = language switch
            {
                "EN" => ("HR notice", "Open the register"),
                "DE" => ("HR-Mitteilung", "Register öffnen"),
                _ => ("HR obavijest", "Otvori registar")
            };
        var badgeBackground = status switch { "approved" => "#e3f4e9", "rejected" => "#fce8e8", _ => "#fff2bd" };
        const string font = "Amalia, 'Segoe UI', Arial, sans-serif";
        var assets = origin is null ? null : WebUtility.HtmlEncode(origin + "/_content/RBI.DesignSystem/assets");
        var webFonts = assets is null ? "" : $$"""
            <style type="text/css">
              @font-face { font-family:Amalia; font-style:normal; font-weight:400; mso-font-alt:'Segoe UI';
                src:url('{{assets}}/fonts/Amalia-Regular.woff2') format('woff2'); }
              @font-face { font-family:Amalia; font-style:normal; font-weight:700; mso-font-alt:'Segoe UI';
                src:url('{{assets}}/fonts/Amalia-Bold.woff2') format('woff2'); }
            </style>
            """;
        var logo = logoUrl is null ? (assets is null ? null : assets + "/logos/rbi-bank-mono-pos.png") : WebUtility.HtmlEncode(logoUrl);
        var brand = logo is null ? "" :
            $"""<img src="{logo}" width="145" alt="" style="display:block;width:145px;height:auto;margin:0 auto 10px;border:0">""";
        var button = link is null ? "" : $"""
            <table role="presentation" align="center" cellpadding="0" cellspacing="0" style="margin:30px auto 0">
              <tr><td bgcolor="#fee600" style="border-radius:10px;mso-padding-alt:14px 28px">
                <a href="{WebUtility.HtmlEncode(link)}" style="display:inline-block;padding:14px 28px;color:#181a1c;font:700 16px {font};text-decoration:none">{WebUtility.HtmlEncode(action)}</a>
              </td></tr>
            </table>
            """;
        // Outlook uses table alignment and inline styles. Other clients can also use the hosted web font.
        return $"""
            {webFonts}
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" bgcolor="#f3f4f5" style="font-family:{font};color:#181a1c">
              <tr><td align="center" style="padding:32px 14px">
                <table role="presentation" align="center" width="100%" cellpadding="0" cellspacing="0" bgcolor="#ffffff" style="max-width:560px;border:1px solid #e4e6e8;border-radius:18px;overflow:hidden">
                  <tr><td bgcolor="#fee600" height="5" style="font-size:1px;line-height:5px">&nbsp;</td></tr>
                  <tr><td align="center" style="padding:30px 24px 23px;font:700 13px {font};color:#4d525a">
                    {brand}Raiffeisen Bank BiH
                  </td></tr>
                  <tr><td align="center" style="padding:0 24px 34px;font-family:{font}">
                    <span style="display:inline-block;background:{badgeBackground};padding:7px 14px;border-radius:24px;font-size:13px;font-weight:700">{WebUtility.HtmlEncode(badge)}</span>
                    <h1 style="margin:14px 0 22px;font:700 28px/1.2 {font};letter-spacing:-.02em">{WebUtility.HtmlEncode(title)}</h1>
                    <table role="presentation" align="center" width="100%" cellpadding="0" cellspacing="0" style="max-width:428px">
                      <tr><td align="center" bgcolor="#f5f6f7" style="padding:18px 20px;border-radius:12px;font:400 16px/1.6 {font};word-wrap:break-word">{encodedDetails}</td></tr>
                    </table>
                    {button}
                  </td></tr>
                  <tr><td align="center" bgcolor="#fafafa" style="padding:18px 24px;border-top:1px solid #edf0f1;font:400 13px/1.55 {font};color:#505663">
                    <strong>{WebUtility.HtmlEncode(product)}</strong><br>{WebUtility.HtmlEncode(footer)}
                  </td></tr>
                </table>
              </td></tr>
            </table>
            """;
    }
}
