using Microsoft.AspNetCore.Mvc.Filters;
using RBBH.ConnectedParties.BL.Services;

namespace RBBH.ConnectedParties.Localization;

/// <summary>Loads the existing runtime pack before binding/validation; scope never crosses requests.</summary>
public sealed class BackendLocalizationFilter(ILogger<BackendLocalizationFilter> logger, RuntimeLocalizationCatalog? catalog = null) : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        IReadOnlyDictionary<string, string> labels = new Dictionary<string, string>();
        try
        {
            var pack = catalog is null ? null : await catalog.ForRequest(context.HttpContext.Request.Headers.AcceptLanguage.ToString(), context.HttpContext.RequestAborted);
            if (pack is not null)
            {
                var valid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in pack.Items)
                {
                    if (!BackendMessages.All.TryGetValue(row.ResourceKey, out var baseline)) continue;
                    try
                    {
                        var expected = System.Text.CompositeFormat.Parse(baseline);
                        var translated = System.Text.CompositeFormat.Parse(row.Label);
                        if (translated.MinimumArgumentCount > expected.MinimumArgumentCount) throw new FormatException();
                        valid[row.ResourceKey] = row.Label;
                    }
                    catch (FormatException)
                    {
                        // Reject the override before DataAnnotations formats it later.
                        logger.LogWarning("Backend resource template invalid. ResourceKey {ResourceKey}", row.ResourceKey);
                    }
                }
                labels = valid;
            }
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // Localization storage failure never turns a safe validation response into an infrastructure disclosure.
            logger.LogWarning("Backend message resources unavailable. FailureType {FailureType}", error.GetType().Name);
        }
        using (BackendMessages.Use(labels)) await next();
    }
}
