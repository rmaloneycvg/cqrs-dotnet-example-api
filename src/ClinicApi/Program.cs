using ClinicApi.Infrastructure;
using ClinicApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

// ─── Migration CLI ───────────────────────────────────────────────────────────
// Usage: dotnet ClinicApi.dll --migrate
if (args.Contains("--migrate"))
{
    var migrateBuilder = WebApplication.CreateBuilder(args);
    var cs = migrateBuilder.Configuration.GetConnectionString("ClinicDb")
        ?? throw new InvalidOperationException("ConnectionStrings:ClinicDb is required for migration.");
    migrateBuilder.Services.AddSingleton<ClinicApi.Infrastructure.Security.ITenantContext>(
        new ClinicApi.Infrastructure.Security.MigrationTenantContext());
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

var connectionString = builder.Configuration.GetConnectionString("ClinicDb")
    ?? throw new InvalidOperationException("ConnectionStrings:ClinicDb is required.");
var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMq is required.");

// Infrastructure services (EF Core, Dapper, FluentValidation, health checks)
builder.Services.AddClinicPersistence(connectionString);

// Caching (Redis + PHI encryption — configuration-driven per endpoint)
builder.Services.AddClinicCaching(builder.Configuration);

// Security (HIPAA audit logging, JWT auth, RBAC policies)
builder.Services.AddClinicSecurity(builder.Environment);

// Observability (OpenTelemetry — only when OTLP endpoint is configured)
builder.AddClinicObservability();

// HTTP features
builder.Services.AddResponseCompression();
builder.Services.AddProblemDetails();
builder.Services.AddWolverineHttp();
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer<ClinicApi.Infrastructure.OpenApi.FakeDataSchemaTransformer>();
    options.AddOperationTransformer<ClinicApi.Infrastructure.OpenApi.BatchExampleTransformer>();
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Servers =
        [
            new() { Url = "/api", Description = "Current environment" }
        ];
        return Task.CompletedTask;
    });
});

// ─── Wolverine Configuration ─────────────────────────────────────────────────

builder.Host.UseWolverine(opts =>
{
    // Durable messaging (outbox/inbox with SQL Server)
    opts.PersistMessagesWithSqlServer(connectionString, "wolverine");
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    opts.Policies.UseDurableInboxOnAllListeners();
    opts.Policies.AutoApplyTransactions();

    // Middleware pipeline
    opts.UseFluentValidation();
    opts.Policies.AddMiddleware(typeof(ClinicApi.Infrastructure.Middleware.LoggingMiddleware));
    opts.Policies.AddMiddleware(typeof(ClinicApi.Infrastructure.Middleware.PhiAuditMiddleware));

    // RabbitMQ transport
    opts.UseRabbitMq(new Uri(rabbitMqConnection))
        .AutoProvision()
        .EnableWolverineControlQueues()
        .DeclareExchange("clinic.dead-letter", ex => { ex.ExchangeType = ExchangeType.Direct; ex.IsDurable = true; })
        .DeclareQueue("clinic.dead-letter-queue", q => q.IsDurable = true)
        .BindExchange("clinic.dead-letter").ToQueue("clinic.dead-letter-queue");

    opts.PublishAllMessages().ToRabbitTopics("clinic.events", ex => { ex.ExchangeType = ExchangeType.Topic; ex.IsDurable = true; });

    opts.ListenToRabbitQueue("clinic.commands", q => q.IsDurable = true)
        .CircuitBreaker(cb =>
        {
            cb.MinimumThreshold = 5;
            cb.PauseTime = TimeSpan.FromSeconds(30);
            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
            cb.FailurePercentageThreshold = 50;
        });

    // Error handling
    opts.OnException<TimeoutException>()
        .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
    opts.OnException<InvalidOperationException>()
        .MoveToErrorQueue();

    // Discovery
    opts.Discovery.IncludeAssembly(typeof(Program).Assembly);
});

// ─── HTTP Pipeline ───────────────────────────────────────────────────────────

var app = builder.Build();

app.UsePathBase("/api");
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseResponseCompression();
app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapWolverineEndpoints(opts => opts.UseFluentValidationProblemDetailMiddleware());

app.Run();

// Required for integration test access
public partial class Program { }
