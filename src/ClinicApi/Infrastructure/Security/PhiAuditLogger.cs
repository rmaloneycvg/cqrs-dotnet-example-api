using System.Security.Claims;

namespace ClinicApi.Infrastructure.Security;

/// <summary>
/// HIPAA §164.312(b): Audit controls — record and examine activity in systems
/// that contain or use ePHI.
///
/// Logs PHI access events with the authenticated user's identity.
/// In production, these route to an immutable audit store (CloudWatch, Azure Monitor)
/// with retention per organizational policy (6 years for HIPAA).
/// </summary>
public interface IPhiAuditLogger
{
    void LogAccess(string userId, string action, string entityType, Guid entityId, string? details = null);
    void LogModification(string userId, string action, string entityType, Guid entityId, string[] fieldsModified);
    void LogBulkAccess(string userId, string action, string entityType, int recordCount);
}

public class PhiAuditLogger : IPhiAuditLogger
{
    private readonly ILogger<PhiAuditLogger> _logger;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public PhiAuditLogger(ILogger<PhiAuditLogger> logger, IHttpContextAccessor? httpContextAccessor = null)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetCurrentUserId()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value
            ?? "anonymous";
    }

    public void LogAccess(string userId, string action, string entityType, Guid entityId, string? details = null)
    {
        var resolvedUser = userId == "system" ? GetCurrentUserId() : userId;
        _logger.LogInformation(
            "PHI_AUDIT: User={UserId} Action={Action} Entity={EntityType} Id={EntityId} Details={Details}",
            resolvedUser, action, entityType, entityId, details ?? "none");
    }

    public void LogModification(string userId, string action, string entityType, Guid entityId, string[] fieldsModified)
    {
        var resolvedUser = userId == "system" ? GetCurrentUserId() : userId;
        _logger.LogInformation(
            "PHI_AUDIT: User={UserId} Action={Action} Entity={EntityType} Id={EntityId} Fields={Fields}",
            resolvedUser, action, entityType, entityId, string.Join(",", fieldsModified));
    }

    public void LogBulkAccess(string userId, string action, string entityType, int recordCount)
    {
        var resolvedUser = userId == "system" ? GetCurrentUserId() : userId;
        _logger.LogInformation(
            "PHI_AUDIT: User={UserId} Action={Action} Entity={EntityType} RecordCount={RecordCount}",
            resolvedUser, action, entityType, recordCount);
    }
}
