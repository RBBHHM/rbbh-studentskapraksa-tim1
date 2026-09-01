using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Events;

namespace RBBH.ConnectedParties.IoC.Extensions.Logging
{
    public static class SerilogExtensions
    {
        public static IHostBuilder AddSerilogExtension(this IHostBuilder hostBuilder)
        {
            ArgumentNullException.ThrowIfNull(hostBuilder);

            // Hijack project to use Serilog instead of the default Microsoft ILogger
            // This means Serilog will be used for all logs (including those on startup)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Console(theme:AnsiConsoleTheme.Code)
                .CreateLogger();

            return hostBuilder.UseSerilog(
                (context, options) =>
                    options
                        .ReadFrom.Configuration(context.Configuration)
                        .MinimumLevel.Is(LogEventLevel.Warning)
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                        .MinimumLevel.Override("System", LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .Enrich.With(new HeaderEnricher(new HttpContextAccessor()))
            );
        }
    }
}
