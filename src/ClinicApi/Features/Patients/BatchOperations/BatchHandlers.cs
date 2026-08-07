using System.Data;
using ClinicApi.Domain.Entities;
using ClinicApi.Domain.Events;
using ClinicApi.Domain.ValueObjects;
using ClinicApi.Infrastructure.Persistence;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace ClinicApi.Features.Patients.BatchOperations;

/// <summary>
/// Batch import handler using EF Core AddRange with chunking.
/// Processes patients in chunks of 500 to avoid overwhelming the change tracker.
/// Demonstrates row-level validation: invalid rows are skipped and reported.
/// </summary>
public static class ImportPatientsBatchHandler
{
    public static async Task<(Commands.BatchImportResult, PatientsBatchImported)> Handle(
        Commands.ImportPatientsBatch command,
        ClinicDbContext db,
        CancellationToken cancellationToken)
    {
        const int chunkSize = 500;
        var errors = new List<string>();
        var successCount = 0;

        var chunks = command.Patients.Chunk(chunkSize);

        foreach (var chunk in chunks)
        {
            var patients = new List<Patient>();

            foreach (var dto in chunk)
            {
                // Idempotency: skip duplicates by ExternalId
                if (dto.ExternalId is not null)
                {
                    var exists = await db.Patients
                        .AnyAsync(p => p.ExternalId == dto.ExternalId, cancellationToken);
                    if (exists)
                    {
                        errors.Add($"Duplicate ExternalId: {dto.ExternalId}");
                        continue;
                    }
                }

                patients.Add(new Patient
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    DateOfBirth = dto.DateOfBirth,
                    Email = dto.Email,
                    Phone = new PhoneNumber(dto.PhoneNumber),
                    Address = new Address(dto.Street, dto.City, dto.State, dto.ZipCode),
                    ExternalId = dto.ExternalId
                });
            }

            if (patients.Count > 0)
            {
                db.Patients.AddRange(patients);
                await db.SaveChangesAsync(cancellationToken);
                successCount += patients.Count;

                // Clear change tracker to avoid memory issues with large batches
                db.ChangeTracker.Clear();
            }
        }

        var result = new Commands.BatchImportResult(
            command.Patients.Count, successCount, command.Patients.Count - successCount, errors);

        var @event = new PatientsBatchImported(result.TotalCount, result.SuccessCount, result.FailureCount);

        return (result, @event);
    }
}

/// <summary>
/// High-volume bulk import using SqlBulkCopy.
/// For 10k+ rows where EF Core AddRange is too slow.
/// Maps patient data to a DataTable for SqlBulkCopy consumption.
/// </summary>
public static class BulkImportPatientsHandler
{
    public static async Task<(Commands.BatchImportResult, PatientsBatchImported)> Handle(
        Commands.BulkImportPatients command,
        IBulkInsertService bulkInsertService,
        CancellationToken cancellationToken)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("Id", typeof(Guid));
        dataTable.Columns.Add("FirstName", typeof(string));
        dataTable.Columns.Add("LastName", typeof(string));
        dataTable.Columns.Add("DateOfBirth", typeof(DateTime));
        dataTable.Columns.Add("Email", typeof(string));
        dataTable.Columns.Add("PhoneNumber", typeof(string));
        dataTable.Columns.Add("PhoneType", typeof(int));
        dataTable.Columns.Add("AddressStreet", typeof(string));
        dataTable.Columns.Add("AddressCity", typeof(string));
        dataTable.Columns.Add("AddressState", typeof(string));
        dataTable.Columns.Add("AddressZipCode", typeof(string));
        dataTable.Columns.Add("AddressCountry", typeof(string));
        dataTable.Columns.Add("Status", typeof(int));
        dataTable.Columns.Add("ExternalId", typeof(string));
        dataTable.Columns.Add("CreatedAt", typeof(DateTime));
        dataTable.Columns.Add("UpdatedAt", typeof(DateTime));

        foreach (var dto in command.Patients)
        {
            dataTable.Rows.Add(
                Guid.NewGuid(),
                dto.FirstName,
                dto.LastName,
                dto.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                dto.Email,
                dto.PhoneNumber,
                (int)PhoneNumberType.Mobile,
                dto.Street,
                dto.City,
                dto.State,
                dto.ZipCode,
                "US",
                (int)PatientStatus.Active,
                dto.ExternalId ?? (object)DBNull.Value,
                DateTime.UtcNow,
                DateTime.UtcNow);
        }

        await bulkInsertService.BulkInsertAsync<Patient>(
            dataTable, "Patients", command.BatchSize, cancellationToken: cancellationToken);

        var result = new Commands.BatchImportResult(
            command.Patients.Count, command.Patients.Count, 0, []);

        var @event = new PatientsBatchImported(result.TotalCount, result.SuccessCount, result.FailureCount);

        return (result, @event);
    }
}

/// <summary>
/// Batch update using EF Core ExecuteUpdate.
/// Deactivates patients who haven't had an appointment in N days.
/// No entities loaded into memory — single SQL UPDATE statement.
/// </summary>
public static class DeactivateInactivePatientsHandler
{
    public static async Task<Commands.DeactivateResult> Handle(
        Commands.DeactivateInactivePatients command,
        ClinicDbContext db,
        CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-command.InactiveDaysThreshold);

        var affected = await db.Patients
            .Where(p => p.Status == PatientStatus.Active)
            .Where(p => p.LastAppointmentAt == null || p.LastAppointmentAt < cutoffDate)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, PatientStatus.Inactive)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        return new Commands.DeactivateResult(affected);
    }
}

/// <summary>
/// Batch update using Dapper Execute with a collection.
/// Updates insurance info for multiple patients in a single roundtrip.
/// </summary>
public static class UpdatePatientInsuranceInfoHandler
{
    public static async Task<Commands.BatchUpdateResult> Handle(
        Commands.UpdatePatientInsuranceInfo command,
        IDbConnectionFactory connectionFactory,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE Patients
            SET InsuranceProviderId = @ProviderId,
                InsuranceProviderName = @ProviderName,
                InsurancePolicyNumber = @PolicyNumber,
                InsuranceGroupNumber = @GroupNumber,
                InsuranceEffectiveDate = @EffectiveDate,
                InsuranceExpirationDate = @ExpirationDate,
                UpdatedAt = @UpdatedAt
            WHERE Id = @PatientId
            """;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        var errors = new List<string>();
        var successCount = 0;

        foreach (var update in command.Updates)
        {
            try
            {
                var affected = await connection.ExecuteAsync(sql, new
                {
                    update.PatientId,
                    update.ProviderId,
                    update.ProviderName,
                    update.PolicyNumber,
                    update.GroupNumber,
                    EffectiveDate = update.EffectiveDate.ToDateTime(TimeOnly.MinValue),
                    ExpirationDate = update.ExpirationDate?.ToDateTime(TimeOnly.MinValue),
                    UpdatedAt = DateTime.UtcNow
                });

                if (affected > 0) successCount++;
                else errors.Add($"Patient {update.PatientId} not found");
            }
            catch (Exception ex)
            {
                errors.Add($"Patient {update.PatientId}: {ex.Message}");
            }
        }

        return new Commands.BatchUpdateResult(
            command.Updates.Count, successCount, command.Updates.Count - successCount, errors);
    }
}

/// <summary>
/// Batch delete using EF Core ExecuteDelete.
/// Purges records older than a specified date.
/// No entities loaded — single SQL DELETE statement.
/// </summary>
public static class PurgeExpiredRecordsHandler
{
    public static async Task<Commands.PurgeResult> Handle(
        Commands.PurgeExpiredRecords command,
        ClinicDbContext db,
        CancellationToken cancellationToken)
    {
        var deleted = await db.Patients
            .Where(p => p.Status == PatientStatus.Inactive)
            .Where(p => p.UpdatedAt < command.OlderThan)
            .ExecuteDeleteAsync(cancellationToken);

        return new Commands.PurgeResult(deleted);
    }
}
