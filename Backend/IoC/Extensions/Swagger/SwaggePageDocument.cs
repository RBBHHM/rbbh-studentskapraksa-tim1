using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
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

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Paste a Keycloak access token."
                });

                options.OperationFilter<BearerAuthorizationOperationFilter>();
                options.OperationFilter<AddCorrelationIdHeader>();

                // Include XML comments if available
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            return services;
        }
    }

    public sealed class BearerAuthorizationOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
            if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
                return;

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                }] = Array.Empty<string>()
            });
        }
    }
}
