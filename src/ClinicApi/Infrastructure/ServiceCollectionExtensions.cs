using ClinicApi.Infrastructure.Caching;
using ClinicApi.Infrastructure.Persistence;
using ClinicApi.Infrastructure.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using StackExchange.Redis;
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
    /// Registers Redis connection, PHI encryption, and configurable caching services.
    /// </summary>
    public static IServiceCollection AddClinicCaching(
        this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            // Redis not configured — register a no-op cache service
            services.Configure<CachingOptions>(o => o.Enabled = false);
            services.AddSingleton<ICacheService, NoOpCacheService>();
            return services;
        }

        // Redis connection (singleton — multiplexer is thread-safe)
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        // PHI encryption
        var phiKey = configuration["PHI_ENCRYPTION_KEY"]
            ?? throw new InvalidOperationException("PHI_ENCRYPTION_KEY is required for caching.");
        services.AddSingleton<IPhiEncryptionService>(new AesGcmPhiEncryptionService(phiKey));

        // Caching configuration
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));

        // Cache service
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Health check for Redis
        services.AddHealthChecks()
            .AddRedis(redisConnectionString, name: "redis");

        return services;
    }

    /// <summary>
    /// Registers HIPAA security services: audit logging, auth, encryption config, tenant context.
    /// </summary>
    public static IServiceCollection AddClinicSecurity(
        this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
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
