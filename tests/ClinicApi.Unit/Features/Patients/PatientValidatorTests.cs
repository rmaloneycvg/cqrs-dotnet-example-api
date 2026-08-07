using ClinicApi.Features.Patients.Commands;
using ClinicApi.Features.Patients.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace ClinicApi.Unit.Features.Patients;

[Trait("Category", "Unit")]
public class PatientValidatorTests
{
    private readonly CreatePatientValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_AllFieldsValid()
    {
        var command = new CreatePatient(
            "John", "Doe", new DateOnly(1990, 1, 1), "john@example.com",
            "555-123-4567", "123 Main St", "Denver", "CO", "80201");

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_FirstNameEmpty()
    {
        var command = new CreatePatient(
            "", "Doe", new DateOnly(1990, 1, 1), "john@example.com",
            "555-123-4567", "123 Main St", "Denver", "CO", "80201");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailInvalid()
    {
        var command = new CreatePatient(
            "John", "Doe", new DateOnly(1990, 1, 1), "not-an-email",
            "555-123-4567", "123 Main St", "Denver", "CO", "80201");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_Should_Fail_When_DateOfBirthInFuture()
    {
        var command = new CreatePatient(
            "John", "Doe", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), "john@example.com",
            "555-123-4567", "123 Main St", "Denver", "CO", "80201");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void Validate_Should_Fail_When_ZipCodeInvalid()
    {
        var command = new CreatePatient(
            "John", "Doe", new DateOnly(1990, 1, 1), "john@example.com",
            "555-123-4567", "123 Main St", "Denver", "CO", "ABCDE");

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ZipCode);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345-6789")]
    public void Validate_Should_Pass_When_ZipCodeValid(string zipCode)
    {
        var command = new CreatePatient(
            "John", "Doe", new DateOnly(1990, 1, 1), "john@example.com",
            "555-123-4567", "123 Main St", "Denver", "CO", zipCode);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ZipCode);
    }
}
