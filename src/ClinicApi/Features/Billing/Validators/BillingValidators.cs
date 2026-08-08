using ClinicApi.Features.Billing.Commands;
using FluentValidation;

namespace ClinicApi.Features.Billing.Validators;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoice>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThan(1_000_000);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DueDate).GreaterThan(DateTime.UtcNow.AddDays(-1))
            .WithMessage("Due date cannot be in the past");
    }
}

public class BatchCreateInvoicesValidator : AbstractValidator<BatchCreateInvoices>
{
    public BatchCreateInvoicesValidator()
    {
        RuleFor(x => x.MonthlyAmount).GreaterThan(0).LessThan(100_000);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
    }
}

public class BatchUpdateInvoiceStatusesValidator : AbstractValidator<BatchUpdateInvoiceStatuses>
{
    public BatchUpdateInvoiceStatusesValidator()
    {
        RuleFor(x => x.OverdueDaysThreshold).InclusiveBetween(1, 365);
    }
}
