using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApi.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Appointments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProviderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Duration = table.Column<TimeSpan>(type: "time", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Appointments", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Invoices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                PaidCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Invoices", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                PhoneType = table.Column<int>(type: "int", nullable: false),
                AddressStreet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                AddressCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                AddressState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                AddressZipCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                AddressCountry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                InsuranceProviderId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                InsuranceProviderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                InsurancePolicyNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                InsuranceGroupNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                InsuranceEffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                InsuranceExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastAppointmentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Patients", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                Method = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Prescriptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MedicationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                RefillsRemaining = table.Column<int>(type: "int", nullable: false),
                TotalRefills = table.Column<int>(type: "int", nullable: false),
                PrescribedDate = table.Column<DateOnly>(type: "date", nullable: false),
                ExpirationDate = table.Column<DateOnly>(type: "date", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                PrescriberName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Prescriptions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_PatientId",
            table: "Appointments",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_ProviderName_ScheduledAt",
            table: "Appointments",
            columns: new[] { "ProviderName", "ScheduledAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_Status",
            table: "Appointments",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_DueDate",
            table: "Invoices",
            column: "DueDate");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_InvoiceNumber",
            table: "Invoices",
            column: "InvoiceNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_PatientId",
            table: "Invoices",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_Status",
            table: "Invoices",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Patients_Email",
            table: "Patients",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Patients_ExternalId",
            table: "Patients",
            column: "ExternalId",
            unique: true,
            filter: "[ExternalId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Patients_LastAppointmentAt",
            table: "Patients",
            column: "LastAppointmentAt");

        migrationBuilder.CreateIndex(
            name: "IX_Patients_Status",
            table: "Patients",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_InvoiceId",
            table: "Payments",
            column: "InvoiceId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_PatientId",
            table: "Payments",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_TransactionId",
            table: "Payments",
            column: "TransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_Prescriptions_ExpirationDate",
            table: "Prescriptions",
            column: "ExpirationDate");

        migrationBuilder.CreateIndex(
            name: "IX_Prescriptions_PatientId",
            table: "Prescriptions",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_Prescriptions_Status",
            table: "Prescriptions",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Appointments");

        migrationBuilder.DropTable(
            name: "Invoices");

        migrationBuilder.DropTable(
            name: "Patients");

        migrationBuilder.DropTable(
            name: "Payments");

        migrationBuilder.DropTable(
            name: "Prescriptions");
    }
}
