using ClinicApi.Domain.Entities;
using ClinicApi.Domain.Events;
using ClinicApi.Features.Patients.Commands;
using ClinicApi.Infrastructure.Persistence;
using ClinicApi.Unit.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Unit.Features.Patients;

/// <summary>
/// Unit tests for CreatePatientHandler.
/// Pattern: {MethodUnderTest}_Should_{ExpectedBehavior}_When_{Scenario}
/// 
/// Uses in-memory EF Core DbContext for handler tests.
/// NSubstitute mocks for dependencies that cross boundaries.
/// </summary>
[Trait("Category", "Unit")]
public class CreatePatientHandlerTests : IDisposable
{
    private readonly ClinicDbContext _db;

    public CreatePatientHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ClinicDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_CreatePatientAndReturnResponse_When_ValidCommand()
    {
        // Arrange
        var command = PatientBuilders.ValidCreatePatientCommand();

        // Act
        var (response, @event) = await CreatePatientHandler.Handle(command, _db, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.FullName.Should().Be($"{command.FirstName} {command.LastName}");
        response.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task Handle_Should_PersistPatientToDatabase_When_ValidCommand()
    {
        // Arrange
        var command = PatientBuilders.ValidCreatePatientCommand();

        // Act
        await CreatePatientHandler.Handle(command, _db, CancellationToken.None);

        // Assert
        var patient = await _db.Patients.FirstOrDefaultAsync();
        patient.Should().NotBeNull();
        patient!.FirstName.Should().Be(command.FirstName);
        patient.LastName.Should().Be(command.LastName);
        patient.Email.Should().Be(command.Email);
        patient.Phone.Number.Should().Be(command.PhoneNumber);
        patient.Address.Street.Should().Be(command.Street);
        patient.Address.City.Should().Be(command.City);
        patient.Address.State.Should().Be(command.State);
        patient.Address.ZipCode.Should().Be(command.ZipCode);
    }

    [Fact]
    public async Task Handle_Should_PublishPatientCreatedEvent_When_ValidCommand()
    {
        // Arrange
        var command = PatientBuilders.ValidCreatePatientCommand();

        // Act
        var (_, @event) = await CreatePatientHandler.Handle(command, _db, CancellationToken.None);

        // Assert — Wolverine cascades the event automatically from the return tuple
        @event.Should().NotBeNull();
        @event.Should().BeOfType<PatientCreated>();
        @event.FirstName.Should().Be(command.FirstName);
        @event.LastName.Should().Be(command.LastName);
        @event.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task Handle_Should_SetExternalId_When_Provided()
    {
        // Arrange
        var command = PatientBuilders.ValidCreatePatientCommand();

        // Act
        await CreatePatientHandler.Handle(command, _db, CancellationToken.None);

        // Assert
        var patient = await _db.Patients.FirstOrDefaultAsync();
        patient!.ExternalId.Should().Be(command.ExternalId);
    }

    [Fact]
    public async Task Handle_Should_SetActiveStatus_When_Created()
    {
        // Arrange
        var command = PatientBuilders.ValidCreatePatientCommand();

        // Act
        await CreatePatientHandler.Handle(command, _db, CancellationToken.None);

        // Assert
        var patient = await _db.Patients.FirstOrDefaultAsync();
        patient!.Status.Should().Be(PatientStatus.Active);
    }

    public void Dispose() => _db.Dispose();
}

[Trait("Category", "Unit")]
public class UpdatePatientHandlerTests : IDisposable
{
    private readonly ClinicDbContext _db;

    public UpdatePatientHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ClinicDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_UpdatePatientFields_When_ValidCommand()
    {
        // Arrange
        var patient = PatientBuilders.ValidPatient();
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var command = new UpdatePatient(
            patient.Id, "Updated", "Name", "new@email.com", "555-1234",
            "New Street", "New City", "CA", "90210");

        // Act
        var @event = await UpdatePatientHandler.Handle(command, _db, CancellationToken.None);

        // Assert
        var updated = await _db.Patients.FindAsync(patient.Id);
        updated!.FirstName.Should().Be("Updated");
        updated.LastName.Should().Be("Name");
        updated.Email.Should().Be("new@email.com");
        @event.PatientId.Should().Be(patient.Id);
    }

    [Fact]
    public async Task Handle_Should_ThrowInvalidOperation_When_PatientNotFound()
    {
        // Arrange
        var command = new UpdatePatient(
            Guid.NewGuid(), "First", "Last", "e@mail.com", "555-0000",
            "St", "City", "ST", "12345");

        // Act & Assert
        var act = () => UpdatePatientHandler.Handle(command, _db, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    public void Dispose() => _db.Dispose();
}
