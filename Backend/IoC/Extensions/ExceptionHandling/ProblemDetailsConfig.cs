using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.Exceptions.Custom;
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

            SetTitle(context, exception);

                if (exception != null)
                {
                    // If this is a validation exception, expose structured errors
                    if (exception is ValidationException vex)
                    {
                        context.ProblemDetails.Extensions.TryAdd("errors", vex.Errors);
                    }

                    context.ProblemDetails.Extensions.TryAdd("message", exception.Message);
                    context.ProblemDetails.Extensions.TryAdd("innerException", exception.InnerException?.Message);

                    if (env.IsDevelopment())
                    {
                        context.ProblemDetails.Extensions.TryAdd("stackTrace", exception.StackTrace);
                    }

                }

            var correlationId = context.HttpContext.Request.Headers[AppConstants.CORRELATION_ID].ToString();
            context.ProblemDetails.Extensions.TryAdd(AppConstants.CORRELATION_ID, correlationId);

            context.ProblemDetails.Extensions.TryAdd("timestamp", DateTime.UtcNow);
        }

        private static void SetTitle(ProblemDetailsContext context, Exception? exception)
        {
            if (exception != null) 
            {
                context.ProblemDetails.Title = exception.GetType().Name;
                return;
            }

            // If null, use default value on "context.ProblemDetails.Title"
        }
    }
}
