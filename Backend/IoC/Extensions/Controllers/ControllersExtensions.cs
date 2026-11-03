namespace RBBH.ConnectedParties.IoC.Extensions.Controllers
{
    public static class ControllersExtensions
    {
        public static IServiceCollection AddControllersExtension(this IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.Filters.Add<BackendLocalizationFilter>();
                options.ModelMetadataDetailsProviders.Add(new BackendValidationMetadataProvider());
            });
            services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(item => item.Value?.Errors.Count > 0)
                        .ToDictionary(
                            item => FriendlyFieldName(item.Key),
                            item => item.Value!.Errors
                                .Select(error => FriendlyValidationMessage(error.ErrorMessage, item.Key))
                                .Distinct()
                                .ToArray());
                    var problem = new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = BackendMessages.Message("errors.model.invalid.title"),
                        Detail = BackendMessages.Message("errors.model.invalid.detail"),
                        Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}"
                    };
                    problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                    return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problem);
                };
            });
            services.AddEndpointsApiExplorer();

            return services;
        }

        private static string FriendlyFieldName(string key)
        {
            var field = key.TrimStart('$', '.').Split('.').LastOrDefault();
            return string.IsNullOrWhiteSpace(field) || field.Equals("dto", StringComparison.OrdinalIgnoreCase)
                ? "podaci"
                : char.ToLowerInvariant(field[0]) + field[1..];
        }

        private static string FriendlyValidationMessage(string message, string key)
        {
            if (string.IsNullOrWhiteSpace(message) ||
                message.Contains("JSON value could not be converted", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("System.", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("BytePositionInLine", StringComparison.OrdinalIgnoreCase))
            {
                var field = FriendlyFieldName(key);
                return BackendMessages.Message("errors.model.field.invalid", field);
            }
            return message.Replace("The dto field is required.",
                BackendMessages.Message("errors.model.form.required"),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
