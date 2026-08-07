using Bogus;
using ClinicApi.Domain.Entities;
using ClinicApi.Domain.ValueObjects;
using ClinicApi.Features.Patients.Commands;

namespace ClinicApi.Unit.Shared;

/// <summary>
/// Test data builders using Bogus.
/// Follows the Builder pattern for test data construction.
/// </summary>
public static class PatientBuilders
{
    private static readonly Faker _faker = new();

    public static CreatePatient ValidCreatePatientCommand() => new(
        FirstName: _faker.Name.FirstName(),
        LastName: _faker.Name.LastName(),
        DateOfBirth: DateOnly.FromDateTime(_faker.Date.Past(50, DateTime.UtcNow.AddYears(-18))),
        Email: _faker.Internet.Email(),
        PhoneNumber: _faker.Phone.PhoneNumber("###-###-####"),
        Street: _faker.Address.StreetAddress(),
        City: _faker.Address.City(),
        State: _faker.Address.State(),
        ZipCode: _faker.Address.ZipCode("#####"),
        ExternalId: _faker.Random.AlphaNumeric(10));

    public static Patient ValidPatient() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = _faker.Name.FirstName(),
        LastName = _faker.Name.LastName(),
        DateOfBirth = DateOnly.FromDateTime(_faker.Date.Past(50, DateTime.UtcNow.AddYears(-18))),
        Email = _faker.Internet.Email(),
        Phone = new PhoneNumber(_faker.Phone.PhoneNumber("###-###-####")),
        Address = new Address(
            _faker.Address.StreetAddress(),
            _faker.Address.City(),
            _faker.Address.State(),
            _faker.Address.ZipCode("#####")),
        Status = PatientStatus.Active,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public static List<CreatePatient> ValidCreatePatientBatch(int count) =>
        Enumerable.Range(0, count).Select(_ => ValidCreatePatientCommand()).ToList();
}
