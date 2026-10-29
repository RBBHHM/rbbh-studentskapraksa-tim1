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
            // Check if the "CorrelationID" header already exists
            if (!context.Request.Headers.ContainsKey(AppConstants.CORRELATION_ID))
            {
                // Add the header if it doesn't exist
                context.Request.Headers.Append(AppConstants.CORRELATION_ID, GenerateCorrelationID());
            }

            await _next(context); // Call the next middleware
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static string GenerateCorrelationID() => Guid.NewGuid().ToString();
    }

}
