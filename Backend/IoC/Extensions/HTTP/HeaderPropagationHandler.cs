using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.IoC.Extensions.HTTP
{
    public class HeaderPropagationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null)
            {
                if (httpContext.Request.Headers.ContainsKey(AppConstants.CORRELATION_ID))
                {
                    request.Headers.Add(
                        AppConstants.CORRELATION_ID, 
                        httpContext.Request.Headers[AppConstants.CORRELATION_ID].ToArray());
                }

            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
