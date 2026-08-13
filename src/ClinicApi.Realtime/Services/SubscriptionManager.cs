using System.Text.Json;
using StackExchange.Redis;

namespace ClinicApi.Realtime.Services;

/// <summary>
/// Manages client subscriptions in Redis.
///
/// Subscription model:
///   - Org-scoped: All clients in an org receive events for that org
///   - Entity-specific: Client explicitly subscribes to a patient/appointment/etc.
///
/// Redis data structures:
///   sub:org:{orgId}                    → SET of connectionIds
///   sub:entity:{type}:{entityId}       → SET of connectionIds
///   conn:{connectionId}                → HASH { userId, orgId, entities: JSON array }
/// </summary>
public interface ISubscriptionManager
{
    /// <summary>Register a new connection with org-level subscription.</summary>
    Task RegisterConnectionAsync(string connectionId, string userId, Guid orgId, CancellationToken ct = default);

    /// <summary>Add an entity-level subscription for a connection.</summary>
    Task SubscribeToEntityAsync(string connectionId, string entityType, Guid entityId, CancellationToken ct = default);

    /// <summary>Remove an entity-level subscription for a connection.</summary>
    Task UnsubscribeFromEntityAsync(string connectionId, string entityType, Guid entityId, CancellationToken ct = default);

    /// <summary>Clean up all subscriptions for a disconnected connection.</summary>
    Task RemoveConnectionAsync(string connectionId, CancellationToken ct = default);

    /// <summary>Get all connection IDs subscribed to a given org (org-level events).</summary>
    Task<IReadOnlyList<string>> GetOrgSubscribersAsync(Guid orgId, CancellationToken ct = default);

    /// <summary>Get all connection IDs subscribed to a specific entity.</summary>
    Task<IReadOnlyList<string>> GetEntitySubscribersAsync(string entityType, Guid entityId, CancellationToken ct = default);

    /// <summary>
    /// Resolve all unique connection IDs that should receive an event
    /// (union of org-level + entity-level subscribers).
    /// </summary>
    Task<IReadOnlyList<string>> ResolveSubscribersAsync(Guid orgId, string entityType, Guid entityId, CancellationToken ct = default);
}

public class RedisSubscriptionManager : ISubscriptionManager
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSubscriptionManager> _logger;

    public RedisSubscriptionManager(IConnectionMultiplexer redis, ILogger<RedisSubscriptionManager> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task RegisterConnectionAsync(string connectionId, string userId, Guid orgId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // Add to org subscription set
        await db.SetAddAsync(OrgKey(orgId), connectionId);

        // Store connection metadata
        await db.HashSetAsync(ConnKey(connectionId), [
            new HashEntry("userId", userId),
            new HashEntry("orgId", orgId.ToString()),
            new HashEntry("entities", "[]"),
        ]);

        // Connection metadata expires after 24h (stale connection cleanup)
        await db.KeyExpireAsync(ConnKey(connectionId), TimeSpan.FromHours(24));

        _logger.LogDebug("Registered connection {ConnectionId} for org {OrgId}", connectionId, orgId);
    }

    public async Task SubscribeToEntityAsync(string connectionId, string entityType, Guid entityId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // Add connection to entity subscription set
        await db.SetAddAsync(EntityKey(entityType, entityId), connectionId);

        // Update connection's entity list
        var entitiesJson = await db.HashGetAsync(ConnKey(connectionId), "entities");
        var entities = DeserializeEntities(entitiesJson);
        var entityRef = $"{entityType}:{entityId}";
        if (!entities.Contains(entityRef))
        {
            entities.Add(entityRef);
            await db.HashSetAsync(ConnKey(connectionId), "entities", JsonSerializer.Serialize(entities));
        }

        _logger.LogDebug("Connection {ConnectionId} subscribed to {EntityType}:{EntityId}",
            connectionId, entityType, entityId);
    }

    public async Task UnsubscribeFromEntityAsync(string connectionId, string entityType, Guid entityId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // Remove connection from entity subscription set
        await db.SetRemoveAsync(EntityKey(entityType, entityId), connectionId);

        // Update connection's entity list
        var entitiesJson = await db.HashGetAsync(ConnKey(connectionId), "entities");
        var entities = DeserializeEntities(entitiesJson);
        entities.Remove($"{entityType}:{entityId}");
        await db.HashSetAsync(ConnKey(connectionId), "entities", JsonSerializer.Serialize(entities));

        _logger.LogDebug("Connection {ConnectionId} unsubscribed from {EntityType}:{EntityId}",
            connectionId, entityType, entityId);
    }

    public async Task RemoveConnectionAsync(string connectionId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // Get connection metadata to find org and entity subscriptions
        var connData = await db.HashGetAllAsync(ConnKey(connectionId));
        if (connData.Length == 0) return;

        var orgIdStr = connData.FirstOrDefault(h => h.Name == "orgId").Value.ToString();
        var entitiesJson = connData.FirstOrDefault(h => h.Name == "entities").Value.ToString();

        // Remove from org set
        if (Guid.TryParse(orgIdStr, out var orgId))
        {
            await db.SetRemoveAsync(OrgKey(orgId), connectionId);
        }

        // Remove from all entity sets
        var entities = DeserializeEntities(entitiesJson);
        foreach (var entityRef in entities)
        {
            var parts = entityRef.Split(':');
            if (parts.Length == 2)
            {
                await db.SetRemoveAsync($"sub:entity:{entityRef}", connectionId);
            }
        }

        // Delete connection metadata
        await db.KeyDeleteAsync(ConnKey(connectionId));

        _logger.LogDebug("Removed connection {ConnectionId} (org: {OrgId}, entities: {Count})",
            connectionId, orgIdStr, entities.Count);
    }

    public async Task<IReadOnlyList<string>> GetOrgSubscribersAsync(Guid orgId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var members = await db.SetMembersAsync(OrgKey(orgId));
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task<IReadOnlyList<string>> GetEntitySubscribersAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var members = await db.SetMembersAsync(EntityKey(entityType, entityId));
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task<IReadOnlyList<string>> ResolveSubscribersAsync(Guid orgId, string entityType, Guid entityId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();

        // Union of org-level and entity-level subscribers
        var orgMembers = await db.SetMembersAsync(OrgKey(orgId));
        var entityMembers = await db.SetMembersAsync(EntityKey(entityType, entityId));

        var combined = new HashSet<string>();
        foreach (var m in orgMembers) combined.Add(m.ToString());
        foreach (var m in entityMembers) combined.Add(m.ToString());

        return combined.ToList();
    }

    private static string OrgKey(Guid orgId) => $"sub:org:{orgId}";
    private static string EntityKey(string entityType, Guid entityId) => $"sub:entity:{entityType}:{entityId}";
    private static string ConnKey(string connectionId) => $"conn:{connectionId}";

    private static List<string> DeserializeEntities(RedisValue value)
    {
        if (value.IsNullOrEmpty) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(value.ToString()) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
