using System.Collections.Concurrent;
using System.Net;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.IdentityModel.Tokens;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;
using RBBH.ConnectedParties.IoC.Extensions.ExceptionHandling;
using RBBH.ConnectedParties.IoC.Middleware;

namespace IntegrationTests.Tests.Operations;

public sealed class SafeDiagnosticsHttpTests
{
    private const string PrivateValue = "private-user@example.test";

    [Theory]
    [InlineData(200)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    public async Task RealPipelineLogsOutcomesWithoutPathQueryClaimsOrExceptionData(int status)
    {
        var logs = new CapturedLogs();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Logging.AddProvider(logs);
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddAuthentication("Empty")
            .AddScheme<AuthenticationSchemeOptions, EmptyAuthentication>("Empty", _ => { });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseMiddleware<IncludeCorrelationIDMiddleware>();
        app.UseExceptionHandler();
        app.UseMiddleware<RequestOutcomeLoggingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/api/records/{identifier}", () => status == 500
            ? throw new InvalidOperationException("internal-secret-" + PrivateValue)
            : Results.StatusCode(status));
        app.MapGet("/api/protected/{identifier}", () => Results.Ok()).RequireAuthorization();
        await app.StartAsync();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + PrivateValue);
        client.DefaultRequestHeaders.Add("Cookie", "session=" + PrivateValue);
        client.DefaultRequestHeaders.Add("X-Correlation-ID", PrivateValue);
        // For 401 use authorization's real short-circuit, not only a returned status.
        var route = status == 401 ? "protected" : "records";
        var response = await client.GetAsync($"/api/{route}/{PrivateValue}?token={PrivateValue}");
        Assert.Equal(status, (int)response.StatusCode);
        Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-ID")), out _));
        if (status == 500) Assert.DoesNotContain("internal-secret-", await response.Content.ReadAsStringAsync());
        var entries = logs.Entries.Where(entry => entry.Category == typeof(RequestOutcomeLoggingMiddleware).FullName
            || entry.Category == typeof(GlobalExceptionHandler).FullName).ToArray();
        var entry = Assert.Single(entries);
        Assert.Contains(status.ToString(), entry.Message);
        Assert.Contains("{identifier}", entry.Message);
        Assert.Contains("TraceId", entry.Message);
        Assert.DoesNotContain(PrivateValue, entry.Message);
        Assert.Null(entry.Exception);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BearerFailureLogsOnlyCategoryAndPreservesExistingResponseHeaders(bool expired)
    {
        var logs = new CapturedLogs();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.AddProvider(logs);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["KeycloakSettings:Enabled"] = "true",
            ["KeycloakSettings:Issuer"] = "https://identity.example.test/realms/test",
            ["KeycloakSettings:ClientId"] = "fixture",
            ["KeycloakSettings:ClientSecret"] = "fixture-only",
            ["KeycloakSettings:Audience"] = "fixture"
        });
        builder.Services.AddDbContext<ConnectedPartiesDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        builder.Services.AddAuthenticationExtension(builder.Configuration, builder.Environment);
        await using var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        var failure = new AuthenticationFailedContext(context,
            new AuthenticationScheme("Bearer", "Bearer", typeof(JwtBearerHandler)), options)
        {
            Exception = expired ? new SecurityTokenExpiredException(PrivateValue)
                : new SecurityTokenInvalidSigningKeyException(PrivateValue)
        };
        await options.Events.AuthenticationFailed(failure);
        Assert.Equal("true", context.Response.Headers[expired ? "Token-Expired" : "SigningKey-Invalid"].ToString());
        var entry = Assert.Single(logs.Entries, entry => entry.Category == "AuthenticationFailure");
        Assert.Contains(failure.Exception.GetType().Name, entry.Message);
        Assert.DoesNotContain(PrivateValue, entry.Message);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public void FallbackProblemDetailsNeverIncludesRawExceptionDetails()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Services.AddCustomProblemDetails(builder.Environment);
        using var provider = builder.Services.BuildServiceProvider();
        var http = new DefaultHttpContext();
        http.Features.Set<IExceptionHandlerFeature>(new ExceptionHandlerFeature
        {
            Error = new InvalidOperationException(PrivateValue, new Exception("nested-secret"))
        });
        var context = new ProblemDetailsContext { HttpContext = http, ProblemDetails = new ProblemDetails { Status = 500 } };
        provider.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value.CustomizeProblemDetails!(context);
        var json = System.Text.Json.JsonSerializer.Serialize(context.ProblemDetails);
        Assert.DoesNotContain(PrivateValue, json);
        Assert.DoesNotContain("nested-secret", json);
        Assert.DoesNotContain("stackTrace", json);
        Assert.True(context.ProblemDetails.Extensions.ContainsKey("traceId"));
    }

    private sealed class EmptyAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
    }

    private sealed class CapturedLogs : ILoggerProvider
    {
        public ConcurrentQueue<(string Category, string Message, Exception? Exception)> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CapturedLogger(this, categoryName);
        public void Dispose() { }

        private sealed class CapturedLogger(CapturedLogs owner, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => owner.Entries.Enqueue((category, formatter(state, exception), exception));
        }
    }
}
