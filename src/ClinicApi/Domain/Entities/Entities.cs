using ClinicApi.Domain.ValueObjects;

namespace ClinicApi.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Email { get; set; } = string.Empty;
    public PhoneNumber Phone { get; set; } = new(string.Empty);
    public Address Address { get; set; } = new(string.Empty, string.Empty, string.Empty, string.Empty);
    public InsuranceInfo? Insurance { get; set; }
    public PatientStatus Status { get; set; } = PatientStatus.Active;
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAppointmentAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}

public enum PatientStatus
{
    Active,
    Inactive,
    Deceased,
    Transferred
}

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30);
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Requested;
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum AppointmentStatus
{
    Requested,
    AvailabilityChecked,
    Booked,
    Confirmed,
    InProgress,
    Completed,
    Cancelled,
    NoShow
}

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Money Amount { get; set; } = Money.Zero;
    public Money PaidAmount { get; set; } = Money.Zero;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Money OutstandingBalance => Amount - PaidAmount;
}

public enum InvoiceStatus
{
    Draft,
    Issued,
    Paid,
    PartiallyPaid,
    Overdue,
    Cancelled,
    WrittenOff
}

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public Guid PatientId { get; set; }
    public Money Amount { get; set; } = Money.Zero;
    public PaymentMethod Method { get; set; } = PaymentMethod.CreditCard;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ProcessedAt { get; set; }
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    Insurance,
    Cash,
    Check,
    BankTransfer
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Refunded
}

public class Prescription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int RefillsRemaining { get; set; }
    public int TotalRefills { get; set; }
    public DateOnly PrescribedDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Active;
    public string PrescriberName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum PrescriptionStatus
{
    Active,
    Expired,
    Discontinued,
    Completed
}
