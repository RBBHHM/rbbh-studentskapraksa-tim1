namespace RBBH.ConnectedParties.IoC.Extensions.Controllers
{
    public static class ControllersExtensions
    {
        public static IServiceCollection AddControllersExtension(this IServiceCollection services)
        {
            services.AddControllers();
            services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var bosnian = context.HttpContext.Request.Headers.AcceptLanguage
                        .Any(value => value?.StartsWith("bs", StringComparison.OrdinalIgnoreCase) == true);
                    var errors = context.ModelState
                        .Where(item => item.Value?.Errors.Count > 0)
                        .ToDictionary(
                            item => FriendlyFieldName(item.Key),
                            item => item.Value!.Errors
                                .Select(error => FriendlyValidationMessage(error.ErrorMessage, item.Key, bosnian))
                                .Distinct()
                                .ToArray());
                    var problem = new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = bosnian ? "Uneseni podaci nisu ispravni." : "The submitted data is invalid.",
                        Detail = bosnian
                            ? "Provjerite označena polja i pokušajte ponovo."
                            : "Check the highlighted fields and try again.",
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

        private static string FriendlyValidationMessage(string message, string key, bool bosnian)
        {
            if (string.IsNullOrWhiteSpace(message) ||
                message.Contains("JSON value could not be converted", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("System.", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("BytePositionInLine", StringComparison.OrdinalIgnoreCase))
            {
                var field = FriendlyFieldName(key);
                return bosnian
                    ? $"Vrijednost polja '{field}' nije ispravna."
                    : $"The value of '{field}' is invalid.";
            }
            return message.Replace("The dto field is required.",
                bosnian ? "Podaci obrasca su obavezni." : "Form data is required.",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
