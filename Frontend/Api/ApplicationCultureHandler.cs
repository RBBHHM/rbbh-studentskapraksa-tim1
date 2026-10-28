using System.Globalization;

namespace RBBH.RelatedPartiesRegister.Web.Api;

// Read culture at send time, not when the pooled handler is constructed.
// No user, token or scoped identity is captured in this handler.
public sealed class ApplicationCultureHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        request.Headers.AcceptLanguage.Clear();
        request.Headers.AcceptLanguage.ParseAdd(string.IsNullOrEmpty(culture) ? "bs" : culture);
        return base.SendAsync(request, cancellationToken);
    }
}
