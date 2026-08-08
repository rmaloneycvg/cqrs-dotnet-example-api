using System.Net;
using Alba;
using ClinicApi.Features.Billing.Commands;
using ClinicApi.Features.Billing.Queries;
using ClinicApi.Features.Patients.Commands;
using ClinicApi.Integration.Fixtures;
using FluentAssertions;

namespace ClinicApi.Integration.Features.Billing;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class BillingEndpointTests
{
    private readonly AppFixture _fixture;

    public BillingEndpointTests(AppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateInvoice_Should_ReturnInvoice_When_ValidRequest()
    {
        // Arrange — create a patient first
        var patient = await CreatePatient();

        var command = new CreateInvoice(
            patient.Id,
            Amount: 250.00m,
            Description: "Annual checkup",
            DueDate: DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/invoices");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        // Assert
        var response = result.ReadAsJson<CreateInvoiceResponse>();
        response.Should().NotBeNull();
        response!.Id.Should().NotBeEmpty();
        response.InvoiceNumber.Should().StartWith("INV-");
        response.Amount.Should().Be(250.00m);
    }

    [Fact]
    public async Task GetPatientBilling_Should_ReturnHistory_When_InvoicesExist()
    {
        // Arrange — create patient + invoice
        var patient = await CreatePatient();

        var invoice = new CreateInvoice(
            patient.Id, 150.00m, "Lab work", DateTime.UtcNow.AddDays(30));
        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(invoice).ToUrl("/api/invoices");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        // Act
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Get.Url($"/api/patients/{patient.Id}/billing?page=1&pageSize=10");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        // Assert
        var response = result.ReadAsJson<BillingHistoryResponse>();
        response.Should().NotBeNull();
        response!.Invoices.Should().NotBeEmpty();
        response.TotalOutstanding.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetOutstandingBalances_Should_ReturnBalances()
    {
        // Arrange — create patient + unpaid invoice
        var patient = await CreatePatient();

        var invoice = new CreateInvoice(
            patient.Id, 500.00m, "Surgery", DateTime.UtcNow.AddDays(30));
        await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(invoice).ToUrl("/api/invoices");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        // Act
        var result = await _fixture.Host.Scenario(s =>
        {
            s.Get.Url("/api/billing/outstanding?minBalance=0.01");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        // Assert
        var response = result.ReadAsJson<List<OutstandingBalanceDto>>();
        response.Should().NotBeNull();
        response.Should().NotBeEmpty();
    }

    private async Task<CreatePatientResponse> CreatePatient()
    {
        var command = new CreatePatient(
            "Billing", "TestPatient", new DateOnly(1980, 3, 20),
            $"billing-{Guid.NewGuid():N}@test.com",
            "555-111-0001", "200 Billing Ln", "Denver", "CO", "80202");

        var result = await _fixture.Host.Scenario(s =>
        {
            s.Post.Json(command).ToUrl("/api/patients");
            s.StatusCodeShouldBe(HttpStatusCode.OK);
        });

        return result.ReadAsJson<CreatePatientResponse>()!;
    }
}
