using ClinicApi.Domain.Entities;
using ClinicApi.Domain.Events;
using ClinicApi.Domain.ValueObjects;
using ClinicApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Features.Patients.Commands;

/// <summary>
/// Wolverine handler for CreatePatient command.
/// PHI audit logging handled by PhiAuditMiddleware (no per-handler injection).
/// </summary>
public static class CreatePatientHandler
{
    public static async Task<(CreatePatientResponse, PatientCreated)> Handle(
        CreatePatient command,
        ClinicDbContext db,
        CancellationToken cancellationToken)
    {
        // Idempotency: check for existing patient by email or external ID
        var existing = await db.Patients
            .Where(p => p.Email == command.Email ||
                       (command.ExternalId != null && p.ExternalId == command.ExternalId))
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            var existingResponse = new CreatePatientResponse(existing.Id, $"{existing.FirstName} {existing.LastName}", existing.Email);
            var existingEvent = new PatientCreated(existing.Id, existing.FirstName, existing.LastName, existing.Email);
            return (existingResponse, existingEvent);
        }

        var patient = new Patient
        {
            FirstName = command.FirstName,
            LastName = command.LastName,
            DateOfBirth = command.DateOfBirth,
            Email = command.Email,
            Phone = new PhoneNumber(command.PhoneNumber),
            Address = new Address(command.Street, command.City, command.State, command.ZipCode),
            ExternalId = command.ExternalId
        };

        db.Patients.Add(patient);
        await db.SaveChangesAsync(cancellationToken);

        var response = new CreatePatientResponse(patient.Id, patient.FullName, patient.Email);
        var @event = new PatientCreated(patient.Id, patient.FirstName, patient.LastName, patient.Email);

        return (response, @event);
    }
}

public static class UpdatePatientHandler
{
    public static async Task<PatientUpdated> Handle(
        UpdatePatient command,
        ClinicDbContext db,
        CancellationToken cancellationToken)
    {
        var patient = await db.Patients.FindAsync([command.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Patient {command.Id} not found");

        patient.FirstName = command.FirstName;
        patient.LastName = command.LastName;
        patient.Email = command.Email;
        patient.Phone = new PhoneNumber(command.PhoneNumber);
        patient.Address = new Address(command.Street, command.City, command.State, command.ZipCode);
        patient.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new PatientUpdated(patient.Id, patient.FirstName, patient.LastName);
    }
}

public static class DeactivatePatientHandler
{
    public static async Task Handle(
        DeactivatePatient command,
        ClinicDbContext db,
        CancellationToken cancellationToken)
    {
        var patient = await db.Patients.FindAsync([command.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Patient {command.Id} not found");

        patient.Status = PatientStatus.Inactive;
        patient.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }
}
