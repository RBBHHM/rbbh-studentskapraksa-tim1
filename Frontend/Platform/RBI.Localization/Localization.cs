using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RBI.Localization.Internal;

namespace RBI.Localization;

/// <summary>Registers static RBI localization and its ASP.NET Core pipeline hooks.</summary>
public static class RbiLocalizationExtensions
{
    /// <summary>Registers application-owned JSON localization resources discovered at startup.</summary>
    public static IServiceCollection AddRbiLocalization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLocalization();
        services.AddOptions<RbiLocalizationOptions>();
        services.TryAddSingleton(provider => new RbiCultureCatalog(
            Path.Combine(provider.GetRequiredService<IHostEnvironment>().ContentRootPath, RbiLocalizationDefaults.ResourceDirectory),
            provider.GetRequiredService<IOptions<RbiLocalizationOptions>>().Value.DefaultCulture));
        services.AddOptions<RequestLocalizationOptions>().Configure<RbiCultureCatalog>((options, catalog) =>
        {
            var cultures = catalog.SupportedCultures.Select(System.Globalization.CultureInfo.GetCultureInfo).ToArray();
            options.SetDefaultCulture(catalog.DefaultCulture);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.RequestCultureProviders =
            [
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider(),
            ];
        });

        services.TryAddSingleton(provider => new RbiStaticJsonLocalizationProvider(
            provider.GetRequiredService<RbiCultureCatalog>(), provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RbiStaticJsonLocalizationProvider>>()));
        services.TryAddSingleton<IRbiLocalizationProvider>(provider =>
            provider.GetRequiredService<RbiStaticJsonLocalizationProvider>());
        services.TryAddSingleton<RbiMissingKeyTracker>();
        services.TryAddScoped<IRbiLocalizer, RbiLocalizer>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, RbiLocalizationStartupFilter>());
        return services;
    }

    /// <summary>Registers discovered resources with an application-selected default culture.</summary>
    public static IServiceCollection AddRbiLocalization(this IServiceCollection services, Action<RbiLocalizationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddRbiLocalization();
        services.Configure(configure);
        return services;
    }

    /// <summary>Enables request culture selection for registered RBI localization.</summary>
    public static IApplicationBuilder UseRbiLocalization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseRequestLocalization();
    }

    /// <summary>Maps the antiforgery-protected culture selection endpoint.</summary>
    public static IEndpointConventionBuilder MapRbiLocalizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapPost(
                $"{RbiLocalizationDefaults.CultureEndpointPrefix}/{{culture}}",
                async (HttpContext context, IAntiforgery antiforgery, RbiCultureCatalog catalog, string culture, string? returnUrl) =>
                {
                    // This handler has no form-bound parameter: middleware
                    // metadata alone does not reject an invalid POST for it.
                    var validation = context.Features.Get<IAntiforgeryValidationFeature>();
                    if (validation is { IsValid: false }) return Results.BadRequest();
                    if (validation is null)
                    {
                        try { await antiforgery.ValidateRequestAsync(context); }
                        catch (AntiforgeryValidationException) { return Results.BadRequest(); }
                    }
                    var normalized = culture.Trim().ToLowerInvariant();
                    if (!catalog.SupportedCultures.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                        return Results.BadRequest();

                    context.Response.Cookies.Append(
                        CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalized)),
                        new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddYears(1),
                            HttpOnly = true,
                            IsEssential = true,
                            SameSite = SameSiteMode.Lax,
                            Secure = context.Request.IsHttps,
                        });

                    var destination = IsLocalReturnUrl(returnUrl) ? returnUrl! : "/";
                    return Results.LocalRedirect(destination);
                })
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));
    }

    internal static bool IsLocalReturnUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Any(char.IsControl)
        && value[0] is '/'
        && (value.Length is 1 || value[1] is not '/' and not '\\');
}
