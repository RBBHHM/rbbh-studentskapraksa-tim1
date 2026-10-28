using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using RBI.Localization;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Components;
using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class LimitPickerCompositionTests
{
    [Fact]
    public async Task StandalonePickerSuppliesValueExpressionAndActualLabelTarget()
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton(Mock.Of<IJSRuntime>());
        var api = new Mock<ILimitsApi>();
        api.Setup(client => client.SearchEntitiesAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        services.AddSingleton(api.Object);
        var localizer = new Mock<IRbiLocalizer>(); localizer.Setup(item => item[It.IsAny<string>()]).Returns((string key) => key);
        services.AddSingleton(new AppText(localizer.Object));
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<LimitEntityPicker>(ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(LimitEntityPicker.Id)] = "fixture-select" }));
            return component.ToHtmlString();
        });
        Assert.Contains("id=\"fixture-select\"", html); Assert.DoesNotContain("id=\"Id\"", html);
        // The type-ahead input is itself the label target: RbiField/aria-label reference the same id and the listbox is wired through aria-controls.
        Assert.Contains("id=\"fixture-select\"", html); Assert.Contains("role=\"combobox\"", html); Assert.Contains("aria-controls=\"fixture-select-list\"", html);
    }
}
