namespace RBBH.ConnectedParties.IoC.Extensions.HTTP
{
    public static class HTTPExtensions
    {
        public static IServiceCollection AddHTTPExtension(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddHttpContextAccessor();
            services.AddTransient<HeaderPropagationHandler>();

            //#if (!IsClean)
            services.AddCustomHttpClient("JsonPlaceholder", configuration["JsonPlaceholderUri"], true);
            services.AddCustomHttpClient("TemenosAPI", configuration["TemenosAPIUri"], true);
            //#endif

            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="clientName"></param>
        /// <param name="clientUri"></param>
        /// <param name="includeHeaderPropagationHandler"></param>
        private static void AddCustomHttpClient(this IServiceCollection services, string clientName, string? clientUri, bool includeHeaderPropagationHandler = false)
        {
            var httpClientBuilder = services.AddHttpClient(clientName, c =>
            {
                if (Uri.TryCreate(clientUri, UriKind.Absolute, out var baseAddress))
                    c.BaseAddress = baseAddress;
                c.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            if (includeHeaderPropagationHandler)
            {
                httpClientBuilder.AddHttpMessageHandler<HeaderPropagationHandler>();
            }
        }
    }
}
