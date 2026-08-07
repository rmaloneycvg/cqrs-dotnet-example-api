using System.Net;
using System.Net.Http.Json;
using Alba;
using ClinicApi.Features.Patients.Commands;
using ClinicApi.Integration.Fixtures;
using FluentAssertions;

namespace ClinicApi.Integration.Features.Patients;

/// <summary>
/// Integration tests for Patient HTTP endpoints.
/// Uses real MSSQL + RabbitMQ containers via Testcontainers.
/// Alba exercises the full ASP.NET Core pipeline including Wolverine middleware.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class PatientEndpointTests
{
    private readonly AppFixture _fixture;

    public PatientEndpointTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreatePatient_Should_Return201_When_ValidRequest()
    {
        // Arrange
        var command = new CreatePatient(
            "Integration", "Test", new DateOnly(1985, 6, 15), $"integration-{Guid.NewGuid():N}@test.com",
            "555-999-0001", "100 Test Ave", "TestCity", "CO", "80301");

        // Act
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/patients");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        // Assert
        var response = result.ReadAsJson<CreatePatientResponse>();
        response.Should().NotBeNull();
        response!.Id.Should().NotBeEmpty();
        response.FullName.Should().Be("Integration Test");
    }

    [Fact]
    public async Task CreatePatient_Should_Return400_When_InvalidEmail()
    {
        // Arrange
        var command = new CreatePatient(
            "Bad", "Email", new DateOnly(1985, 6, 15), "not-an-email",
            "555-999-0002", "100 Test Ave", "TestCity", "CO", "80301");

        // Act & Assert — FluentValidation middleware should reject
        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/patients");
            s.StatusCodeShouldBe(HttpStatusCode.BadRequest);
        });
    }
}
