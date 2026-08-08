using System.Net;
using Alba;
using ClinicApi.Features.Patients.Commands;
using ClinicApi.Features.Prescriptions.Commands;
using ClinicApi.Integration.Fixtures;
using FluentAssertions;

namespace ClinicApi.Integration.Features.Prescriptions;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class PrescriptionEndpointTests
{
    private readonly AppFixture _fixture;

    public PrescriptionEndpointTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreatePrescription_Should_ReturnPrescription_When_Valid()
    {
        // Arrange
        var patient = await CreatePatient();

        var command = new CreatePrescription(
            PatientId: patient.Id,
            MedicationName: "Amoxicillin",
            Dosage: "500mg",
            Frequency: "3x daily",
            TotalRefills: 3,
            ExpirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            PrescriberName: "Dr. Smith",
            Notes: "Take with food");

        // Act
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/prescriptions");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        // Assert
        var response = result.ReadAsJson<CreatePrescriptionResponse>();
        response.Should().NotBeNull();
        response!.Id.Should().NotBeEmpty();
        response.MedicationName.Should().Be("Amoxicillin");
        response.TotalRefills.Should().Be(3);
    }

    [Fact]
    public async Task DiscontinuePrescription_Should_Succeed_When_PrescriptionExists()
    {
        // Arrange — create patient + prescription
        var patient = await CreatePatient();

        var createCommand = new CreatePrescription(
            PatientId: patient.Id,
            MedicationName: "Lisinopril",
            Dosage: "10mg",
            Frequency: "1x daily",
            TotalRefills: 12,
            ExpirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            PrescriberName: "Dr. Jones");

        var createResult = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(createCommand).ToUrl("/api/prescriptions");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });
        var prescription = createResult.ReadAsJson<CreatePrescriptionResponse>()!;

        // Act
        var discontinueCommand = new DiscontinuePrescription(prescription.Id, "Patient experiencing side effects");

        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(discontinueCommand).ToUrl($"/api/prescriptions/{prescription.Id}/discontinue");
            s.StatusCodeShouldBe(HttpStatusCode.NoContent);
        });
    }

    private async Task<CreatePatientResponse> CreatePatient()
    {
        var command = new CreatePatient(
            "Rx", "TestPatient", new DateOnly(1975, 11, 5),
            $"rx-{Guid.NewGuid():N}@test.com",
            "555-222-0001", "300 Pharmacy Dr", "Boulder", "CO", "80302");

        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/patients");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        return result.ReadAsJson<CreatePatientResponse>()!;
    }
}
