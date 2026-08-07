using ClinicApi.Domain.Entities;
using ClinicApi.Domain.Events;
using ClinicApi.Domain.ValueObjects;
using ClinicApi.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace ClinicApi.Features.Billing.Events;

/// <summary>
/// Cross-feature event handler: when a patient is created,
/// automatically create a billing account (invoice placeholder).
/// 
/// This demonstrates RabbitMQ pub/sub — PatientCreated is published
/// from the Patients feature and consumed here in Billing.
/// Wolverine routes via the topic exchange configuration.
/// </summary>
public static class PatientCreatedBillingHandler
{
    public static void Handle(
        PatientCreated @event,
        ClinicDbContext db,
        ILogger logger)
    {
        logger.LogInformation(
            "Billing: Setting up billing account for new patient {PatientId} ({Name})",
            @event.PatientId, $"{@event.FirstName} {@event.LastName}");

        // In a real system, this might create a billing profile, set up payment methods, etc.
        // For this demo, we log the cross-feature event handling.
    }
}

/// <summary>
/// Handler for InvoiceOverdue events — sends reminder notifications.
/// Demonstrates scheduled message trigger → event → cross-cutting handler.
/// </summary>
public static class InvoiceOverdueHandler
{
    public static void Handle(
        InvoiceOverdue @event,
        ILogger logger)
    {
        logger.LogWarning(
            "OVERDUE NOTICE: Invoice {InvoiceId} for patient {PatientId} is {DaysOverdue} days overdue",
            @event.InvoiceId, @event.PatientId, @event.DaysOverdue);

        // In production: send email/SMS notification, escalate to collections, etc.
    }
}
