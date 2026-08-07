using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace ClinicApi.Infrastructure.Middleware;

/// <summary>
/// Wolverine middleware for structured logging and performance tracking of all handler invocations.
/// Wolverine middleware uses the Before/After naming convention.
/// The middleware is code-generated into the handler pipeline at compile time.
/// 
/// Thread-safety: uses Stopwatch.GetTimestamp() instead of instance-level Stopwatch
/// to avoid corruption under concurrent handler execution.
/// 
/// Combines logging and performance tracking in a single middleware to avoid
/// Wolverine code-generation variable collisions with multiple Before/After middlewares.
/// </summary>
public class LoggingMiddleware
{
    private const double SlowHandlerThresholdMs = 500;
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public long Before(Envelope envelope)
    {
        _logger.LogInformation(
            "Handling message {MessageType} (Id: {MessageId}, CorrelationId: {CorrelationId})",
            envelope.MessageType,
            envelope.Id,
            envelope.CorrelationId);
        return Stopwatch.GetTimestamp();
    }

    public void After(Envelope envelope, long startTimestamp)
    {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        _logger.LogInformation(
            "Handled message {MessageType} in {ElapsedMs:F1}ms (Id: {MessageId})",
            envelope.MessageType,
            elapsed.TotalMilliseconds,
            envelope.Id);

        if (elapsed.TotalMilliseconds > SlowHandlerThresholdMs)
        {
            _logger.LogWarning(
                "Slow handler detected: {MessageType} took {ElapsedMs:F1}ms (Id: {MessageId})",
                envelope.MessageType,
                elapsed.TotalMilliseconds,
                envelope.Id);
        }
    }
}
