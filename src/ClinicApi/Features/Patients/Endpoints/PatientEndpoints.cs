using ClinicApi.Features.Patients.Commands;
using ClinicApi.Features.Patients.Queries;
using Wolverine;
using Wolverine.Http;

namespace ClinicApi.Features.Patients.Endpoints;

/// <summary>
/// HTTP endpoints for the Patients feature using WolverineFx.Http.
/// Thin delegation layer — all business logic lives in handlers.
/// Routes through Wolverine's handler pipeline (FluentValidation, logging, etc.)
/// </summary>
public static class PatientEndpoints
{
    [WolverinePost("/patients")]
    public static async Task<CreatePatientResponse> CreatePatient(CreatePatient command, IMessageBus bus)
    {
        var response = await bus.InvokeAsync<CreatePatientResponse>(command);
        return response;
    }

    [WolverineGet("/patients/{id}")]
    public static Task<PatientDto?> GetPatient(Guid id, IMessageBus bus)
        => bus.InvokeAsync<PatientDto?>(new GetPatientById(id));

    [WolverineGet("/patients")]
    public static Task<PatientListResponse> ListPatients(
        int page, int pageSize, string? status, string? searchTerm, IMessageBus bus)
        => bus.InvokeAsync<PatientListResponse>(new ListPatients(
            page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, status, searchTerm));

    [WolverinePut("/patients/{id}")]
    public static Task UpdatePatient(Guid id, UpdatePatient command, IMessageBus bus)
        => bus.InvokeAsync(command with { Id = id });

    // Batch operations
    [WolverinePost("/patients/batch-import")]
    public static Task<BatchImportResult> BatchImport(ImportPatientsBatch command, IMessageBus bus)
        => bus.InvokeAsync<BatchImportResult>(command);

    [WolverinePost("/patients/bulk-import")]
    public static Task<BatchImportResult> BulkImport(BulkImportPatients command, IMessageBus bus)
        => bus.InvokeAsync<BatchImportResult>(command);

    [WolverinePost("/patients/deactivate-inactive")]
    public static Task<DeactivateResult> DeactivateInactive(DeactivateInactivePatients command, IMessageBus bus)
        => bus.InvokeAsync<DeactivateResult>(command);

    [WolverinePost("/patients/update-insurance-batch")]
    public static Task<BatchUpdateResult> UpdateInsuranceBatch(UpdatePatientInsuranceInfo command, IMessageBus bus)
        => bus.InvokeAsync<BatchUpdateResult>(command);

    [WolverinePost("/patients/purge-expired")]
    public static Task<PurgeResult> PurgeExpired(PurgeExpiredRecords command, IMessageBus bus)
        => bus.InvokeAsync<PurgeResult>(command);

    [WolverineDelete("/patients/{id}")]
    public static Task DeactivatePatient(Guid id, IMessageBus bus)
        => bus.InvokeAsync(new DeactivatePatient(id));
}
