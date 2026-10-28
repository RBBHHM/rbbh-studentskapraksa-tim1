using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace RBI.Localization.Internal;

internal sealed class RbiLocalizationStartupFilter(IRbiLocalizationProvider provider) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        _ = provider;
        return next;
    }
}
