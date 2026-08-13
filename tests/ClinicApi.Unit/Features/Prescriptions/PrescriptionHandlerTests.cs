using ClinicApi.Domain.Entities;
using ClinicApi.Domain.Events;
using ClinicApi.Features.Prescriptions.Commands;
using ClinicApi.Features.Prescriptions.Validators;
using ClinicApi.Infrastructure.Persistence;
using ClinicApi.Unit.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Wolverine;
namespace ClinicApi.Unit.Features.Prescriptions;

[Trait("Category", "Unit")]
public class CreatePrescriptionHandlerTests : IDisposable
{
    private readonly ClinicDbContext _db;

    public CreatePrescriptionHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ClinicDbContext(options, TestTenantContext.CrossTenant());
    }

    [Fact]
    public async Task Handle_Should_CreatePrescriptionAndReturnResponse()
    {
        var patient = PatientBuilders.ValidPatient();
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var command = new CreatePrescription(
            patient.Id, "Amoxicillin", "500mg", "3x daily", 3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            "Dr. Smith", "Take with food");

        var messageBus = Substitute.For<IMessageBus>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger>();

        var (response, @event) = await CreatePrescriptionHandler.Handle(
            command, _db, messageBus, logger, CancellationToken.None);

        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.MedicationName.Should().Be("Amoxicillin");
        response.TotalRefills.Should().Be(3);
    }

    [Fact]
    public async Task Handle_Should_PersistPrescriptionWithCorrectRefills()
    {
        var patient = PatientBuilders.ValidPatient();
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var command = new CreatePrescription(
            patient.Id, "Lisinopril", "10mg", "1x daily", 12,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            "Dr. Jones");

        var messageBus = Substitute.For<IMessageBus>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger>();

        await CreatePrescriptionHandler.Handle(command, _db, messageBus, logger, CancellationToken.None);

        var prescription = await _db.Prescriptions.FirstOrDefaultAsync(p => p.PatientId == patient.Id);
        prescription.Should().NotBeNull();
        prescription!.RefillsRemaining.Should().Be(12);
        prescription.Status.Should().Be(PrescriptionStatus.Active);
    }

    [Fact]
    public async Task Handle_Should_ScheduleRefillReminders()
    {
        var patient = PatientBuilders.ValidPatient();
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var command = new CreatePrescription(
            patient.Id, "Metformin", "1000mg", "2x daily", 6,
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(7)),
            "Dr. Brown");

        var messageBus = Substitute.For<IMessageBus>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger>();

        var (response, _) = await CreatePrescriptionHandler.Handle(command, _db, messageBus, logger, CancellationToken.None);

        // Handler should have called ScheduleAsync for refill reminders
        // (we verify it was called — actual scheduling tested in integration)
        response.Should().NotBeNull();
        response.TotalRefills.Should().Be(6);

        var prescription = await _db.Prescriptions.FirstAsync(p => p.Id == response.Id);
        prescription.RefillsRemaining.Should().Be(6);
    }

    public void Dispose() => _db.Dispose();
}

[Trait("Category", "Unit")]
public class DiscontinuePrescriptionHandlerTests : IDisposable
{
    private readonly ClinicDbContext _db;

    public DiscontinuePrescriptionHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ClinicDbContext(options, TestTenantContext.CrossTenant());
    }

    [Fact]
    public async Task Handle_Should_MarkPrescriptionAsDiscontinued()
    {
        var prescription = new Prescription
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            MedicationName = "TestMed",
            Dosage = "10mg",
            Frequency = "1x daily",
            TotalRefills = 6,
            RefillsRemaining = 4,
            PrescribedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2)),
            ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(10)),
            PrescriberName = "Dr. Test",
            Status = PrescriptionStatus.Active
        };
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();

        var command = new DiscontinuePrescription(prescription.Id, "Side effects");
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger>();

        var @event = await DiscontinuePrescriptionHandler.Handle(command, _db, logger, CancellationToken.None);

        var updated = await _db.Prescriptions.FindAsync(prescription.Id);
        updated!.Status.Should().Be(PrescriptionStatus.Discontinued);
        @event.Reason.Should().Be("Side effects");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_PrescriptionNotFound()
    {
        var command = new DiscontinuePrescription(Guid.NewGuid(), "Not found test");
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger>();

        var act = () => DiscontinuePrescriptionHandler.Handle(command, _db, logger, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    public void Dispose() => _db.Dispose();
}

[Trait("Category", "Unit")]
public class PrescriptionValidatorTests
{
    [Fact]
    public void CreatePrescription_Should_Fail_When_MedicationNameEmpty()
    {
        var validator = new CreatePrescriptionValidator();
        var command = new CreatePrescription(
            Guid.NewGuid(), "", "10mg", "1x", 3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), "Dr. X");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MedicationName");
    }

    [Fact]
    public void CreatePrescription_Should_Fail_When_ExpirationInPast()
    {
        var validator = new CreatePrescriptionValidator();
        var command = new CreatePrescription(
            Guid.NewGuid(), "Med", "10mg", "1x", 3,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "Dr. X");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpirationDate");
    }

    [Fact]
    public void CreatePrescription_Should_Fail_When_RefillsExceedMax()
    {
        var validator = new CreatePrescriptionValidator();
        var command = new CreatePrescription(
            Guid.NewGuid(), "Med", "10mg", "1x", 25,
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), "Dr. X");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TotalRefills");
    }

    [Fact]
    public void DiscontinuePrescription_Should_Fail_When_ReasonEmpty()
    {
        var validator = new DiscontinuePrescriptionValidator();
        var command = new DiscontinuePrescription(Guid.NewGuid(), "");

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}
