using System.Globalization;

namespace RBBH.RelatedPartiesRegister.Web.Components;

/// <summary>Display formats copied from the reference product's rendering of the same values.</summary>
public static class AppFormat
{
    public static string Timestamp(DateTime value)
    {
        var local = (value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value).ToLocalTime();
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "bs"
            ? local.ToString("yyyy 'M'MM dd HH:mm", CultureInfo.InvariantCulture)
            : local.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>The reference shows the report date as the raw API timestamp.</summary>
    public static string RawTimestamp(DateTime value) => value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
}
