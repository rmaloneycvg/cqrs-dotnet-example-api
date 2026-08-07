using ClinicApi.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

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



// Dapper connection factory (read-side)
builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));

// Bulk insert service
builder.Services.AddSingleton<IBulkInsertService>(new SqlBulkInsertService(connectionString));

// FluentValidation - auto-register validators from this assembly
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ClinicDbContext>("mssql");

// Wolverine HTTP endpoint discovery with FluentValidation
builder.Services.AddWolverineHttp();

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

    // Custom middleware
    opts.Policies.AddMiddleware(typeof(ClinicApi.Infrastructure.Middleware.LoggingMiddleware));
    opts.Policies.AddMiddleware(typeof(ClinicApi.Infrastructure.Middleware.PerformanceMiddleware));
});

var app = builder.Build();

// Middleware
app.UseHealthChecks("/health");

// Wolverine HTTP endpoints
app.MapWolverineEndpoints(opts =>
{
    opts.UseFluentValidationProblemDetailMiddleware();
});

app.Run();

// Required for integration test access
public partial class Program { }
