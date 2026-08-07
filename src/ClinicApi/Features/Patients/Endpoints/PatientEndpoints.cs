using ClinicApi.Features.Patients.Commands;
using ClinicApi.Features.Patients.Queries;
using Wolverine.Http;

namespace ClinicApi.Features.Patients.Endpoints;

/// <summary>
/// HTTP endpoints for the Patients feature using WolverineFx.Http.
/// Each method is both an HTTP endpoint and routes through Wolverine's handler pipeline
/// (including FluentValidation middleware, logging middleware, etc.)
/// </summary>
public static class PatientEndpoints
{
    [WolverinePost("/api/patients")]
    public static Task<CreatePatientResponse> CreatePatient(CreatePatient command) => throw new NotImplementedException();

    [WolverineGet("/api/patients/{id}")]
    public static Task<PatientDto?> GetPatient(Guid id) => throw new NotImplementedException();

    [WolverineGet("/api/patients")]
    public static Task<PatientListResponse> ListPatients(int page = 1, int pageSize = 20, string? status = null, string? searchTerm = null) => throw new NotImplementedException();

    [WolverinePut("/api/patients/{id}")]
    public static Task UpdatePatient(Guid id, UpdatePatient command) => throw new NotImplementedException();

    // Batch operations
    [WolverinePost("/api/patients/batch-import")]
    public static Task<BatchImportResult> BatchImport(ImportPatientsBatch command) => throw new NotImplementedException();

    [WolverinePost("/api/patients/bulk-import")]
    public static Task<BatchImportResult> BulkImport(BulkImportPatients command) => throw new NotImplementedException();

    [WolverinePost("/api/patients/deactivate-inactive")]
    public static Task<DeactivateResult> DeactivateInactive(DeactivateInactivePatients command) => throw new NotImplementedException();

    [WolverinePost("/api/patients/update-insurance-batch")]
    public static Task<BatchUpdateResult> UpdateInsuranceBatch(UpdatePatientInsuranceInfo command) => throw new NotImplementedException();

    [WolverinePost("/api/patients/purge-expired")]
    public static Task<PurgeResult> PurgeExpired(PurgeExpiredRecords command) => throw new NotImplementedException();
}
