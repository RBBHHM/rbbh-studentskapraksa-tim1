using Microsoft.OpenApi.Models;
using System.Reflection;

namespace RBBH.ConnectedParties.IoC.Extensions.Swagger
{
    public static class SwaggePageDocument
    {
        public static IServiceCollection SwaggerPageConfig(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                var title = "Connected Parties API";
                var version = Assembly.GetExecutingAssembly()?.GetName().Version!.ToString();
                var ver = $"v{version?.Split(".")[0]}" ?? "v1";

                options.SwaggerDoc(ver, new OpenApiInfo
                {
                    Version = version,
                    Title = title,
                    Description = "API registra povezanih fizičkih i pravnih lica, limita i regulatornih izvještaja.",
                    License = new OpenApiLicense
                    {
                        Name = "RBBH internal use"
                    }
                });

                options.OperationFilter<AddCorrelationIdHeader>();

                // Include XML comments if available
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            return services;
        }
    }
}
