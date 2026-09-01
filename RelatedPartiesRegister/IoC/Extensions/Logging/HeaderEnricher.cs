using RBBH.ConnectedParties.Helpers.Constants;
using Serilog.Core;
using Serilog.Events;

namespace RBBH.ConnectedParties.IoC.Extensions.Logging
{
    public class HeaderEnricher : ILogEventEnricher
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HeaderEnricher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var headers = _httpContextAccessor.HttpContext?.Request?.Headers;
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (header.Key.Equals(AppConstants.CORRELATION_ID))
                    {
                        // Add X-Correlation-ID into the Log Console. This is triggered for each log.
                        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty($"{header.Key}", header.Value.ToString()));
                    }

                }
            }
        }
    }
}
