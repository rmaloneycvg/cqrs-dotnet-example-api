using ClinicApi.Domain.Entities;
using ClinicApi.Features.Billing.Commands;
using ClinicApi.Features.Billing.Validators;
using ClinicApi.Infrastructure.Persistence;
using ClinicApi.Unit.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Wolverine;

namespace ClinicApi.Unit.Features.Billing;

[Trait("Category", "Unit")]
public class CreateInvoiceHandlerTests : IDisposable
{
    private readonly ClinicDbContext _db;

    public CreateInvoiceHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ClinicDbContext(options, TestTenantContext.CrossTenant());
    }

    [Fact]
    public async Task Handle_Should_CreateInvoiceWithGeneratedNumber()
    {
        var patient = PatientBuilders.ValidPatient();
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var command = new CreateInvoice(patient.Id, 250.00m, "Checkup", DateTime.UtcNow.AddDays(30));

        var (response, @event) = await CreateInvoiceHandler.Handle(command, _db, CancellationToken.None);

        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.InvoiceNumber.Should().StartWith("INV-");
        response.Amount.Should().Be(250.00m);
    }

    [Fact]
    public async Task Handle_Should_PersistInvoiceToDatabase()
    {
        var patient = PatientBuilders.ValidPatient();
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var command = new CreateInvoice(patient.Id, 100.00m, "Lab", DateTime.UtcNow.AddDays(14));

        await CreateInvoiceHandler.Handle(command, _db, CancellationToken.None);

        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.PatientId == patient.Id);
        invoice.Should().NotBeNull();
        invoice!.Amount.Amount.Should().Be(100.00m);
        invoice.Status.Should().Be(InvoiceStatus.Issued);
    }

    [Fact]
    public async Task Handle_Should_PublishInvoiceCreatedEvent()
    {
        var patient = PatientBuilders.ValidPatient();
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var command = new CreateInvoice(patient.Id, 75.00m, "Consultation", DateTime.UtcNow.AddDays(30));

        var (_, @event) = await CreateInvoiceHandler.Handle(command, _db, CancellationToken.None);

        @event.PatientId.Should().Be(patient.Id);
        @event.Amount.Should().Be(75.00m);
    }

    public void Dispose() => _db.Dispose();
}

[Trait("Category", "Unit")]
public class BatchUpdateInvoiceStatusesHandlerTests : IDisposable
{
    private readonly ClinicDbContext _db;

    public BatchUpdateInvoiceStatusesHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ClinicDbContext(options, TestTenantContext.CrossTenant());
    }

    [Fact(Skip = "ExecuteUpdateAsync not supported by InMemory provider — tested in integration")]
    public async Task Handle_Should_MarkOverdueInvoices_When_PastDueDate()
    {
        var patient = PatientBuilders.ValidPatient();
        _db.Patients.Add(patient);

        var invoice = new Invoice
        {
            PatientId = patient.Id,
            InvoiceNumber = "INV-TEST-001",
            Amount = new Domain.ValueObjects.Money(200m),
            PaidAmount = Domain.ValueObjects.Money.Zero,
            Status = InvoiceStatus.Issued,
            DueDate = DateTime.UtcNow.AddDays(-60),
            IssuedAt = DateTime.UtcNow.AddDays(-90)
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var command = new BatchUpdateInvoiceStatuses(OverdueDaysThreshold: 30);
        var messageBus = Substitute.For<IMessageBus>();

        var result = await BatchUpdateInvoiceStatusesHandler.Handle(command, _db, messageBus, CancellationToken.None);

        result.UpdatedCount.Should().BeGreaterThan(0);
    }

    public void Dispose() => _db.Dispose();
}

[Trait("Category", "Unit")]
public class BillingValidatorTests
{
    [Fact]
    public void CreateInvoice_Should_Fail_When_AmountIsNegative()
    {
        var validator = new CreateInvoiceValidator();
        var command = new CreateInvoice(Guid.NewGuid(), -50m, "Test", DateTime.UtcNow.AddDays(30));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void CreateInvoice_Should_Fail_When_DueDateInPast()
    {
        var validator = new CreateInvoiceValidator();
        var command = new CreateInvoice(Guid.NewGuid(), 100m, "Test", DateTime.UtcNow.AddDays(-5));

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DueDate");
    }

    [Fact]
    public void CreateInvoice_Should_Pass_When_Valid()
    {
        var validator = new CreateInvoiceValidator();
        var command = new CreateInvoice(Guid.NewGuid(), 250m, "Annual checkup", DateTime.UtcNow.AddDays(30));

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void BatchCreate_Should_Fail_When_AmountIsZero()
    {
        var validator = new BatchCreateInvoicesValidator();
        var command = new BatchCreateInvoices(0m, "Monthly");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BatchUpdateStatuses_Should_Fail_When_ThresholdNegative()
    {
        var validator = new BatchUpdateInvoiceStatusesValidator();
        var command = new BatchUpdateInvoiceStatuses(-1);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
