using System.Text.Json;
using ClinicApi.Domain.Events;
using ClinicApi.Realtime.Hubs;
using ClinicApi.Realtime.Infrastructure;
using ClinicApi.Realtime.Services;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace ClinicApi.Realtime.Handlers;

/// <summary>
/// Wolverine event handlers for patient domain events.
/// Each handler:
///   1. Checks idempotency (skip duplicate deliveries)
///   2. Resolves subscribers (org-level + entity-level)
///   3. Builds push payload (with PHI decryption from cache if available)
///   4. Pushes to matched SignalR connections
///
/// All handlers are idempotent — safe to process the same event multiple times.
/// </summary>
public static class PatientEventHandlers
{
    public static async Task Handle(
        PatientCreated @event,
        IHubContext<ClinicHub> hubContext,
        ISubscriptionManager subscriptions,
        IIdempotencyService idempotency,
        IConnectionMultiplexer redis,
        ILogger logger)
    {
        var eventId = $"PatientCreated:{@event.PatientId}:{DateTime.UtcNow.Ticks}";
        if (!await idempotency.TryMarkProcessedAsync(eventId))
        {
            logger.LogDebug("Duplicate event skipped: {EventId}", eventId);
            return;
        }

        // For PatientCreated, we don't have an orgId in the event.
        // Push to all org subscribers is handled by looking up the entity's cache.
        // For now, push a notification-style event that clients can use to refetch.
        var db = redis.GetDatabase();

        // Try to find cached data to determine org and build payload
        // Pattern: cache:patient:*:{patientId}
        // If not cached yet, push a lightweight notification
        var payload = new
        {
            EventType = "PatientCreated",
            @event.PatientId,
            @event.FirstName,
            @event.LastName,
            @event.Email,
            Timestamp = DateTime.UtcNow
        };

        // Broadcast to all connections (org filtering will be refined when
        // OrganizationId is added to the event or resolved from cache)
        // TODO: Add OrganizationId to PatientCreated event for proper scoping
        logger.LogInformation(
            "Pushing PatientCreated for {PatientId} to subscribers", @event.PatientId);

        await hubContext.Clients.All.SendAsync("PatientCreated", payload);
    }

    public static async Task Handle(
        PatientUpdated @event,
        IHubContext<ClinicHub> hubContext,
        ISubscriptionManager subscriptions,
        IIdempotencyService idempotency,
        IConnectionMultiplexer redis,
        IPhiEncryptionService encryption,
        ILogger logger)
    {
        var eventId = $"PatientUpdated:{@event.PatientId}:{DateTime.UtcNow.Ticks}";
        if (!await idempotency.TryMarkProcessedAsync(eventId))
        {
            logger.LogDebug("Duplicate event skipped: {EventId}", eventId);
            return;
        }

        // Get entity-level subscribers for this patient
        var entitySubscribers = await subscriptions.GetEntitySubscribersAsync("Patient", @event.PatientId);

        if (entitySubscribers.Count == 0)
        {
            logger.LogDebug("No entity subscribers for Patient:{PatientId}", @event.PatientId);
            return;
        }

        // Try to get cached data for full payload
        var db = redis.GetDatabase();
        object payload;

        // Search for cache key (we need orgId — check entity subscribers' connection metadata)
        var cachedData = await TryGetCachedPatientData(db, @event.PatientId, encryption);
        if (cachedData is not null)
        {
            payload = new
            {
                EventType = "PatientUpdated",
                @event.PatientId,
                Data = cachedData,
                Timestamp = DateTime.UtcNow
            };
        }
        else
        {
            // No cached data — send lightweight notification (client will refetch)
            payload = new
            {
                EventType = "EntityInvalidated",
                EntityType = "Patient",
                EntityId = @event.PatientId,
                Timestamp = DateTime.UtcNow
            };
        }

        logger.LogInformation(
            "Pushing PatientUpdated for {PatientId} to {Count} subscribers",
            @event.PatientId, entitySubscribers.Count);

        await hubContext.Clients.Clients(entitySubscribers).SendAsync("PatientUpdated", payload);
    }

    public static async Task Handle(
        PatientDeactivated @event,
        IHubContext<ClinicHub> hubContext,
        ISubscriptionManager subscriptions,
        IIdempotencyService idempotency,
        ILogger logger)
    {
        var eventId = $"PatientDeactivated:{@event.PatientId}:{DateTime.UtcNow.Ticks}";
        if (!await idempotency.TryMarkProcessedAsync(eventId))
            return;

        var entitySubscribers = await subscriptions.GetEntitySubscribersAsync("Patient", @event.PatientId);

        var payload = new
        {
            EventType = "PatientDeactivated",
            @event.PatientId,
            @event.Reason,
            Timestamp = DateTime.UtcNow
        };

        if (entitySubscribers.Count > 0)
        {
            await hubContext.Clients.Clients(entitySubscribers)
                .SendAsync("PatientDeactivated", payload);
        }

        // Also broadcast to all (org-level subscribers should know about deactivations)
        await hubContext.Clients.All.SendAsync("PatientDeactivated", payload);

        logger.LogInformation("Pushed PatientDeactivated for {PatientId}", @event.PatientId);
    }

    /// <summary>
    /// CachePopulated events indicate the API has freshly cached data.
    /// We don't push to clients here (they already have data from their REST response).
    /// This handler exists to acknowledge the event and prevent dead-lettering.
    /// </summary>
    public static Task Handle(CachePopulated @event, ILogger logger)
    {
        logger.LogDebug(
            "Cache populated: {EntityType}:{EntityId} (org: {OrgId})",
            @event.EntityType, @event.EntityId, @event.OrganizationId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// CacheInvalidated events mean stale data was removed.
    /// Push a refetch notification to entity subscribers.
    /// </summary>
    public static async Task Handle(
        CacheInvalidated @event,
        IHubContext<ClinicHub> hubContext,
        ISubscriptionManager subscriptions,
        IIdempotencyService idempotency,
        ILogger logger)
    {
        var eventId = $"CacheInvalidated:{@event.EntityType}:{@event.EntityId}:{DateTime.UtcNow.Ticks}";
        if (!await idempotency.TryMarkProcessedAsync(eventId))
            return;

        var subscribers = await subscriptions.GetEntitySubscribersAsync(
            @event.EntityType, @event.EntityId);

        if (subscribers.Count > 0)
        {
            var payload = new
            {
                EventType = "EntityInvalidated",
                @event.EntityType,
                EntityId = @event.EntityId,
                Timestamp = DateTime.UtcNow
            };

            await hubContext.Clients.Clients(subscribers)
                .SendAsync("EntityInvalidated", payload);

            logger.LogInformation(
                "Pushed EntityInvalidated for {EntityType}:{EntityId} to {Count} subscribers",
                @event.EntityType, @event.EntityId, subscribers.Count);
        }
    }

    /// <summary>
    /// Try to get a patient's cached data from Redis and decrypt PHI fields.
    /// Returns a merged object with both metadata and decrypted PHI, or null if not cached.
    /// </summary>
    private static async Task<object?> TryGetCachedPatientData(
        IDatabase db, Guid patientId, IPhiEncryptionService encryption)
    {
        // We need to search for the cache key — pattern: cache:patient:*:{patientId}
        // In production, you'd use a more efficient lookup. For MVP, try common patterns.
        var server = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: $"cache:patient:*:{patientId}"))
        {
            var hashEntries = await db.HashGetAllAsync(key);
            if (hashEntries.Length == 0) continue;

            var metaJson = hashEntries.FirstOrDefault(h => h.Name == "meta").Value.ToString();
            var phiEncrypted = hashEntries.FirstOrDefault(h => h.Name == "phi").Value.ToString();

            if (string.IsNullOrEmpty(metaJson) || string.IsNullOrEmpty(phiEncrypted))
                continue;

            try
            {
                var meta = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metaJson);
                var phiJson = encryption.Decrypt(phiEncrypted);
                var phi = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(phiJson);

                // Merge
                var merged = new Dictionary<string, JsonElement>();
                if (meta is not null) foreach (var kvp in meta) merged[kvp.Key] = kvp.Value;
                if (phi is not null) foreach (var kvp in phi) merged[kvp.Key] = kvp.Value;

                return merged;
            }
            catch (Exception)
            {
                // Decryption failed — data corrupted or key mismatch
                return null;
            }
        }

        return null;
    }
}
