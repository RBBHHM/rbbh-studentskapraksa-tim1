using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.IoC.Middleware
{
    public class IncludeCorrelationIDMiddleware
    {
        private readonly RequestDelegate _next;

        public IncludeCorrelationIDMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var supplied = context.Request.Headers[AppConstants.CORRELATION_ID];
            // Untrusted headers also reach outbound requests and log enrichment.
            // Accept one bounded transport-safe ID; replace malformed/multiple values.
            if (supplied.Count != 1 || !IsValid(supplied[0]))
            {
                context.Request.Headers[AppConstants.CORRELATION_ID] = GenerateCorrelationID();
            }

            context.Response.Headers[AppConstants.CORRELATION_ID] = context.Request.Headers[AppConstants.CORRELATION_ID];

            var correlationId = context.Request.Headers[AppConstants.CORRELATION_ID].ToString();
            context.Response.OnStarting(() =>
            {
                // Exception handling clears response headers before writing its result.
                context.Response.Headers[AppConstants.CORRELATION_ID] = correlationId;
                return Task.CompletedTask;
            });
            await _next(context);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static string GenerateCorrelationID() => Guid.NewGuid().ToString();

        private static bool IsValid(string? value) =>
            value is { Length: > 0 and <= 64 } &&
            value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    }

}
