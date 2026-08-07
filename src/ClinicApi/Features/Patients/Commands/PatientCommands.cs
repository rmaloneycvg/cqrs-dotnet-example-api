using ClinicApi.Domain.ValueObjects;

namespace ClinicApi.Features.Patients.Commands;

// Create a single patient
public record CreatePatient(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string PhoneNumber,
    string Street,
    string City,
    string State,
    string ZipCode,
    string? ExternalId = null);

public record CreatePatientResponse(Guid Id, string FullName, string Email);

// Update an existing patient
public record UpdatePatient(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Street,
    string City,
    string State,
    string ZipCode);

// Batch import patients (EF Core AddRange with chunking)
public record ImportPatientsBatch(List<CreatePatient> Patients);

public record BatchImportResult(
    int TotalCount,
    int SuccessCount,
    int FailureCount,
    List<string> Errors);

// Bulk import patients (SqlBulkCopy for high volume)
public record BulkImportPatients(List<CreatePatient> Patients, int BatchSize = 5000);

// Batch update - deactivate inactive patients
public record DeactivateInactivePatients(int InactiveDaysThreshold = 730);

public record DeactivateResult(int AffectedCount);

// Batch update - update insurance info via Dapper
public record UpdatePatientInsuranceInfo(List<PatientInsuranceUpdate> Updates);

public record PatientInsuranceUpdate(
    Guid PatientId,
    string ProviderId,
    string ProviderName,
    string PolicyNumber,
    string GroupNumber,
    DateOnly EffectiveDate,
    DateOnly? ExpirationDate = null);

public record BatchUpdateResult(int TotalCount, int SuccessCount, int FailureCount, List<string> Errors);

// Batch delete - purge expired records
public record PurgeExpiredRecords(DateTime OlderThan);

public record PurgeResult(int DeletedCount);
