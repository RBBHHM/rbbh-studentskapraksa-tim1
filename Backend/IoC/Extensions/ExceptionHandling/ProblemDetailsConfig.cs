using RBBH.ConnectedParties.Helpers.Constants;
using Microsoft.AspNetCore.Diagnostics;

namespace RBBH.ConnectedParties.IoC.Extensions.ExceptionHandling
{
    public static class ProblemDetailsConfig
    {
        public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services, IWebHostEnvironment env)
        {
            services.AddProblemDetails(options =>
                options.CustomizeProblemDetails = context =>
                {
                    SetInstance(context);
                    AddCustomExtensions(context, env);
                });

            return services;
        }

        private static void SetInstance(ProblemDetailsContext context)
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        }

        private static void AddCustomExtensions(ProblemDetailsContext context, IWebHostEnvironment env)
        {
            var exception = context.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;

            // The fallback writer must be as safe as GlobalExceptionHandler.
            // Never copy exception messages, inner exceptions or stack traces to HTTP.
            if (exception is not null)
            {
                context.ProblemDetails.Title = "Zahtjev trenutno nije moguće završiti.";
                context.ProblemDetails.Detail = "Pokušajte ponovo. Ako se problem ponovi, podršci pošaljite navedeni ID.";
            }
            context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);

            var correlationId = context.HttpContext.Request.Headers[AppConstants.CORRELATION_ID].ToString();
            context.ProblemDetails.Extensions.TryAdd(AppConstants.CORRELATION_ID, correlationId);

            context.ProblemDetails.Extensions.TryAdd("timestamp", DateTime.UtcNow);
        }

    }
}
