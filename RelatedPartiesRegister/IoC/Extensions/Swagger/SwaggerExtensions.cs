
namespace RBBH.ConnectedParties.IoC.Extensions.Swagger
{
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Used for Dependency Injection
        /// </summary>
        /// <param name="services"></param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddSwaggerExtension(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.SwaggerPageConfig();

            return services;
        }

        /// <summary>
        /// Used for App pipeline
        /// </summary>
        /// <param name="app"></param>
        public static void UseSwaggerExtension(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Connected Parties API v1");
                c.RoutePrefix = "";
            });
        }
    }
}
