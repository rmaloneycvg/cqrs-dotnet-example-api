using System.Net;
using Alba;
using ClinicApi.Features.Patients.Commands;
using ClinicApi.Features.Patients.Queries;
using ClinicApi.Integration.Fixtures;
using FluentAssertions;

namespace ClinicApi.Integration.Features.Patients;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class PatientCrudEndpointTests
{
    private readonly AppFixture _fixture;

    public PatientCrudEndpointTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreatePatient_Should_BeIdempotent_When_DuplicateEmail()
    {
        var email = $"idempotent-{Guid.NewGuid():N}@test.com";
        var command = new CreatePatient(
            "Idem", "Potent", new DateOnly(1990, 1, 1), email,
            "555-999-0010", "200 Dup St", "Denver", "CO", "80201");

        var result1 = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/patients");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });
        var first = result1.ReadAsJson<CreatePatientResponse>()!;

        var result2 = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/patients");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });
        var second = result2.ReadAsJson<CreatePatientResponse>()!;

        second.Id.Should().Be(first.Id);
    }

    [Fact]
    public async Task GetPatient_Should_ReturnPatient_When_Exists()
    {
        var command = new CreatePatient(
            "Get", "ById", new DateOnly(1980, 3, 20), $"getbyid-{Guid.NewGuid():N}@test.com",
            "555-999-0011", "300 Fetch Ln", "Aurora", "CO", "80012");

        var createResult = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/patients");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });
        var created = createResult.ReadAsJson<CreatePatientResponse>()!;

        var getResult = await _fixture.Host.Scenario(s =>
        {
            s.Get.Url($"/api/patients/{created.Id}");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        var patient = getResult.ReadAsJson<PatientDto>();
        patient.Should().NotBeNull();
        patient!.FirstName.Should().Be("Get");
    }

    [Fact]
    public async Task ListPatients_Should_ReturnPaginatedResults()
    {
        var command = new CreatePatient(
            "List", "Test", new DateOnly(1975, 8, 10), $"list-{Guid.NewGuid():N}@test.com",
            "555-999-0012", "400 Page Dr", "Lakewood", "CO", "80215");

        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/patients");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        var result = await _fixture.Host.Scenario(s =>
        {
            s.Get.Url("/api/patients?page=1&pageSize=10");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        var response = result.ReadAsJson<PatientListResponse>();
        response.Should().NotBeNull();
        response!.Items.Should().NotBeEmpty();
        response.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPatient_Should_Return404_When_NotExists()
    {
        await _fixture.Host.Scenario(s =>
        {
            s.Get.Url($"/api/patients/{Guid.NewGuid()}");
            s.StatusCodeShouldBe(HttpStatusCode.NotFound);
        });
    }
}
