namespace ClinicApi.Infrastructure.Caching;

/// <summary>
/// No-op cache service used when Redis is not configured.
/// All operations are no-ops — the system functions without caching.
/// </summary>
public class NoOpCacheService : ICacheService
{
    public bool IsEndpointCacheable(string endpointName) => false;

    public Task<T?> GetAsync<T>(string endpointName, Guid orgId, Guid entityId, CancellationToken ct = default)
        where T : class => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string endpointName, Guid orgId, Guid entityId, T data, CancellationToken ct = default)
        where T : class => Task.CompletedTask;

    public Task InvalidateAsync(string endpointName, Guid orgId, Guid entityId, CancellationToken ct = default)
        => Task.CompletedTask;
}
