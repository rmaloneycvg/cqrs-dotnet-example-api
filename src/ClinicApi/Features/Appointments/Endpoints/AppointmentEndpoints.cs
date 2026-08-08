using ClinicApi.Features.Appointments.Saga;
using Wolverine;
using Wolverine.Http;

namespace ClinicApi.Features.Appointments.Endpoints;

/// <summary>
/// HTTP endpoints for Appointment scheduling.
/// Triggers the appointment scheduling saga via Wolverine's message bus.
/// </summary>
public static class AppointmentEndpoints
{
    [WolverinePost("/api/appointments")]
    public static Task ScheduleAppointment(ScheduleAppointment command, IMessageBus bus)
        => bus.InvokeAsync(command);
}
