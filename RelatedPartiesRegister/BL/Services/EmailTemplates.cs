namespace RBBH.ConnectedParties.BL.Services;

internal static class EmailTemplates
{
    private static readonly string[] BosnianMonths =
    [
        "Januar","Februar","Mart","April","Maj","Juni",
        "Juli","August","Septembar","Oktobar","Novembar","Decembar"
    ];

    internal static string MonthDisplay(int year, int month) =>
        $"{BosnianMonths[month - 1]} {year}.";

    // ── Shared layout helpers ────────────────────────────────────────────────

    private static string Header() => """
        <!DOCTYPE html>
        <html lang="bs">
        <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#F2F2F2;font-family:Arial,Helvetica,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0">
            <tr>
              <td align="center" style="padding:40px 16px;">
                <table width="600" cellpadding="0" cellspacing="0"
                       style="background:#FFFFFF;border-radius:8px;overflow:hidden;
                              box-shadow:0 4px 16px rgba(0,0,0,.12);max-width:600px;width:100%;">
                  <tr>
                    <td style="background:#1A1A1A;padding:24px 32px;">
                      <span style="color:#FFD700;font-size:18px;font-weight:700;letter-spacing:.5px;">RAIFFEISEN BANK BiH</span>
                      <br><span style="color:#888888;font-size:11px;">Registar povezanih lica</span>
                    </td>
                  </tr>
                  <tr><td style="background:#FFD700;line-height:4px;height:4px;">&nbsp;</td></tr>
        """;

    private static string Footer() => """
                  <tr>
                    <td style="background:#F8F8F8;padding:16px 32px;border-top:1px solid #EEEEEE;text-align:center;">
                      <p style="color:#BBBBBB;font-size:11px;margin:0;line-height:1.6;">
                        Ova poruka je automatski generisana od strane Registra povezanih lica — Raiffeisen Bank BiH.<br>
                        Molimo ne odgovarajte na ovaj email.
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private static string ActionButton(string href, string label) => $"""
        <tr>
          <td style="padding:0 32px 36px;text-align:center;">
            <a href="{href}" target="_blank" rel="noopener noreferrer"
               style="display:inline-block;background:#FFD700;color:#1A1A1A;font-family:Arial,Helvetica,sans-serif;
                      font-size:14px;font-weight:700;padding:13px 32px;border-radius:6px;
                      text-decoration:none;letter-spacing:.2px;">
              {label} &#8594;
            </a>
          </td>
        </tr>
        """;

    private static string InfoTable(params (string label, string value, bool shaded)[] rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("""<table width="100%" cellpadding="0" cellspacing="0" style="font-size:14px;border-radius:6px;overflow:hidden;border:1px solid #EEEEEE;">""");
        foreach (var (label, value, shaded) in rows)
        {
            var bg = shaded ? "background:#F8F8F8;" : "";
            sb.Append($"""
                <tr>
                  <td style="{bg}padding:12px 16px;color:#888888;font-weight:600;width:140px;border-bottom:1px solid #EEEEEE;">{label}</td>
                  <td style="{bg}padding:12px 16px;color:#1A1A1A;border-bottom:1px solid #EEEEEE;">{value}</td>
                </tr>
                """);
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    // ── Templates ────────────────────────────────────────────────────────────

    /// <summary>Email adminu: korisnik podnio zahtjev za otključavanje. Uključuje action button.</summary>
    internal static string UnlockRequest(
        string requestedBy, int year, int month, string reason, string appBaseUrl)
    {
        var period    = MonthDisplay(year, month);
        var timestamp = DateTime.Now.ToString("dd.MM.yyyy. HH:mm");
        var adminLink = $"{appBaseUrl.TrimEnd('/')}/admin/period";

        return Header() + $"""
                  <tr>
                    <td style="padding:36px 32px 24px;">
                      <h1 style="color:#1A1A1A;font-size:18px;font-weight:700;margin:0 0 8px;">
                        Novi zahtjev za otključavanje perioda
                      </h1>
                      <p style="color:#555555;font-size:14px;line-height:1.7;margin:0 0 24px;">
                        Korisnik je podnio zahtjev za otključavanje perioda unosa u Registru povezanih lica.
                        Pregledajte zahtjev i odobrite ili odbijte u admin panelu.
                      </p>
                      {InfoTable(
                          ("Korisnik",       requestedBy, true),
                          ("Period",         period,      false),
                          ("Datum zahtjeva", timestamp,   true),
                          ("Razlog",         reason,      false)
                      )}
                    </td>
                  </tr>
            """ + ActionButton(adminLink, "Upravljanje periodom") + Footer();
    }

    /// <summary>Email korisniku: admin tražio više informacija. Sadrži adminovu poruku i action button.</summary>
    internal static string NeedsInfo(
        string requestedBy, int year, int month, string adminNote, string appBaseUrl)
    {
        var period    = MonthDisplay(year, month);
        var timestamp = DateTime.Now.ToString("dd.MM.yyyy. HH:mm");
        var appLink   = $"{appBaseUrl.TrimEnd('/')}/notifikacije";

        return Header() + $"""
                  <tr>
                    <td style="padding:36px 32px 24px;">
                      <h1 style="color:#1A1A1A;font-size:18px;font-weight:700;margin:0 0 8px;">
                        Administrator treba više informacija
                      </h1>
                      <p style="color:#555555;font-size:14px;line-height:1.7;margin:0 0 20px;">
                        Poštovani/a <strong>{requestedBy}</strong>, vaš zahtjev za otključavanje
                        perioda <strong>{period}</strong> je na čekanju.
                        Administrator Registra povezanih lica je postavio sljedeće pitanje ili napomenu:
                      </p>

                      <!-- Admin note blockquote -->
                      <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:20px;">
                        <tr>
                          <td width="4" style="background:#FFD700;border-radius:3px;">&nbsp;</td>
                          <td style="padding:14px 18px;background:#FFFBEA;border-radius:0 6px 6px 0;
                                     font-size:14px;color:#333333;line-height:1.7;font-style:italic;">
                            {adminNote}
                          </td>
                        </tr>
                      </table>

                      <p style="color:#555555;font-size:14px;line-height:1.7;margin:0 0 4px;">
                        Molimo vas da pristupite aplikaciji i pošaljite novi zahtjev s odgovorom
                        na navedeno pitanje.
                      </p>
                      <p style="color:#AAAAAA;font-size:12px;margin:0;">Datum: {timestamp}</p>
                    </td>
                  </tr>
            """ + ActionButton(appLink, "Odgovorite na zahtjev") + Footer();
    }

    /// <summary>Email adminu: korisnik odgovorio na zahtjev za informacijama.</summary>
    internal static string UserResponse(
        string requestedBy, int year, int month, string userMessage, string appBaseUrl)
    {
        var period    = MonthDisplay(year, month);
        var timestamp = DateTime.Now.ToString("dd.MM.yyyy. HH:mm");
        var adminLink = $"{appBaseUrl.TrimEnd('/')}/admin/period";

        return Header() + $"""
                  <tr>
                    <td style="padding:36px 32px 24px;">
                      <h1 style="color:#1A1A1A;font-size:18px;font-weight:700;margin:0 0 8px;">
                        Korisnik dostavio dodatne informacije
                      </h1>
                      <p style="color:#555555;font-size:14px;line-height:1.7;margin:0 0 24px;">
                        Korisnik <strong>{requestedBy}</strong> je odgovorio na vaš zahtjev za
                        dodatnim informacijama vezanim za period <strong>{period}</strong>.
                      </p>
                      {InfoTable(
                          ("Korisnik",        requestedBy, true),
                          ("Period",          period,      false),
                          ("Datum odgovora",  timestamp,   true)
                      )}
                      <!-- User response blockquote -->
                      <table width="100%" cellpadding="0" cellspacing="0" style="margin-top:20px;">
                        <tr>
                          <td width="4" style="background:#FFD700;border-radius:3px;">&nbsp;</td>
                          <td style="padding:14px 18px;background:#FFFBEA;border-radius:0 6px 6px 0;
                                     font-size:14px;color:#333333;line-height:1.7;">
                            <strong style="display:block;margin-bottom:6px;color:#888888;font-size:12px;text-transform:uppercase;letter-spacing:.3px;">Odgovor korisnika</strong>
                            {userMessage}
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
            """ + ActionButton(adminLink, "Upravljanje periodom") + Footer();
    }

    /// <summary>Email HR-u: novo fizičko lice dodano u registar.</summary>
    internal static string HrNewPhysicalPerson(
        string personName, string createdBy, string relationBasis,
        DateTime? dateFrom, DateTime? dateTo, string appBaseUrl)
    {
        var timestamp = DateTime.Now.ToString("dd.MM.yyyy. HH:mm");
        var appLink   = $"{appBaseUrl.TrimEnd('/')}/physical-persons";
        var dfStr     = dateFrom.HasValue ? dateFrom.Value.ToString("dd.MM.yyyy.") : "—";
        var dtStr     = dateTo.HasValue   ? dateTo.Value.ToString("dd.MM.yyyy.")   : "—";

        return Header() + $"""
                  <tr>
                    <td style="padding:36px 32px 24px;">
                      <h1 style="color:#1A1A1A;font-size:18px;font-weight:700;margin:0 0 8px;">
                        Novo povezano fizičko lice
                      </h1>
                      <p style="color:#555555;font-size:14px;line-height:1.7;margin:0 0 24px;">
                        U Registar povezanih lica dodano je novo fizičko lice.
                        Molimo provjerite podatke u aplikaciji.
                      </p>
                      {InfoTable(
                          ("Lice",             personName,    true),
                          ("Osnov",            relationBasis, false),
                          ("Datum od",         dfStr,         true),
                          ("Datum do",         dtStr,         false),
                          ("Unio/la",          createdBy,     true),
                          ("Datum unosa",      timestamp,     false)
                      )}
                    </td>
                  </tr>
            """ + ActionButton(appLink, "Pregledaj registar") + Footer();
    }

    /// <summary>Email HR-u: osnov povezanosti fizičkog lica je istekao ili postavljen na prošli datum.</summary>
    internal static string HrPhysicalPersonExpired(
        string personName, string updatedBy, string relationBasis,
        DateTime dateTo, string appBaseUrl)
    {
        var timestamp = DateTime.Now.ToString("dd.MM.yyyy. HH:mm");
        var appLink   = $"{appBaseUrl.TrimEnd('/')}/physical-persons";
        var dtStr     = dateTo.ToString("dd.MM.yyyy.");

        return Header() + $"""
                  <tr>
                    <td style="padding:36px 32px 24px;">
                      <h1 style="color:#1A1A1A;font-size:18px;font-weight:700;margin:0 0 8px;">
                        Istekao osnov povezanosti — fizičko lice
                      </h1>
                      <p style="color:#555555;font-size:14px;line-height:1.7;margin:0 0 24px;">
                        Osnov povezanosti za jedno fizičko lice u Registru je istekao ili je promijenjen
                        na datum koji je u prošlosti. Molimo pregledajte podatke.
                      </p>

                      <!-- Upozorenje -->
                      <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:20px;">
                        <tr>
                          <td width="4" style="background:#C0392B;border-radius:3px;">&nbsp;</td>
                          <td style="padding:14px 18px;background:#FDEDEC;border-radius:0 6px 6px 0;
                                     font-size:14px;color:#333333;line-height:1.7;">
                            Osnov povezanosti za lice <strong>{personName}</strong> istekao je
                            <strong>{dtStr}</strong>.
                          </td>
                        </tr>
                      </table>

                      {InfoTable(
                          ("Lice",         personName,    true),
                          ("Osnov",        relationBasis, false),
                          ("Istekao",      dtStr,         true),
                          ("Izmijenio/la", updatedBy,     false),
                          ("Datum izmjene",timestamp,     true)
                      )}
                    </td>
                  </tr>
            """ + ActionButton(appLink, "Pregledaj registar") + Footer();
    }

    /// <summary>Email korisniku: period otključan, zahtjev odobren.</summary>
    internal static string UnlockConfirmation(
        string requestedBy, int year, int month, string appBaseUrl)
    {
        var period    = MonthDisplay(year, month);
        var timestamp = DateTime.Now.ToString("dd.MM.yyyy. HH:mm");
        var appLink   = $"{appBaseUrl.TrimEnd('/')}/notifikacije";

        return Header() + $"""
                  <tr>
                    <td style="padding:36px 32px 24px;">
                      <h1 style="color:#1A1A1A;font-size:18px;font-weight:700;margin:0 0 8px;">
                        Period je otključan
                      </h1>
                      <p style="color:#555555;font-size:14px;line-height:1.7;margin:0 0 20px;">
                        Poštovani/a <strong>{requestedBy}</strong>, vaš zahtjev za otključavanje
                        perioda <strong>{period}</strong> je odobren.
                        Možete nastaviti s unosom podataka.
                      </p>
                      <p style="color:#AAAAAA;font-size:12px;margin:0;">Otključano: {timestamp}</p>
                    </td>
                  </tr>
            """ + ActionButton(appLink, "Unesite podatke") + Footer();
    }
}
