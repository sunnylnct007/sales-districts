using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SalesDistricts.Api.Observability;

public static class ObservabilitySetup
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
            builder.Logging.AddJsonConsole();   // machine-readable logs for the log collector

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("sales-districts-api"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithLogging();

        // Exported only when a collector is configured, e.g. OTEL_EXPORTER_OTLP_ENDPOINT=http://collector:4317
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
            telemetry.UseOtlpExporter();

        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        return builder;
    }

    // Liveness: the process answers. Readiness: it can also reach the database.
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
    }
}
