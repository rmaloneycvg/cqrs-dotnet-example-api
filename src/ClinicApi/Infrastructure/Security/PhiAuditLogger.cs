namespace ClinicApi.Infrastructure.Security;

/// <summary>
/// HIPAA §164.312(b): Audit controls — record and examine activity in systems
/// that contain or use ePHI.
///
/// Logs PHI access events to a structured log sink. In production, these should
/// route to an immutable audit store (CloudWatch, Azure Monitor, or dedicated SIEM)
/// with retention per organizational policy (typically 6 years for HIPAA).
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

    public PhiAuditLogger(ILogger<PhiAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogAccess(string userId, string action, string entityType, Guid entityId, string? details = null)
    {
        _logger.LogInformation(
            "PHI_AUDIT: User={UserId} Action={Action} Entity={EntityType} Id={EntityId} Details={Details}",
            userId, action, entityType, entityId, details ?? "none");
    }

    public void LogModification(string userId, string action, string entityType, Guid entityId, string[] fieldsModified)
    {
        _logger.LogInformation(
            "PHI_AUDIT: User={UserId} Action={Action} Entity={EntityType} Id={EntityId} Fields={Fields}",
            userId, action, entityType, entityId, string.Join(",", fieldsModified));
    }

    public void LogBulkAccess(string userId, string action, string entityType, int recordCount)
    {
        _logger.LogInformation(
            "PHI_AUDIT: User={UserId} Action={Action} Entity={EntityType} RecordCount={RecordCount}",
            userId, action, entityType, recordCount);
    }
}
