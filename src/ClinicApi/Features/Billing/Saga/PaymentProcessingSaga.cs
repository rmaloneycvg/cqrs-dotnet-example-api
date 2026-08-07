using ClinicApi.Domain.Entities;
using ClinicApi.Domain.Events;
using ClinicApi.Domain.ValueObjects;
using ClinicApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace ClinicApi.Features.Billing.Saga;

/// <summary>
/// Payment Processing Saga — orchestrates the payment workflow:
///   SubmitPayment (start) → validate → process charge → generate receipt → complete
/// 
/// Compensation on failure:
///   PaymentFailed → mark invoice as unpaid, notify patient
/// </summary>
public class PaymentProcessingSaga : Wolverine.Saga
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid PatientId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string PaymentState { get; set; } = "Initiated";

    // ─── Start ──────────────────────────────────────────────────────────────

    public static (PaymentProcessingSaga, PaymentValidationRequested) Start(SubmitPaymentCommand command)
    {
        var saga = new PaymentProcessingSaga
        {
            Id = Guid.NewGuid(),
            InvoiceId = command.InvoiceId,
            PatientId = command.PatientId,
            Amount = command.Amount,
            Method = command.Method,
            PaymentState = "Validating"
        };

        return (saga, new PaymentValidationRequested(saga.Id, command.InvoiceId, command.Amount));
    }

    // ─── Step Handlers ──────────────────────────────────────────────────────

    public PaymentChargeRequested Handle(PaymentValidated validated)
    {
        PaymentState = "Charging";
        return new PaymentChargeRequested(Id, InvoiceId, Amount, Method.ToString());
    }

    public async Task<PaymentReceived> Handle(
        PaymentCharged charged,
        ClinicDbContext db,
        CancellationToken cancellationToken)
    {
        PaymentState = "Charged";

        // Record the payment
        var payment = new Payment
        {
            Id = Id,
            InvoiceId = InvoiceId,
            PatientId = PatientId,
            Amount = new Money(Amount),
            Method = Method,
            Status = PaymentStatus.Completed,
            TransactionId = charged.TransactionId,
            ProcessedAt = DateTime.UtcNow
        };

        db.Payments.Add(payment);

        // Update invoice paid amount
        var invoice = await db.Invoices.FindAsync([InvoiceId], cancellationToken);
        if (invoice is not null)
        {
            invoice.PaidAmount = new Money(invoice.PaidAmount.Amount + Amount);
            invoice.Status = invoice.PaidAmount.Amount >= invoice.Amount.Amount
                ? InvoiceStatus.Paid
                : InvoiceStatus.PartiallyPaid;
            invoice.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);

        MarkCompleted();
        return new PaymentReceived(Id, InvoiceId, PatientId, Amount);
    }

    // ─── Compensation ───────────────────────────────────────────────────────

    public PaymentFailed Handle(PaymentChargeFailed failed, ClinicDbContext db)
    {
        PaymentState = "Failed";

        // Record the failed payment attempt
        var payment = new Payment
        {
            Id = Id,
            InvoiceId = InvoiceId,
            PatientId = PatientId,
            Amount = new Money(Amount),
            Method = Method,
            Status = PaymentStatus.Failed,
            FailureReason = failed.Reason,
            ProcessedAt = DateTime.UtcNow
        };

        db.Payments.Add(payment);
        MarkCompleted();

        return new PaymentFailed(Id, InvoiceId, failed.Reason);
    }
}

// ─── Saga Messages ──────────────────────────────────────────────────────────

public record SubmitPaymentCommand(Guid InvoiceId, Guid PatientId, decimal Amount, PaymentMethod Method);
public record PaymentValidationRequested(Guid SagaId, Guid InvoiceId, decimal Amount);
public record PaymentValidated(Guid SagaId);
public record PaymentChargeRequested(Guid SagaId, Guid InvoiceId, decimal Amount, string Method);
public record PaymentCharged(Guid SagaId, string TransactionId);
public record PaymentChargeFailed(Guid SagaId, string Reason);
