using ClinicApi.Domain.Entities;
using ClinicApi.Domain.Events;
using ClinicApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace ClinicApi.Features.Appointments.Saga;

/// <summary>
/// Wolverine Saga for multi-step appointment scheduling.
/// 
/// Flow:
///   ScheduleAppointment (start) → check availability
///   AvailabilityConfirmed → book the slot
///   SlotBooked → send confirmation notification
///   AppointmentConfirmed → mark complete (saga ends)
/// 
/// Compensation:
///   AvailabilityDenied → notify patient, cancel
///   BookingFailed → release hold, notify patient
///   AppointmentTimedOut → cancel everything (15 min timeout)
/// 
/// Wolverine persists saga state in SQL Server automatically.
/// The saga correlates messages via the Id property.
/// </summary>
public class AppointmentSchedulingSaga : Wolverine.Saga
{
    // Saga identity — Wolverine uses this for correlation
    public Guid Id { get; set; }

    // Saga state
    public Guid PatientId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public AppointmentStatus CurrentStatus { get; set; } = AppointmentStatus.Requested;
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Start handler: triggered when a ScheduleAppointment command arrives.
    /// Returns cascaded messages: an availability check event AND a timeout message.
    /// </summary>
    public static (AppointmentSchedulingSaga, AppointmentRequested, ScheduledMessage<AppointmentTimedOut>) Start(
        ScheduleAppointment command)
    {
        var sagaId = Guid.NewGuid();

        var saga = new AppointmentSchedulingSaga
        {
            Id = sagaId,
            PatientId = command.PatientId,
            ProviderName = command.ProviderName,
            ScheduledAt = command.ScheduledAt,
            CurrentStatus = AppointmentStatus.Requested,
            StartedAt = DateTime.UtcNow
        };

        var @event = new AppointmentRequested(sagaId, command.PatientId, command.ProviderName, command.ScheduledAt);

        // Schedule a timeout message for 15 minutes — if saga hasn't completed by then, compensate
        var timeout = new ScheduledMessage<AppointmentTimedOut>(
            new AppointmentTimedOut(sagaId, command.PatientId),
            TimeSpan.FromMinutes(15));

        return (saga, @event, timeout);
    }

    /// <summary>
    /// Handle availability confirmation — proceed to booking.
    /// </summary>
    public SlotBooked Handle(AvailabilityConfirmed confirmed, ClinicDbContext db)
    {
        CurrentStatus = AppointmentStatus.AvailabilityChecked;

        // Create the appointment record
        var appointment = new Appointment
        {
            Id = Id,
            PatientId = PatientId,
            ProviderName = ProviderName,
            ScheduledAt = ScheduledAt,
            Status = AppointmentStatus.Booked
        };

        db.Appointments.Add(appointment);

        return new SlotBooked(Id, PatientId);
    }

    /// <summary>
    /// Handle slot booked — send confirmation.
    /// Returns the final confirmation event which completes the saga.
    /// </summary>
    public AppointmentConfirmed Handle(SlotBooked booked)
    {
        CurrentStatus = AppointmentStatus.Booked;
        return new AppointmentConfirmed(Id, PatientId);
    }

    /// <summary>
    /// Handle final confirmation — saga completes.
    /// MarkCompleted() tells Wolverine to delete the saga state.
    /// </summary>
    public void Handle(AppointmentConfirmed confirmed, ClinicDbContext db)
    {
        CurrentStatus = AppointmentStatus.Confirmed;

        // Update the appointment status
        var appointment = db.Appointments.Find(Id);
        if (appointment is not null)
        {
            appointment.Status = AppointmentStatus.Confirmed;
            appointment.UpdatedAt = DateTime.UtcNow;
        }

        // Signal saga completion — Wolverine deletes persisted state
        MarkCompleted();
    }

    // ─── Compensation Handlers ──────────────────────────────────────────────

    /// <summary>
    /// Availability denied — notify patient and end saga.
    /// </summary>
    public void Handle(AvailabilityDenied denied)
    {
        CurrentStatus = AppointmentStatus.Cancelled;
        // In production: publish notification to patient
        MarkCompleted();
    }

    /// <summary>
    /// Booking failed — release hold and compensate.
    /// </summary>
    public void Handle(BookingFailed failed, ClinicDbContext db)
    {
        CurrentStatus = AppointmentStatus.Cancelled;

        // Remove any partially-created appointment
        var appointment = db.Appointments.Find(Id);
        if (appointment is not null)
        {
            db.Appointments.Remove(appointment);
        }

        MarkCompleted();
    }

    /// <summary>
    /// Timeout handler — if saga hasn't completed in 15 minutes, cancel everything.
    /// Wolverine delivers this as a scheduled message.
    /// </summary>
    public void Handle(AppointmentTimedOut timedOut, ClinicDbContext db)
    {
        // If already completed, ignore the timeout
        if (CurrentStatus == AppointmentStatus.Confirmed)
            return;

        CurrentStatus = AppointmentStatus.Cancelled;

        var appointment = db.Appointments.Find(Id);
        if (appointment is not null)
        {
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.Notes = "Cancelled due to confirmation timeout (15 min)";
            appointment.UpdatedAt = DateTime.UtcNow;
        }

        MarkCompleted();
    }
}

// ─── Commands ───────────────────────────────────────────────────────────────

/// <summary>
/// Command to initiate the appointment scheduling saga.
/// </summary>
public record ScheduleAppointment(
    Guid PatientId,
    string ProviderName,
    DateTime ScheduledAt,
    string? Reason = null);

/// <summary>
/// Wolverine scheduled message wrapper.
/// Wolverine delivers this after the specified delay.
/// </summary>
public record ScheduledMessage<T>(T Message, TimeSpan Delay);
