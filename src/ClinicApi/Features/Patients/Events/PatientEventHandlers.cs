using ClinicApi.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ClinicApi.Features.Patients.Events;

/// <summary>
/// Wolverine event handlers for Patient domain events.
/// Auto-discovered via opts.Discovery.IncludeAssembly().
/// 
/// These handlers run when events are published locally (via InvokeAsync/PublishAsync)
/// or received from RabbitMQ. Extend with side effects as needed
/// (notifications, audit logs, read-model projections, etc.)
/// </summary>
public static class PatientEventHandlers
{
    public static void Handle(PatientCreated @event, ILogger logger)
    {
        logger.LogInformation(
            "Patient created: {PatientId} ({FirstName} {LastName}, {Email})",
            @event.PatientId, @event.FirstName, @event.LastName, @event.Email);
    }

    public static void Handle(PatientUpdated @event, ILogger logger)
    {
        logger.LogInformation(
            "Patient updated: {PatientId} ({FirstName} {LastName})",
            @event.PatientId, @event.FirstName, @event.LastName);
    }

    public static void Handle(PatientDeactivated @event, ILogger logger)
    {
        logger.LogInformation(
            "Patient deactivated: {PatientId} (Reason: {Reason})",
            @event.PatientId, @event.Reason);
    }

    public static void Handle(PatientsBatchImported @event, ILogger logger)
    {
        logger.LogInformation(
            "Batch import completed: {SuccessCount}/{TotalCount} succeeded, {FailureCount} failed",
            @event.SuccessCount, @event.TotalCount, @event.FailureCount);
    }
}
