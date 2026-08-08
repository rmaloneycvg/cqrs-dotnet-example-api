using ClinicApi.Features.Prescriptions.Commands;
using Wolverine;
using Wolverine.Http;

namespace ClinicApi.Features.Prescriptions.Endpoints;

/// <summary>
/// HTTP endpoints for Prescription management.
/// Triggers scheduled message workflows (refill reminders, expiration warnings).
/// </summary>
public static class PrescriptionEndpoints
{
    [WolverinePost("/api/prescriptions")]
    public static Task<CreatePrescriptionResponse> CreatePrescription(
        CreatePrescription command, IMessageBus bus)
        => bus.InvokeAsync<CreatePrescriptionResponse>(command);

    [WolverinePost("/api/prescriptions/{prescriptionId}/discontinue")]
    public static Task DiscontinuePrescription(
        Guid prescriptionId, DiscontinuePrescription command, IMessageBus bus)
        => bus.InvokeAsync(command with { PrescriptionId = prescriptionId });
}
