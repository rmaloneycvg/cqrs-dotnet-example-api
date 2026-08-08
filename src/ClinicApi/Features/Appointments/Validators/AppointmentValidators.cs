using ClinicApi.Features.Appointments.Saga;
using FluentValidation;

namespace ClinicApi.Features.Appointments.Validators;

public class ScheduleAppointmentValidator : AbstractValidator<ScheduleAppointment>
{
    public ScheduleAppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ProviderName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTime.UtcNow)
            .WithMessage("Appointment must be scheduled in the future");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
