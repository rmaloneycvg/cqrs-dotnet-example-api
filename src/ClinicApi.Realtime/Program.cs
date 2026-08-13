using ClinicApi.Realtime.Hubs;
using ClinicApi.Realtime.Infrastructure;
using ClinicApi.Realtime.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using StackExchange.Redis;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("ConnectionStrings:RabbitMq is required.");
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("ConnectionStrings:Redis is required.");

// ─── Redis ───────────────────────────────────────────────────────────────────

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));

// ─── SignalR with Redis Backplane ────────────────────────────────────────────

builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("clinic-realtime");
    });

// ─── Authentication (same JWT validation as Clinic API) ──────────────────────

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];

        // Allow token to be passed as query string for WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/realtime-hub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };

        // Development: disable strict validation when running without real IdP
        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                RequireSignedTokens = false,
            };
        }
    });
builder.Services.AddAuthorization();

// ─── Application Services ────────────────────────────────────────────────────

builder.Services.AddSingleton<ISubscriptionManager, RedisSubscriptionManager>();
builder.Services.AddSingleton<IIdempotencyService, RedisIdempotencyService>();

// PHI Encryption (for decrypting cached data before push)
var phiKey = builder.Configuration["PHI_ENCRYPTION_KEY"]
    ?? throw new InvalidOperationException("PHI_ENCRYPTION_KEY is required.");
builder.Services.AddSingleton<IPhiEncryptionService>(new AesGcmPhiEncryptionService(phiKey));

// ─── Health Checks ───────────────────────────────────────────────────────────

builder.Services.AddHealthChecks()
    .AddRedis(redisConnection, name: "redis");

// ─── Wolverine (RabbitMQ consumer) ───────────────────────────────────────────

builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "ClinicApi.Realtime";

    // RabbitMQ transport — consume from dedicated realtime queue
    opts.UseRabbitMq(new Uri(rabbitMqConnection))
        .AutoProvision()
        .EnableWolverineControlQueues()
        .DeclareExchange("clinic.events", ex =>
        {
            ex.ExchangeType = ExchangeType.Topic;
            ex.IsDurable = true;
        })
        .DeclareQueue("clinic.realtime", q =>
        {
            q.IsDurable = true;
        })
        .BindExchange("clinic.events")
            .ToQueue("clinic.realtime", "ClinicApi.Domain.Events.#");

    // Listen on the realtime queue
    opts.ListenToRabbitQueue("clinic.realtime", q =>
    {
        q.IsDurable = true;
    })
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

// ─── Build App ───────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHub<ClinicHub>("/realtime-hub");

app.Run();
