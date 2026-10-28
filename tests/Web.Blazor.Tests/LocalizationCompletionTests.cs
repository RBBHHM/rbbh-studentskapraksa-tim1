using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using RBI.Localization;

namespace Web.Blazor.Tests;

public sealed class LocalizationCompletionTests
{
    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RelatedPartiesRegister.slnx"))) directory = directory.Parent;
        return Path.Combine(directory!.FullName, "Frontend");
    }
    [Fact]
    public void BothBusinessCatalogsHaveIdenticalKeysAndNoEmptyValuesOrDuplicateProperties()
    {
        var sets = new List<string[]>();
        foreach (var culture in new[] { "bs", "en" })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Localization", culture + ".json")));
            var properties = document.RootElement.GetProperty("translations").EnumerateObject().ToArray();
            Assert.Equal(properties.Length, properties.Select(item => item.Name).Distinct().Count());
            Assert.All(properties, item => Assert.False(string.IsNullOrWhiteSpace(item.Value.GetString()), item.Name));
            sets.Add(properties.Select(item => item.Name).Order().ToArray());
        }
        Assert.Equal(sets[0], sets[1]);
    }
    [Fact]
    public void ActualPlatformCatalogDiscoversOnlyBusinessCulturesWithBosnianDefaultAndSafeMissingKey()
    {
        var services = new ServiceCollection(); services.AddLogging();
        services.AddSingleton(Mock.Of<IHostEnvironment>(environment => environment.ContentRootPath == Root()));
        services.AddRbiLocalization(options => options.DefaultCulture = "bs");
        using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
        var catalog = provider.GetRequiredService<RbiCultureCatalog>();
        Assert.Equal("bs", catalog.DefaultCulture); Assert.Equal(new[] { "bs", "en" }, catalog.SupportedCultures.Order().ToArray());
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
            var localizer = scope.ServiceProvider.GetRequiredService<IRbiLocalizer>();
            Assert.Equal("User guide", localizer["guide.title"]);
            Assert.Equal("missing.safe-key", localizer["missing.safe-key"]);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de");
            Assert.Equal("Korisnički vodič", localizer["guide.title"]);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }
}
