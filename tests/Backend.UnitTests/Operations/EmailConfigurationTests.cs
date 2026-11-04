using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RBBH.ConnectedParties.BL.Services;

namespace UnitTests.Operations;

public sealed class EmailConfigurationTests
{
    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "fixture";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Testing", true)]
    [InlineData("Staging", false)]
    [InlineData("Production", false)]
    public void DemoDeliveryIsNotAProductionTransport(string environment, bool expected) =>
        Assert.Equal(expected, new EmailSettings { Provider = "demo" }.IsConfiguredFor(new Environment(environment)));

    [Theory]
    [InlineData("smtp")]
    [InlineData("httpapi")]
    public void RealTransportRequiresCurrentRecipientsAndSafeUrlsButNotRetiredHrMailbox(string provider)
    {
        var settings = new EmailSettings { Provider = provider, AdminEmail = "admin@example.test", HrEmail = "",
            AppBaseUrl = "https://app.example.test", From = "outbox@example.test", SendEmailWithHtmlBodyUrl = "https://mail.example.test/send",
            Smtp = new() { Host = "smtp.example.test", FromAddress = "outbox@example.test", EnableSsl = true } };
        var production = new Environment("Production");
        Assert.True(settings.IsConfiguredFor(production));
        settings.AdminEmail = ""; Assert.False(settings.IsConfiguredFor(production)); settings.AdminEmail = "admin@example.test";
        settings.AppBaseUrl = "http://localhost:5001"; Assert.False(settings.IsConfiguredFor(production));
        settings.AppBaseUrl = "https://user:secret@app.example.test"; Assert.False(settings.IsConfiguredFor(production));
        settings.AppBaseUrl = "https://app.example.test";
        if (provider == "smtp") settings.Smtp.EnableSsl = false;
        else settings.SendEmailWithHtmlBodyUrl = "http://mail.example.test/send";
        Assert.False(settings.IsConfiguredFor(production));
    }
}
