using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace ClinicApi.Performance.LoadTests;

/// <summary>
/// Additional load test scenarios for Billing and List endpoints.
/// Requires running API via 'tilt up'.
/// </summary>
[Trait("Category", "Performance")]
public class BillingAndListLoadTests
{
    private const string BaseUrl = "http://localhost:5000";

    private static bool IsApiRunning()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = client.GetAsync($"{BaseUrl}/health").GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    [Fact]
    public void ListPatients_Should_HandlePaginatedLoad()
    {
        if (!IsApiRunning()) return;

        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("list_patients_paginated", async context =>
        {
            var page = context.InvocationNumber % 10 + 1;
            var request = Http.CreateRequest("GET", $"{BaseUrl}/api/patients?page={page}&pageSize=20");
            return await Http.Send(httpClient, request);
        })
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(15)),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("nbomber_reports")
            .Run();
    }

    [Fact]
    public void CreateInvoice_Should_HandleConcurrentCreation()
    {
        if (!IsApiRunning()) return;

        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        // Pre-create a patient for invoice creation
        var patientId = CreateTestPatient(httpClient);
        if (patientId == Guid.Empty) return;

        var scenario = Scenario.Create("create_invoice", async context =>
        {
            var invoice = new
            {
                patientId,
                amount = 100m + context.InvocationNumber,
                description = $"Load test invoice #{context.InvocationNumber}",
                dueDate = DateTime.UtcNow.AddDays(30).ToString("O")
            };

            var request = Http.CreateRequest("POST", $"{BaseUrl}/api/invoices")
                .WithJsonBody(invoice);
            return await Http.Send(httpClient, request);
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("nbomber_reports")
            .Run();
    }

    [Fact]
    public void GetOutstandingBalances_Should_HandleConcurrentReads()
    {
        if (!IsApiRunning()) return;

        using var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("outstanding_balances", async context =>
        {
            var request = Http.CreateRequest("GET", $"{BaseUrl}/api/billing/outstanding?minBalance=0.01&page=1&pageSize=50");
            return await Http.Send(httpClient, request);
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 30, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("nbomber_reports")
            .Run();
    }

    private static Guid CreateTestPatient(HttpClient client)
    {
        try
        {
            var patient = new
            {
                firstName = "PerfTest",
                lastName = "Patient",
                dateOfBirth = "1990-01-01",
                email = $"perf-{Guid.NewGuid():N}@test.com",
                phoneNumber = "555-000-0000",
                street = "Perf St",
                city = "Denver",
                state = "CO",
                zipCode = "80201"
            };

            var response = client.PostAsJsonAsync($"{BaseUrl}/api/patients", patient).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) return Guid.Empty;

            var json = response.Content.ReadFromJsonAsync<Dictionary<string, object>>().GetAwaiter().GetResult();
            return json is not null && json.TryGetValue("id", out var id)
                ? Guid.Parse(id.ToString()!)
                : Guid.Empty;
        }
        catch { return Guid.Empty; }
    }
}
