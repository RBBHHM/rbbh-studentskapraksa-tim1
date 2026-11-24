using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RBBH.ConnectedParties.BL.Services;

namespace UnitTests.Services;

public sealed class EmailDiagnosticsTests
{
    private const string PrivateValue = "private-recipient@example.test";
    private const string Sender = "sender@example.test";
    private const string Endpoint = "https://mail.example.test/api/Email/SendEmailWithHtmlBody";

    [Theory]
    [InlineData(200)]
    [InlineData(500)]
    [InlineData(0)]
    public async Task SendsSwaggerPayloadAndPropagatesFailureWithoutLoggingMailContent(int status)
    {
        var logger = new Mock<ILogger<HttpApiEmailService>>();
        using var handler = new MailTransport(status);
        using var http = new HttpClient(handler);
        var service = new HttpApiEmailService(http,
            Options.Create(new EmailSettings { From = Sender, SendEmailWithHtmlBodyUrl = Endpoint }), logger.Object);

        Task Send() => service.SendApplicationNotificationAsync(PrivateValue, "Subject", "<p>Body</p>");
        if (status == 200) await Send();
        else await Assert.ThrowsAsync<HttpRequestException>(Send);

        var payload = handler.Payload;
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(Endpoint, handler.Url);
        Assert.Equal(Sender, payload.GetProperty("from").GetString());
        Assert.Equal(PrivateValue, payload.GetProperty("to").GetString());
        Assert.Equal("Subject", payload.GetProperty("subject").GetString());
        Assert.Equal("<p>Body</p>", payload.GetProperty("text").GetString());
        var entry = Assert.Single(logger.Invocations, item => item.Method.Name == "Log");
        Assert.DoesNotContain(PrivateValue, entry.Arguments[2].ToString());
        Assert.Null(entry.Arguments[3]);
        if (status == 500) Assert.Contains("InternalServerError", entry.Arguments[2].ToString());
    }

    [Fact]
    public async Task DemoMailKeepsContentInItsStoreWithoutCopyingItToOperationalLogs()
    {
        var logger = new Mock<ILogger<MockEmailService>>();
        var store = new EmailLogStore();
        var service = new MockEmailService(store, logger.Object);
        await service.SendApplicationNotificationAsync(PrivateValue, "Subject", "<p>Body</p>");
        Assert.Equal(PrivateValue, Assert.Single(store.GetAll()).To);
        var entry = Assert.Single(logger.Invocations, item => item.Method.Name == "Log");
        Assert.DoesNotContain(PrivateValue, entry.Arguments[2].ToString());
    }

    private sealed class MailTransport(int status) : HttpMessageHandler
    {
        public JsonElement Payload { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? Url { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Url = request.RequestUri?.AbsoluteUri;
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Payload = json.RootElement.Clone();
            return status == 0 ? throw new HttpRequestException(PrivateValue) : new((HttpStatusCode)status);
        }
    }
}
