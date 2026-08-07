using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace ClinicApi.Infrastructure.Middleware;

/// <summary>
/// Wolverine middleware for structured logging of all handler invocations.
/// Wolverine middleware uses the Before/After naming convention.
/// The middleware is code-generated into the handler pipeline at compile time.
/// </summary>
public class LoggingMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly Stopwatch _stopwatch = new();

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public void Before(Envelope envelope)
    {
        _stopwatch.Restart();
        _logger.LogInformation(
            "Handling message {MessageType} (Id: {MessageId}, CorrelationId: {CorrelationId})",
            envelope.MessageType,
            envelope.Id,
            envelope.CorrelationId);
    }

    public void After(Envelope envelope)
    {
        _stopwatch.Stop();
        _logger.LogInformation(
            "Handled message {MessageType} in {ElapsedMs}ms (Id: {MessageId})",
            envelope.MessageType,
            _stopwatch.ElapsedMilliseconds,
            envelope.Id);
    }
}

/// <summary>
/// Performance tracking middleware that logs slow handlers.
/// Emits warnings for handlers that exceed 500ms.
/// </summary>
public class PerformanceMiddleware
{
    private readonly ILogger<PerformanceMiddleware> _logger;
    private readonly Stopwatch _stopwatch = new();

    public PerformanceMiddleware(ILogger<PerformanceMiddleware> logger)
    {
        _logger = logger;
    }

    public void Before(Envelope envelope)
    {
        _stopwatch.Restart();
    }

    public void After(Envelope envelope)
    {
        _stopwatch.Stop();

        if (_stopwatch.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "Slow handler detected: {MessageType} took {ElapsedMs}ms (Id: {MessageId})",
                envelope.MessageType,
                _stopwatch.ElapsedMilliseconds,
                envelope.Id);
        }
    }
}
