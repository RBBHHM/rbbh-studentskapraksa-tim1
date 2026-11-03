using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.Middlewares;

namespace UnitTests.Middleware;

public sealed class PeriodLockPrivacyTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LockedRequestsLogOnlyMethodAndRouteTemplate(bool matched)
    {
        var context = new DefaultHttpContext(); context.Request.Method = "PUT";
        context.Request.Path = "/api/legal-entities/sentinel-user@example.invalid";
        context.Response.Body = new MemoryStream();
        using var services = new ServiceCollection().BuildServiceProvider(); context.RequestServices = services;
        if (matched) context.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask,
            RoutePatternFactory.Parse("api/legal-entities/{id:guid}"), 0, EndpointMetadataCollection.Empty, "fixture"));
        var repository = Substitute.For<IPeriodLockRepository>(); repository.IsCurrentPeriodLockedAsync().Returns(Task.FromResult(true));
        var logger = new CapturingLogger();
        await new PeriodLockMiddleware(_ => throw new InvalidOperationException("Locked action must not execute."), logger).InvokeAsync(context, repository);
        Assert.Equal(423, context.Response.StatusCode);
        var message = Assert.Single(logger.Messages);
        Assert.Contains(matched ? "api/legal-entities/{id:guid}" : "(unmatched)", message);
        Assert.Contains("PUT", message); Assert.DoesNotContain("sentinel-user", message); Assert.DoesNotContain("@example.invalid", message);
    }

    private sealed class CapturingLogger : ILogger<PeriodLockMiddleware>
    {
        internal List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { Assert.Null(exception); Messages.Add(formatter(state, exception)); }
    }
}
