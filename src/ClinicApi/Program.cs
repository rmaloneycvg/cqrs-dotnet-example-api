using ClinicApi.Infrastructure.Persistence;
using ClinicApi.Infrastructure.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

// ─── Migration CLI ───────────────────────────────────────────────────────────
// Usage: dotnet ClinicApi.dll --migrate
// Applies pending EF Core migrations and exits. Used by the db-migrate k8s Job.
if (args.Contains("--migrate"))
{
    var migrateBuilder = WebApplication.CreateBuilder(args);
    var cs = migrateBuilder.Configuration.GetConnectionString("ClinicDb")
        ?? throw new InvalidOperationException("ConnectionStrings:ClinicDb is required for migration.");
    migrateBuilder.Services.AddDbContext<ClinicDbContext>(options => options.UseSqlServer(cs));
    var migrateApp = migrateBuilder.Build();
    using var scope = migrateApp.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ClinicDbContext>();
    await db.Database.MigrateAsync();
    Console.WriteLine("Database migration completed successfully.");
    return;
}

// ─── Application Builder ─────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// Configuration
var connectionString = builder.Configuration.GetConnectionString("ClinicDb")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:ClinicDb is required. Set it in appsettings.json, environment variables, or user-secrets.");
var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:RabbitMq is required. Set it in appsettings.json, environment variables, or user-secrets.");

// EF Core (write-side)
builder.Services.AddDbContextWithWolverineIntegration<ClinicDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
    });
});

// HIPAA §164.312(a)(2)(iv): PHI encryption at rest
// Handled by SQL Server Always Encrypted — transparent to application code.
// Connection string includes "Column Encryption Setting=Enabled" which tells
// Microsoft.Data.SqlClient to decrypt PHI columns automatically.
// CMK credentials injected via Vault sidecar in staging/prod.
//
// HIPAA §164.312(b): Audit controls
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPhiAuditLogger, PhiAuditLogger>();

// HIPAA §164.312(a)(1): Access controls
builder.Services.AddClinicAuthorization(builder.Environment);

// Response compression (gzip/brotli for list endpoints)
builder.Services.AddResponseCompression();



// Dapper connection factory (read-side)
builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));

// Bulk insert service
builder.Services.AddSingleton<IBulkInsertService>(new SqlBulkInsertService(connectionString));

// FluentValidation - auto-register validators from this assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ClinicDbContext>("mssql");

// OpenTelemetry (tracing + metrics) — enabled when OTEL_EXPORTER_OTLP_ENDPOINT is set
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

if (otlpEndpoint is not null)
{
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

    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.AddOtlpExporter();
    });
}

// Global exception handling with ProblemDetails
builder.Services.AddProblemDetails();

// Wolverine HTTP endpoint discovery with FluentValidation
builder.Services.AddWolverineHttp();

// OpenAPI documentation
builder.Services.AddOpenApi();

// Wolverine configuration
builder.Host.UseWolverine(opts =>
{
    // SQL Server persistence for durable messaging (outbox/inbox)
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    opts.Policies.UseDurableInboxOnAllListeners();

    // EF Core integration for transactional outbox
    opts.Policies.AutoApplyTransactions();

    // FluentValidation middleware
    opts.UseFluentValidation();

    // RabbitMQ transport (single configuration chain)
    opts.UseRabbitMq(new Uri(rabbitMqConnection))
        .AutoProvision()
        .EnableWolverineControlQueues()
        .DeclareExchange("clinic.dead-letter", exchange =>
        {
            exchange.ExchangeType = ExchangeType.Direct;
            exchange.IsDurable = true;
        })
        .DeclareQueue("clinic.dead-letter-queue", queue =>
        {
            queue.IsDurable = true;
        })
        .BindExchange("clinic.dead-letter").ToQueue("clinic.dead-letter-queue");

    // Configure exchanges
    opts.PublishAllMessages().ToRabbitTopics("clinic.events", exchange =>
    {
        exchange.ExchangeType = ExchangeType.Topic;
        exchange.IsDurable = true;
    });

    opts.ListenToRabbitQueue("clinic.commands", queue =>
    {
        queue.IsDurable = true;
    }).CircuitBreaker(cb =>
    {
        cb.MinimumThreshold = 5;
        cb.PauseTime = TimeSpan.FromSeconds(30);
        cb.TrackingPeriod = TimeSpan.FromMinutes(1);
        cb.FailurePercentageThreshold = 50;
    });

    // Error handling and retry policies
    opts.OnException<TimeoutException>()
        .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));

    opts.OnException<InvalidOperationException>()
        .MoveToErrorQueue();

    // Discovery
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);

    // Custom middleware (logging + performance tracking combined)
    opts.Policies.AddMiddleware(typeof(ClinicApi.Infrastructure.Middleware.LoggingMiddleware));
});

var app = builder.Build();

// Middleware
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/health");

// OpenAPI + Scalar (development only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Wolverine HTTP endpoints
app.MapWolverineEndpoints(opts =>
{
    opts.UseFluentValidationProblemDetailMiddleware();
});

app.Run();

// Required for integration test access
public partial class Program { }
