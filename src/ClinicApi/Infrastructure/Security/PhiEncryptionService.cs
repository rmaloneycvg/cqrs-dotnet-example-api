namespace ClinicApi.Infrastructure.Security;

/// <summary>
/// HIPAA §164.312(a)(2)(iv): PHI encryption at rest.
///
/// This project uses SQL Server Always Encrypted for column-level encryption.
/// Encryption/decryption is handled transparently by Microsoft.Data.SqlClient
/// when the connection string includes "Column Encryption Setting=Enabled".
///
/// Architecture:
///   Column Master Key (CMK) → stored in Azure Key Vault / HashiCorp Vault
///   Column Encryption Key (CEK) → stored in SQL Server, encrypted by CMK
///   PHI columns → encrypted with CEK (deterministic or randomized)
///
/// The application never sees plaintext keys or performs manual encrypt/decrypt.
/// The SQL Client driver intercepts parameterized queries and transparently:
///   - Encrypts parameters before sending to SQL Server
///   - Decrypts result columns after receiving from SQL Server
///
/// This works with both EF Core and Dapper — no handler code changes needed.
///
/// Deterministic encryption: allows equality comparisons, indexing, JOINs
///   → Used for: Email (unique index), Status filters
///
/// Randomized encryption: maximum security, no server-side operations
///   → Used for: Names, Phone, Address, Notes, PolicyNumber
///
/// Key rotation: rotate CEK without re-encrypting data (re-encrypt CMK only).
/// Vault sidecar provides CMK credentials to the pod at runtime.
/// </summary>
public static class AlwaysEncryptedInfo
{
    /// <summary>
    /// PHI columns protected by Always Encrypted.
    /// This list documents what is encrypted — the actual encryption is configured
    /// in the SQL Server migration, not in application code.
    /// </summary>
    public static class EncryptedColumns
    {
        // Deterministic (queryable: equality, JOIN, GROUP BY, index)
        public const string PatientEmail = "Patients.Email";

        // Randomized (maximum security, no server-side operations)
        public const string PatientFirstName = "Patients.FirstName";
        public const string PatientLastName = "Patients.LastName";
        public const string PatientPhone = "Patients.PhoneNumber";
        public const string PatientStreet = "Patients.AddressStreet";
        public const string PatientInsurancePolicyNumber = "Patients.InsurancePolicyNumber";
        public const string AppointmentNotes = "Appointments.Notes";
        public const string PrescriptionNotes = "Prescriptions.Notes";
    }
}
