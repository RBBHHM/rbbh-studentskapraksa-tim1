using RBBH.ConnectedParties.Exceptions;

namespace RBBH.ConnectedParties.IoC.Extensions.ExceptionHandling
{
    public static class ExceptionHandlingExtension
    {
        public static IServiceCollection AddExceptionHandlingExtension(this IServiceCollection services, IWebHostEnvironment env)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(env);

            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddCustomProblemDetails(env);

            return services;
        }
    }
}
