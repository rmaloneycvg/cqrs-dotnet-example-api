using ClinicApi.Features.Patients.Commands;
using FluentValidation;

namespace ClinicApi.Features.Patients.Validators;

public class CreatePatientValidator : AbstractValidator<CreatePatient>
{
    public CreatePatientValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DateOfBirth).NotEmpty()
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past");
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(10)
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Invalid ZIP code format");
    }
}

public class UpdatePatientValidator : AbstractValidator<UpdatePatient>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ZipCode).NotEmpty().MaximumLength(10);
    }
}

public class ImportPatientsBatchValidator : AbstractValidator<ImportPatientsBatch>
{
    public ImportPatientsBatchValidator()
    {
        RuleFor(x => x.Patients).NotEmpty()
            .Must(p => p.Count <= 10000)
            .WithMessage("Batch size cannot exceed 10,000 records. Use BulkImportPatients for larger datasets.");

        RuleForEach(x => x.Patients).SetValidator(new CreatePatientValidator());
    }
}
