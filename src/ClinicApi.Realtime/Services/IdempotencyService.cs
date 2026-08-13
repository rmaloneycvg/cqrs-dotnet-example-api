using StackExchange.Redis;

namespace ClinicApi.Realtime.Services;

/// <summary>
/// Idempotency service for RabbitMQ event deduplication.
/// At-least-once delivery means the same event may arrive multiple times.
/// Track processed event IDs in Redis with a short TTL to prevent duplicate processing.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Try to mark an event as processed. Returns true if this is the first time
    /// (proceed with processing). Returns false if already processed (skip).
    /// </summary>
    Task<bool> TryMarkProcessedAsync(string eventId, CancellationToken ct = default);
}

public class RedisIdempotencyService : IIdempotencyService
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromMinutes(5);

    public RedisIdempotencyService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> TryMarkProcessedAsync(string eventId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var key = $"idemp:{eventId}";

        // SET NX (set if not exists) + EX (expire) — atomic operation
        var wasSet = await db.StringSetAsync(key, "1", IdempotencyTtl, When.NotExists);
        return wasSet;
    }
}
