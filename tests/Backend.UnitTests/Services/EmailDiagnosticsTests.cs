using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RBBH.ConnectedParties.BL.Services;

namespace UnitTests.Services;

public sealed class EmailDiagnosticsTests
{
    private const string PrivateValue = "private-recipient@example.test";

    [Theory]
    [InlineData(200)]
    [InlineData(500)]
    [InlineData(0)]
    public async Task HttpDeliveryLogsStatusOrFailureTypeWithoutRecipientSubjectOrException(int status)
    {
        var logger = new Mock<ILogger<HttpApiEmailService>>();
        using var http = new HttpClient(new MailTransport(status));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(value => value.CreateClient(It.IsAny<string>())).Returns(http);
        var service = new HttpApiEmailService(factory.Object, Options.Create(new EmailSettings
        {
            AppBaseUrl = "https://app.example.test", From = "sender@example.test",
            SendEmailWithHtmlBodyUrl = "https://mail.example.test/send"
        }), new EmailLogStore(), logger.Object);
        await service.SendUnlockRequestAsync(PrivateValue, PrivateValue, 2026, 10, PrivateValue);
        var entry = Assert.Single(logger.Invocations, entry => entry.Method.Name == "Log");
        Assert.DoesNotContain(PrivateValue, entry.Arguments[2].ToString());
        Assert.Null(entry.Arguments[3]);
        if (status == 500) Assert.Contains("500", entry.Arguments[2].ToString());
        if (status == 0) Assert.Contains(nameof(HttpRequestException), entry.Arguments[2].ToString());
    }

    [Fact]
    public async Task DemoMailKeepsBusinessContentInItsStoreWithoutCopyingItToOperationalLogs()
    {
        var logger = new Mock<ILogger<MockEmailService>>();
        var store = new EmailLogStore();
        var service = new MockEmailService(Options.Create(new EmailSettings { AppBaseUrl = "https://app.example.test" }), store, logger.Object);
        await service.SendUnlockRequestAsync(PrivateValue, PrivateValue, 2026, 10, PrivateValue);
        Assert.Equal(PrivateValue, Assert.Single(store.GetAll()).To);
        var entry = Assert.Single(logger.Invocations, entry => entry.Method.Name == "Log");
        Assert.DoesNotContain(PrivateValue, entry.Arguments[2].ToString());
    }

    private sealed class MailTransport(int status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            status == 0 ? throw new HttpRequestException(PrivateValue)
                : Task.FromResult(new HttpResponseMessage((HttpStatusCode)status));
    }
}
