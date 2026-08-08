using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace ClinicApi.Performance.LoadTests;

/// <summary>
/// NBomber load test scenarios for the Clinic API.
/// These tests require a running API instance (via 'tilt up' or 'dotnet run').
/// 
/// Currently SKIPPED due to NBomber 6.5.0 being incompatible with Spectre.Console 0.55
/// (pulled transitively by WolverineFx → JasperFx). The TypeLoadException for
/// Spectre.Console.Color is a known ABI break in Spectre.Console 0.55.
/// 
/// To run (once NBomber is updated):
///   dotnet test tests/ClinicApi.Performance --filter "Category=Performance"
/// </summary>
[Trait("Category", "Performance")]
public class PatientApiLoadTests
{
    private const string BaseUrl = "http://localhost:5000";

    [Fact(Skip = "NBomber 6.5.0 incompatible with Spectre.Console 0.55 (pulled by WolverineFx). Awaiting NBomber update.")]
    public void CreatePatient_Should_HandleConcurrentLoad()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("create_patient", async context =>
        {
            var patient = new
            {
                firstName = $"Load-{context.ScenarioInfo.InstanceNumber}",
                lastName = $"Test-{context.InvocationNumber}",
                dateOfBirth = "1990-01-01",
                email = $"load-{Guid.NewGuid():N}@test.com",
                phoneNumber = "555-000-0000",
                street = "Load Test Ave",
                city = "Denver",
                state = "CO",
                zipCode = "80201"
            };

            var request = Http.CreateRequest("POST", $"{BaseUrl}/api/patients")
                .WithJsonBody(patient);

            return await Http.Send(httpClient, request);
        })
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("nbomber_reports")
            .Run();
    }

    [Fact(Skip = "NBomber 6.5.0 incompatible with Spectre.Console 0.55 (pulled by WolverineFx). Awaiting NBomber update.")]
    public void BatchImport_Should_HandleLargePayloads()
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };

        var scenario = Scenario.Create("batch_import", async context =>
        {
            var patients = Enumerable.Range(0, 100).Select(i => new
            {
                firstName = $"Batch-{i}",
                lastName = $"Import-{context.InvocationNumber}",
                dateOfBirth = "1990-01-01",
                email = $"batch-{Guid.NewGuid():N}@test.com",
                phoneNumber = "555-000-0000",
                street = "Batch St",
                city = "Denver",
                state = "CO",
                zipCode = "80201"
            }).ToList();

            var request = Http.CreateRequest("POST", $"{BaseUrl}/api/patients/batch-import")
                .WithJsonBody(new { patients });

            return await Http.Send(httpClient, request);
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("nbomber_reports")
            .Run();
    }
}
