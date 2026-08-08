using ClinicApi.Domain.Entities;
using ClinicApi.Domain.Events;
using ClinicApi.Domain.ValueObjects;
using ClinicApi.Infrastructure.Persistence;
using ClinicApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace ClinicApi.Features.Patients.Commands;

/// <summary>
/// Wolverine handler for CreatePatient command.
/// Convention: static Handle method with message as first parameter.
/// Return values are cascaded as published messages.
/// PHI writes are audit-logged per HIPAA §164.312(b).
/// </summary>
public static class CreatePatientHandler
{
    public static async Task<(CreatePatientResponse, PatientCreated)> Handle(
        CreatePatient command,
        ClinicDbContext db,
        IPhiAuditLogger audit,
        CancellationToken cancellationToken)
    {
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

        audit.LogModification("system", "Create", "Patient", patient.Id,
            ["FirstName", "LastName", "Email", "Phone", "Address", "DateOfBirth"]);

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
        IPhiAuditLogger audit,
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

        audit.LogModification("system", "Update", "Patient", patient.Id,
            ["FirstName", "LastName", "Email", "Phone", "Address"]);

        return new PatientUpdated(patient.Id, patient.FirstName, patient.LastName);
    }
}
