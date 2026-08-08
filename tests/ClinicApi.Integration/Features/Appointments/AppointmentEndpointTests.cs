using System.Net;
using Alba;
using ClinicApi.Features.Appointments.Saga;
using ClinicApi.Features.Patients.Commands;
using ClinicApi.Integration.Fixtures;
using FluentAssertions;

namespace ClinicApi.Integration.Features.Appointments;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class AppointmentEndpointTests
{
    private readonly AppFixture _fixture;

    public AppointmentEndpointTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ScheduleAppointment_Should_Return200_When_ValidRequest()
    {
        // Arrange
        var patient = await CreatePatient();

        var command = new ScheduleAppointment(
            PatientId: patient.Id,
            ProviderName: "Dr. Williams",
            ScheduledAt: DateTime.UtcNow.AddDays(7),
            Reason: "Annual physical");

        // Act — triggers the saga (async workflow, returns 204 No Content)
        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/appointments");
            s.StatusCodeShouldBe(HttpStatusCode.NoContent);
        });
    }

    [Fact]
    public async Task ScheduleAppointment_Should_Return400_When_MissingPatientId()
    {
        // Arrange — empty PatientId
        var command = new ScheduleAppointment(
            PatientId: Guid.Empty,
            ProviderName: "Dr. Williams",
            ScheduledAt: DateTime.UtcNow.AddDays(7));

        // Act & Assert — FluentValidation rejects empty PatientId
        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/appointments");
            s.StatusCodeShouldBe(HttpStatusCode.BadRequest);
        });
    }

    private async Task<CreatePatientResponse> CreatePatient()
    {
        var command = new CreatePatient(
            "Appt", "TestPatient", new DateOnly(1990, 7, 12),
            $"appt-{Guid.NewGuid():N}@test.com",
            "555-333-0001", "400 Clinic Way", "Arvada", "CO", "80003");

        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/patients");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        return result.ReadAsJson<CreatePatientResponse>()!;
    }
}
