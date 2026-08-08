using ClinicApi.Infrastructure.Persistence;
using ClinicApi.Infrastructure.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Wolverine.EntityFrameworkCore;

namespace ClinicApi.Infrastructure;

/// <summary>
/// Extension methods to keep Program.cs lean.
/// Each method registers a cohesive group of services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core, Dapper, BulkInsert, FluentValidation, health checks.
    /// </summary>
    public static IServiceCollection AddClinicPersistence(
        this IServiceCollection services, string connectionString)
    {
        // EF Core (write-side) with Wolverine integration
        services.AddDbContextWithWolverineIntegration<ClinicDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql => sql.CommandTimeout(30));
        });

        // Dapper connection factory (read-side)
        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));

        // Bulk insert service
        services.AddSingleton<IBulkInsertService>(new SqlBulkInsertService(connectionString));

        // FluentValidation - auto-register validators from the main assembly
        services.AddValidatorsFromAssemblyContaining<ClinicDbContext>(ServiceLifetime.Singleton);

        // Health checks
        services.AddHealthChecks()
            .AddDbContextCheck<ClinicDbContext>("mssql");

        return services;
    }

    /// <summary>
    /// Registers HIPAA security services: audit logging, auth, encryption config.
    /// </summary>
    public static IServiceCollection AddClinicSecurity(
        this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<IPhiAuditLogger, PhiAuditLogger>();
        services.AddClinicAuthorization(environment);
        return services;
    }

    /// <summary>
    /// Registers OpenTelemetry when OTEL_EXPORTER_OTLP_ENDPOINT is configured.
    /// </summary>
    public static WebApplicationBuilder AddClinicObservability(this WebApplicationBuilder builder)
    {
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (otlpEndpoint is null) return builder;

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options => options.SetDbStatementForText = true)
                    .AddSource("Wolverine")
                    .AddOtlpExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Wolverine")
                    .AddOtlpExporter();
            });

        builder.Logging.AddOpenTelemetry(logging => logging.AddOtlpExporter());

        return builder;
    }
}
