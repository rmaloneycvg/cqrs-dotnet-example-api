namespace ClinicApi.Domain.Events;

// Patient Events
public record PatientCreated(Guid PatientId, string FirstName, string LastName, string Email);
public record PatientUpdated(Guid PatientId, string FirstName, string LastName);
public record PatientDeactivated(Guid PatientId, string Reason);
public record PatientsBatchImported(int TotalCount, int SuccessCount, int FailureCount);

// Appointment Events
public record AppointmentRequested(Guid AppointmentId, Guid PatientId, string ProviderName, DateTime ScheduledAt);
public record AvailabilityConfirmed(Guid AppointmentId, Guid PatientId, string ProviderName, DateTime ScheduledAt);
public record AvailabilityDenied(Guid AppointmentId, Guid PatientId, string Reason);
public record SlotBooked(Guid AppointmentId, Guid PatientId);
public record BookingFailed(Guid AppointmentId, Guid PatientId, string Reason);
public record AppointmentConfirmed(Guid AppointmentId, Guid PatientId);

// Billing Events
public record InvoiceCreated(Guid InvoiceId, Guid PatientId, decimal Amount, DateTime DueDate);
public record PaymentReceived(Guid PaymentId, Guid InvoiceId, Guid PatientId, decimal Amount);
public record PaymentFailed(Guid PaymentId, Guid InvoiceId, string Reason);
public record InvoiceOverdue(Guid InvoiceId, Guid PatientId, int DaysOverdue);
public record BatchInvoicesCreated(int Count, decimal TotalAmount);

// Prescription Events
public record PrescriptionCreated(Guid PrescriptionId, Guid PatientId, string MedicationName, DateOnly ExpirationDate);
public record PrescriptionRefillReminder(Guid PrescriptionId, Guid PatientId, string MedicationName, int RefillsRemaining);
public record PrescriptionExpiringWarning(Guid PrescriptionId, Guid PatientId, string MedicationName, DateOnly ExpirationDate);
public record PrescriptionDiscontinued(Guid PrescriptionId, Guid PatientId, string Reason);

// Insurance Batch Events
public record PatientInsuranceVerification(Guid PatientId, string PolicyNumber, string ProviderId);
public record UpdateClaimStatus(Guid PatientId, string ClaimId, string NewStatus);
public record InsuranceVerificationBatchCompleted(int ProcessedCount, int SuccessCount, int FailureCount);

// Cache Events (used by SignalR service for subscription-based push)
public record CachePopulated(string EntityType, Guid EntityId, Guid OrganizationId);
public record CacheInvalidated(string EntityType, Guid EntityId, Guid OrganizationId);
