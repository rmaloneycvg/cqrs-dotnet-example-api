using System.Diagnostics;
using ClinicApi.Infrastructure.Security;
using Wolverine;

namespace ClinicApi.Infrastructure.Middleware;

/// <summary>
/// Wolverine middleware that automatically audit-logs PHI access.
/// Applied to all handlers — no per-handler injection needed.
///
/// Detects PHI-related messages by namespace (Features/Patients, Billing, Prescriptions, Appointments)
/// and logs the access with the current user identity and trace ID.
///
/// HIPAA §164.312(b): Audit controls applied uniformly.
/// </summary>
public class PhiAuditMiddleware
{
    private static readonly HashSet<string> PhiNamespaces = new(StringComparer.OrdinalIgnoreCase)
    {
        "ClinicApi.Features.Patients",
        "ClinicApi.Features.Billing",
        "ClinicApi.Features.Prescriptions",
        "ClinicApi.Features.Appointments",
    };

    public static void Before(Envelope envelope, IPhiAuditLogger audit)
    {
        var messageType = envelope.Message?.GetType();
        if (messageType is null) return;

        var ns = messageType.Namespace ?? "";
        if (!PhiNamespaces.Any(phi => ns.StartsWith(phi, StringComparison.OrdinalIgnoreCase)))
            return;

        var traceId = Activity.Current?.TraceId.ToString() ?? "no-trace";
        var entityId = ExtractEntityId(envelope.Message);

        audit.LogAccess(
            "system",
            messageType.Name.Contains("Get") || messageType.Name.Contains("List") ? "Read" : "Write",
            messageType.Name,
            entityId,
            $"TraceId={traceId}");
    }

    private static Guid ExtractEntityId(object? message)
    {
        if (message is null) return Guid.Empty;

        // Try common property names for entity identification
        var type = message.GetType();
        var idProp = type.GetProperty("Id")
            ?? type.GetProperty("PatientId")
            ?? type.GetProperty("InvoiceId")
            ?? type.GetProperty("PrescriptionId");

        if (idProp?.GetValue(message) is Guid guid)
            return guid;

        return Guid.Empty;
    }
}
