using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using RBBH.ConnectedParties.BL.Services;

namespace RBBH.ConnectedParties.Localization;

/// <summary>Loads runtime validation labels before model binding.</summary>
public sealed class BackendLocalizationFilter(ILogger<BackendLocalizationFilter> logger, RuntimeLocalizationCatalog? catalog = null) : IAsyncResourceFilter
{
    private static readonly IReadOnlyDictionary<string, string> EmptyLabels = new Dictionary<string, string>();
    private static readonly ConditionalWeakTable<RuntimeResourcePack, IReadOnlyDictionary<string, string>> ValidatedLabels = new();

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var labels = EmptyLabels;
        try
        {
            if (catalog is not null)
            {
                var pack = await catalog.ForRequest(context.HttpContext.Request.Headers.AcceptLanguage.ToString(), context.HttpContext.RequestAborted);
                if (pack is not null) labels = ValidatedLabels.GetValue(pack, ValidateLabels);
            }
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning("Backend message resources unavailable. FailureType {FailureType}", error.GetType().Name);
        }

        using (BackendMessages.Use(labels)) await next();
    }

    private IReadOnlyDictionary<string, string> ValidateLabels(RuntimeResourcePack pack)
    {
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in pack.Items)
        {
            if (!BackendMessages.All.TryGetValue(row.ResourceKey, out var baseline)) continue;
            try
            {
                if (CompositeFormat.Parse(row.Label).MinimumArgumentCount > CompositeFormat.Parse(baseline).MinimumArgumentCount)
                    throw new FormatException();
                labels[row.ResourceKey] = row.Label;
            }
            catch (FormatException)
            {
                logger.LogWarning("Backend resource template invalid. ResourceKey {ResourceKey}", row.ResourceKey);
            }
        }
        return labels;
    }
}
