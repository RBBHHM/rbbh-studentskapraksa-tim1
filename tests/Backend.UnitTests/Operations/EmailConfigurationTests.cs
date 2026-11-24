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
        Assert.Equal(expected, new EmailSettings().IsConfiguredFor(new Environment(environment)));

    [Theory]
    [InlineData("https://mail.example.test/send", true)]
    [InlineData("http://mail.test.rbbh/api/Email/SendEmailWithHtmlBody", true)]
    [InlineData("http://mail.example.test/send", false)]
    [InlineData("https://user:secret@mail.example.test/send", false)]
    [InlineData("", false)]
    public void MailTransportRequiresOnlySenderAndApprovedEndpoint(string endpoint, bool expected)
    {
        var production = new Environment("Production");
        var settings = new EmailSettings { From = "outbox@example.test", SendEmailWithHtmlBodyUrl = endpoint };
        Assert.Equal(expected, settings.IsConfiguredFor(production));
        settings.From = "";
        Assert.False(settings.IsConfiguredFor(production));
    }
}
