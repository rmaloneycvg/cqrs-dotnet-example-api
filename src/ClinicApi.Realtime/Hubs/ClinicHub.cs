using System.Security.Claims;
using ClinicApi.Realtime.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClinicApi.Realtime.Hubs;

/// <summary>
/// SignalR hub for real-time clinic data push.
///
/// Subscription model:
///   - On connect: automatically subscribed to all events for the user's organization (from JWT org_id claim)
///   - Explicit: client calls SubscribeToPatient/UnsubscribeFromPatient for entity-level notifications
///
/// Authentication:
///   - JWT token passed as query parameter (WebSocket doesn't support Authorization header)
///   - Same token validation as Clinic API
///
/// Push events sent to clients:
///   - "PatientCreated" → { patientId, firstName, lastName, ... }
///   - "PatientUpdated" → { patientId, firstName, lastName, ... }
///   - "PatientDeactivated" → { patientId, reason }
///   - "EntityUpdated" → { entityType, entityId, data? } (generic for other entity types)
///   - "EntityInvalidated" → { entityType, entityId } (client should refetch)
/// </summary>
[Authorize]
public class ClinicHub : Hub
{
    private readonly ISubscriptionManager _subscriptions;
    private readonly ILogger<ClinicHub> _logger;

    public ClinicHub(ISubscriptionManager subscriptions, ILogger<ClinicHub> logger)
    {
        _subscriptions = subscriptions;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value
            ?? "anonymous";

        var orgIdClaim = Context.User?.FindFirst("org_id")?.Value;
        var orgId = Guid.TryParse(orgIdClaim, out var parsedOrg)
            ? parsedOrg
            : Guid.Parse("00000000-0000-0000-0000-000000000001"); // Dev default

        await _subscriptions.RegisterConnectionAsync(Context.ConnectionId, userId, orgId);

        _logger.LogInformation(
            "Client connected: {ConnectionId} (user: {UserId}, org: {OrgId})",
            Context.ConnectionId, userId, orgId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _subscriptions.RemoveConnectionAsync(Context.ConnectionId);

        _logger.LogInformation(
            "Client disconnected: {ConnectionId} (reason: {Reason})",
            Context.ConnectionId, exception?.Message ?? "normal");

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client subscribes to a specific patient's updates.
    /// Called when user navigates to patient detail view.
    /// </summary>
    public async Task SubscribeToPatient(Guid patientId)
    {
        await _subscriptions.SubscribeToEntityAsync(Context.ConnectionId, "Patient", patientId);

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to Patient:{PatientId}",
            Context.ConnectionId, patientId);
    }

    /// <summary>
    /// Client unsubscribes from a specific patient's updates.
    /// Called when user navigates away from patient detail view.
    /// </summary>
    public async Task UnsubscribeFromPatient(Guid patientId)
    {
        await _subscriptions.UnsubscribeFromEntityAsync(Context.ConnectionId, "Patient", patientId);

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from Patient:{PatientId}",
            Context.ConnectionId, patientId);
    }

    /// <summary>
    /// Generic entity subscription (for future appointment, billing, etc. views).
    /// </summary>
    public async Task SubscribeToEntity(string entityType, Guid entityId)
    {
        await _subscriptions.SubscribeToEntityAsync(Context.ConnectionId, entityType, entityId);
    }

    /// <summary>
    /// Generic entity unsubscription.
    /// </summary>
    public async Task UnsubscribeFromEntity(string entityType, Guid entityId)
    {
        await _subscriptions.UnsubscribeFromEntityAsync(Context.ConnectionId, entityType, entityId);
    }
}
