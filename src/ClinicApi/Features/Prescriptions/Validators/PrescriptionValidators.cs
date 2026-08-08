using ClinicApi.Features.Prescriptions.Commands;
using FluentValidation;

namespace ClinicApi.Features.Prescriptions.Validators;

public class CreatePrescriptionValidator : AbstractValidator<CreatePrescription>
{
    public CreatePrescriptionValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.MedicationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dosage).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Frequency).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TotalRefills).InclusiveBetween(0, 24);
        RuleFor(x => x.ExpirationDate).GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Expiration date must be in the future");
        RuleFor(x => x.PrescriberName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class DiscontinuePrescriptionValidator : AbstractValidator<DiscontinuePrescription>
{
    public DiscontinuePrescriptionValidator()
    {
        RuleFor(x => x.PrescriptionId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
