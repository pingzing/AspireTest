using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Exceptions;
using static Serilog.Sinks.OpenTelemetry.IncludedData;

namespace Microsoft.Extensions.Hosting;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureSerilog();
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureSerilog(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSerilog(config =>
        {
            config
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithExceptionDetails()
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .WriteTo.OpenTelemetry(options =>
                {
                    options.IncludedData = TraceIdField | SpanIdField;
                    options.Endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]!;
                    AddHeaders(
                        options.Headers,
                        builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"]!
                    );
                    AddResourceAttributes(
                        options.ResourceAttributes,
                        builder.Configuration["OTEL_RESOURCE_ATTRIBUTES"]!
                    );

                    static void AddHeaders(IDictionary<string, string> headers, string headerConfig)
                    {
                        if (!string.IsNullOrEmpty(headerConfig))
                        {
                            foreach (var header in headerConfig.Split(','))
                            {
                                var parts = header.Split('=');
                                if (parts.Length == 2)
                                {
                                    headers[parts[0]] = parts[1];
                                }
                                else
                                {
                                    throw new InvalidOperationException(
                                        $"Invalid header format: {header}"
                                    );
                                }
                            }
                        }
                    }

                    static void AddResourceAttributes(
                        IDictionary<string, object> attributes,
                        string attributeConfig
                    )
                    {
                        if (!string.IsNullOrEmpty(attributeConfig))
                        {
                            var parts = attributeConfig.Split('=');

                            if (parts.Length == 2)
                            {
                                attributes[parts[0]] = parts[1];
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    $"Invalid resource attribute foramt: {attributeConfig}"
                                );
                            }
                        }
                    }
                });
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(
        this IHostApplicationBuilder builder
    )
    {
        // The default WithLogging is removed, so that projects can add serilog, and make it report to OTLP on their own
        builder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation(options =>
                        options.FilterHttpRequestMessage = request =>
                        {
                            // Don't collect instrumentation on the http requests that send info to the OTLP endpoint
                            return !request.RequestUri?.AbsoluteUri.Contains(
                                    builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]!,
                                    StringComparison.Ordinal
                                ) ?? true;
                        }
                    );
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(
        this IHostApplicationBuilder builder
    )
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
        );

        if (useOtlpExporter)
        {
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
                metrics.AddOtlpExporter()
            );
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
                tracing.AddOtlpExporter()
            );
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(
        this IHostApplicationBuilder builder
    )
    {
        builder
            .Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks("/health");

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(
                "/alive",
                new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }
            );
        }

        return app;
    }
}
