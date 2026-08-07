using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace ClinicApi.Infrastructure.Middleware;

/// <summary>
/// Wolverine middleware for structured logging of all handler invocations.
/// Wolverine middleware uses the Before/After naming convention.
/// The middleware is code-generated into the handler pipeline at compile time.
/// 
/// Thread-safety: uses Stopwatch.GetTimestamp() instead of instance-level Stopwatch
/// to avoid corruption under concurrent handler execution.
/// </summary>
public class LoggingMiddleware
{
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
    }
}

/// <summary>
/// Performance tracking middleware that logs slow handlers.
/// Emits warnings for handlers that exceed 500ms.
/// 
/// Thread-safety: uses Stopwatch.GetTimestamp() instead of instance-level Stopwatch.
/// </summary>
public class PerformanceMiddleware
{
    private readonly ILogger<PerformanceMiddleware> _logger;

    public PerformanceMiddleware(ILogger<PerformanceMiddleware> logger)
    {
        _logger = logger;
    }

    public long Before(Envelope envelope)
    {
        return Stopwatch.GetTimestamp();
    }

    public void After(Envelope envelope, long startTimestamp)
    {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        if (elapsed.TotalMilliseconds > 500)
        {
            _logger.LogWarning(
                "Slow handler detected: {MessageType} took {ElapsedMs:F1}ms (Id: {MessageId})",
                envelope.MessageType,
                elapsed.TotalMilliseconds,
                envelope.Id);
        }
    }
}
