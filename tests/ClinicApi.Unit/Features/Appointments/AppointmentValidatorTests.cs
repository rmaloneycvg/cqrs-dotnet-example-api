using ClinicApi.Features.Appointments.Saga;
using ClinicApi.Features.Appointments.Validators;
using FluentAssertions;

namespace ClinicApi.Unit.Features.Appointments;

[Trait("Category", "Unit")]
public class AppointmentValidatorTests
{
    private readonly ScheduleAppointmentValidator _validator = new();

    [Fact]
    public void Should_Fail_When_PatientIdEmpty()
    {
        var command = new ScheduleAppointment(Guid.Empty, "Dr. Smith", DateTime.UtcNow.AddDays(1));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PatientId");
    }

    [Fact]
    public void Should_Fail_When_ProviderNameEmpty()
    {
        var command = new ScheduleAppointment(Guid.NewGuid(), "", DateTime.UtcNow.AddDays(1));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProviderName");
    }

    [Fact]
    public void Should_Fail_When_ScheduledAtInPast()
    {
        var command = new ScheduleAppointment(Guid.NewGuid(), "Dr. Smith", DateTime.UtcNow.AddHours(-1));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ScheduledAt");
    }

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var command = new ScheduleAppointment(Guid.NewGuid(), "Dr. Smith", DateTime.UtcNow.AddDays(7));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
