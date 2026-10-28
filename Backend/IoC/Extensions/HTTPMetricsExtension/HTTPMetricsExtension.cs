using System.Reflection;
using Prometheus;

namespace RBBH.ConnectedParties.IoC.Extensions.HTTPMetricsExtension
{
    public static class HTTPMetricsExtension
    {
        public static void AddHTTPMetricsExtension(this WebApplication app)
        {
            app.UseMetricServer();

            app.UseHttpMetrics(options =>
            {
                options.RequestCount.Enabled = true;

                options.RequestDuration.Histogram = Metrics.CreateHistogram(
                    $"{Assembly.GetExecutingAssembly().GetName().Name?.Replace(".", "_")}_http_request_duration_seconds",
                    $"{Assembly.GetExecutingAssembly().GetName().Name?.Replace(".", "_")} Metrika",
                    new HistogramConfiguration
                    {
                        Buckets = Histogram.LinearBuckets(start: 0, width: 0.2, count: 50),
                        LabelNames = new[] { "code", "method", "controller", "action" },
                    }
                );
            });
        }
    }
}
