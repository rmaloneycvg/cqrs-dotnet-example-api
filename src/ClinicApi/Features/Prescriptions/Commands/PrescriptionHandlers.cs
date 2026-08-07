using ClinicApi.Domain.Entities;
using ClinicApi.Domain.Events;
using ClinicApi.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace ClinicApi.Features.Prescriptions.Commands;

public record CreatePrescription(
    Guid PatientId,
    string MedicationName,
    string Dosage,
    string Frequency,
    int TotalRefills,
    DateOnly ExpirationDate,
    string PrescriberName,
    string? Notes = null);

public record CreatePrescriptionResponse(Guid Id, string MedicationName, DateOnly ExpirationDate, int TotalRefills);

public record DiscontinuePrescription(Guid PrescriptionId, string Reason);

/// <summary>
/// Handler for CreatePrescription.
/// Demonstrates Wolverine's scheduled message delivery:
/// - Schedules refill reminders at calculated future dates
/// - Schedules expiration warning 7 days before expiry
/// - All scheduled via IMessageBus.ScheduleAsync()
/// </summary>
public static class CreatePrescriptionHandler
{
    public static async Task<(CreatePrescriptionResponse, PrescriptionCreated)> Handle(
        CreatePrescription command,
        ClinicDbContext db,
        IMessageBus messageBus,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var prescription = new Prescription
        {
            PatientId = command.PatientId,
            MedicationName = command.MedicationName,
            Dosage = command.Dosage,
            Frequency = command.Frequency,
            TotalRefills = command.TotalRefills,
            RefillsRemaining = command.TotalRefills,
            PrescribedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpirationDate = command.ExpirationDate,
            PrescriberName = command.PrescriberName,
            Notes = command.Notes
        };

        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync(cancellationToken);

        // Schedule refill reminders
        // Assuming monthly refills, schedule reminders 3 days before each refill is due
        var refillInterval = (command.ExpirationDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow) / (command.TotalRefills + 1);
        for (var i = 1; i <= command.TotalRefills; i++)
        {
            var refillDate = DateTime.UtcNow + (refillInterval * i) - TimeSpan.FromDays(3);
            if (refillDate > DateTime.UtcNow)
            {
                var reminder = new PrescriptionRefillReminder(
                    prescription.Id,
                    command.PatientId,
                    command.MedicationName,
                    command.TotalRefills - i);

                await messageBus.ScheduleAsync(reminder, refillDate);

                logger.LogInformation(
                    "Scheduled refill reminder for prescription {PrescriptionId} at {RefillDate}",
                    prescription.Id, refillDate);
            }
        }

        // Schedule expiration warning 7 days before expiry
        var expirationWarningDate = command.ExpirationDate.ToDateTime(TimeOnly.MinValue) - TimeSpan.FromDays(7);
        if (expirationWarningDate > DateTime.UtcNow)
        {
            var warning = new PrescriptionExpiringWarning(
                prescription.Id,
                command.PatientId,
                command.MedicationName,
                command.ExpirationDate);

            await messageBus.ScheduleAsync(warning, expirationWarningDate);

            logger.LogInformation(
                "Scheduled expiration warning for prescription {PrescriptionId} at {WarningDate}",
                prescription.Id, expirationWarningDate);
        }

        var response = new CreatePrescriptionResponse(
            prescription.Id, prescription.MedicationName, prescription.ExpirationDate, prescription.TotalRefills);
        var @event = new PrescriptionCreated(
            prescription.Id, command.PatientId, command.MedicationName, command.ExpirationDate);

        return (response, @event);
    }
}

/// <summary>
/// Handler for DiscontinuePrescription.
/// Demonstrates cancellation of scheduled messages when a prescription is stopped.
/// Note: Wolverine doesn't support direct cancellation of scheduled messages by default,
/// so the handlers check prescription status before processing.
/// </summary>
public static class DiscontinuePrescriptionHandler
{
    public static async Task<PrescriptionDiscontinued> Handle(
        DiscontinuePrescription command,
        ClinicDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var prescription = await db.Prescriptions.FindAsync([command.PrescriptionId], cancellationToken)
            ?? throw new InvalidOperationException($"Prescription {command.PrescriptionId} not found");

        prescription.Status = PrescriptionStatus.Discontinued;
        prescription.UpdatedAt = DateTime.UtcNow;
        prescription.Notes = $"{prescription.Notes}\nDiscontinued: {command.Reason}";

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Prescription {PrescriptionId} discontinued. Future scheduled messages will be ignored.",
            command.PrescriptionId);

        return new PrescriptionDiscontinued(prescription.Id, prescription.PatientId, command.Reason);
    }
}

/// <summary>
/// Handlers for scheduled prescription messages.
/// These check if the prescription is still active before processing.
/// This is the idempotent pattern for handling scheduled messages that may have been cancelled.
/// </summary>
public static class PrescriptionScheduledMessageHandlers
{
    public static async Task Handle(
        PrescriptionRefillReminder reminder,
        ClinicDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var prescription = await db.Prescriptions.FindAsync([reminder.PrescriptionId], cancellationToken);

        // If prescription no longer active, ignore the scheduled message
        if (prescription is null || prescription.Status != PrescriptionStatus.Active)
        {
            logger.LogInformation(
                "Ignoring refill reminder for prescription {PrescriptionId} — no longer active",
                reminder.PrescriptionId);
            return;
        }

        // In production: send notification (email, SMS, push)
        logger.LogInformation(
            "REFILL REMINDER: Patient {PatientId} — {Medication} has {RefillsRemaining} refills remaining",
            reminder.PatientId, reminder.MedicationName, reminder.RefillsRemaining);
    }

    public static async Task Handle(
        PrescriptionExpiringWarning warning,
        ClinicDbContext db,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var prescription = await db.Prescriptions.FindAsync([warning.PrescriptionId], cancellationToken);

        if (prescription is null || prescription.Status != PrescriptionStatus.Active)
        {
            logger.LogInformation(
                "Ignoring expiration warning for prescription {PrescriptionId} — no longer active",
                warning.PrescriptionId);
            return;
        }

        // In production: send urgent notification
        logger.LogWarning(
            "EXPIRATION WARNING: Patient {PatientId} — {Medication} expires on {ExpirationDate}",
            warning.PatientId, warning.MedicationName, warning.ExpirationDate);
    }
}
