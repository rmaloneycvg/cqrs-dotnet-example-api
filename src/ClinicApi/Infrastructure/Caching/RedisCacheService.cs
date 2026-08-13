using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ClinicApi.Infrastructure.Caching;

/// <summary>
/// Cache service interface for Redis-backed caching with PHI encryption.
/// Consumers provide the endpoint name, entity ID, and org ID — the service
/// resolves the key pattern from configuration and handles encryption.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a cached entity. Returns null on cache miss.
    /// PHI fields are decrypted transparently.
    /// </summary>
    Task<T?> GetAsync<T>(string endpointName, Guid orgId, Guid entityId, CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Store an entity in cache with PHI fields encrypted.
    /// Only stores if the endpoint is configured as cacheable.
    /// </summary>
    Task SetAsync<T>(string endpointName, Guid orgId, Guid entityId, T data, CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Invalidate a cached entity (e.g., after mutation).
    /// </summary>
    Task InvalidateAsync(string endpointName, Guid orgId, Guid entityId, CancellationToken ct = default);

    /// <summary>
    /// Check if an endpoint is configured for caching.
    /// </summary>
    bool IsEndpointCacheable(string endpointName);
}

/// <summary>
/// Redis-backed cache service with PHI encryption.
///
/// Data is stored as a Redis hash with two fields:
///   - "meta" → plaintext JSON of non-PHI fields (id, orgId, status, timestamps)
///   - "phi"  → AES-256-GCM encrypted JSON of PHI fields (names, email, phone, dob, address)
///
/// This split allows the SignalR service to read metadata for routing/subscription
/// matching without needing decryption keys (defense in depth).
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IPhiEncryptionService _encryption;
    private readonly CachingOptions _options;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IPhiEncryptionService encryption,
        IOptions<CachingOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _encryption = encryption;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEndpointCacheable(string endpointName)
    {
        if (!_options.Enabled) return false;
        return _options.Endpoints.TryGetValue(endpointName, out var config) && config.Enabled;
    }

    public async Task<T?> GetAsync<T>(string endpointName, Guid orgId, Guid entityId, CancellationToken ct = default)
        where T : class
    {
        if (!IsEndpointCacheable(endpointName)) return null;

        var key = ResolveKey(endpointName, orgId, entityId);
        var db = _redis.GetDatabase();

        var values = await db.HashGetAllAsync(key);
        if (values.Length == 0)
        {
            _logger.LogDebug("Cache miss: {Key}", key);
            return null;
        }

        _logger.LogDebug("Cache hit: {Key}", key);

        var metaJson = values.FirstOrDefault(v => v.Name == "meta").Value.ToString();
        var phiEncrypted = values.FirstOrDefault(v => v.Name == "phi").Value.ToString();

        if (string.IsNullOrEmpty(metaJson) || string.IsNullOrEmpty(phiEncrypted))
            return null;

        // Merge meta (plaintext) and phi (decrypted) into a single object
        var metaDoc = JsonSerializer.Deserialize<JsonElement>(metaJson);
        var phiJson = _encryption.Decrypt(phiEncrypted);
        var phiDoc = JsonSerializer.Deserialize<JsonElement>(phiJson);

        // Merge both JSON objects
        var merged = MergeJsonElements(metaDoc, phiDoc);
        return JsonSerializer.Deserialize<T>(merged);
    }

    public async Task SetAsync<T>(string endpointName, Guid orgId, Guid entityId, T data, CancellationToken ct = default)
        where T : class
    {
        if (!IsEndpointCacheable(endpointName)) return;

        var key = ResolveKey(endpointName, orgId, entityId);
        var config = _options.Endpoints[endpointName];
        var db = _redis.GetDatabase();

        // Split data into PHI and non-PHI fields
        var (metaJson, phiJson) = SplitPhiFields(data);
        var phiEncrypted = _encryption.Encrypt(phiJson);

        var hashEntries = new HashEntry[]
        {
            new("meta", metaJson),
            new("phi", phiEncrypted),
        };

        await db.HashSetAsync(key, hashEntries);
        await db.KeyExpireAsync(key, TimeSpan.FromSeconds(config.TtlSeconds));

        _logger.LogDebug("Cache set: {Key} (TTL: {Ttl}s)", key, config.TtlSeconds);
    }

    public async Task InvalidateAsync(string endpointName, Guid orgId, Guid entityId, CancellationToken ct = default)
    {
        if (!IsEndpointCacheable(endpointName)) return;

        var key = ResolveKey(endpointName, orgId, entityId);
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);

        _logger.LogInformation("Cache invalidated: {Key}", key);
    }

    private string ResolveKey(string endpointName, Guid orgId, Guid entityId)
    {
        var pattern = _options.Endpoints[endpointName].KeyPattern;
        return pattern
            .Replace("{orgId}", orgId.ToString())
            .Replace("{Id}", entityId.ToString());
    }

    /// <summary>
    /// Split an entity into PHI fields (encrypted) and metadata fields (plaintext).
    /// PHI fields: FirstName, LastName, Email, PhoneNumber, Phone, DateOfBirth, Address*
    /// Metadata: everything else (Id, OrganizationId, Status, timestamps, etc.)
    /// </summary>
    private static (string metaJson, string phiJson) SplitPhiFields<T>(T data)
    {
        var json = JsonSerializer.Serialize(data);
        var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

        var phiFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FirstName", "LastName", "Email", "PhoneNumber", "Phone",
            "DateOfBirth", "Address", "AddressCity", "AddressState",
            "AddressStreet", "AddressZipCode", "City", "State",
            "Street", "ZipCode", "FullName"
        };

        var metaFields = new Dictionary<string, JsonElement>();
        var phiFields = new Dictionary<string, JsonElement>();

        foreach (var kvp in doc)
        {
            if (phiFieldNames.Contains(kvp.Key))
                phiFields[kvp.Key] = kvp.Value;
            else
                metaFields[kvp.Key] = kvp.Value;
        }

        return (
            JsonSerializer.Serialize(metaFields),
            JsonSerializer.Serialize(phiFields)
        );
    }

    private static string MergeJsonElements(JsonElement meta, JsonElement phi)
    {
        var merged = new Dictionary<string, JsonElement>();

        if (meta.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in meta.EnumerateObject())
                merged[prop.Name] = prop.Value;
        }

        if (phi.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in phi.EnumerateObject())
                merged[prop.Name] = prop.Value;
        }

        return JsonSerializer.Serialize(merged);
    }
}
