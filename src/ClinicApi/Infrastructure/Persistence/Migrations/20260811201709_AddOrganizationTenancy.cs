using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApi.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
/// <summary>
/// Adds OrganizationId (BOLA protection) to all domain entities.
///
/// OWASP API1:2023 — Broken Object-Level Authorization.
/// Every entity is now scoped to an organization. EF Core global query filters
/// and Dapper parameterized queries enforce tenant isolation automatically.
///
/// Existing data is backfilled with a default organization ID.
/// Production deployments should update this to the real org IDs before removing the default.
/// </summary>
public partial class AddOrganizationTenancy : Migration
{
    // Default org for existing data. In production, run a data migration
    // to assign correct org IDs before dropping this default.
    private static readonly Guid DefaultOrgId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Patients_Email",
            table: "Patients");

        migrationBuilder.DropIndex(
            name: "IX_Patients_Status",
            table: "Patients");

        // ─── Add OrganizationId columns with default for existing rows ───────

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Prescriptions",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: DefaultOrgId);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Payments",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: DefaultOrgId);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Patients",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: DefaultOrgId);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Invoices",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: DefaultOrgId);

        migrationBuilder.AddColumn<Guid>(
            name: "OrganizationId",
            table: "Appointments",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: DefaultOrgId);

        // ─── Indexes for tenant-scoped queries ───────────────────────────────

        migrationBuilder.CreateIndex(
            name: "IX_Prescriptions_OrganizationId",
            table: "Prescriptions",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_OrganizationId",
            table: "Payments",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Patients_OrganizationId",
            table: "Patients",
            column: "OrganizationId");

        // Email uniqueness is now per-org (two orgs can have the same patient email)
        migrationBuilder.CreateIndex(
            name: "IX_Patients_OrganizationId_Email",
            table: "Patients",
            columns: new[] { "OrganizationId", "Email" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Patients_OrganizationId_Status",
            table: "Patients",
            columns: new[] { "OrganizationId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_Invoices_OrganizationId",
            table: "Invoices",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_Appointments_OrganizationId",
            table: "Appointments",
            column: "OrganizationId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Prescriptions_OrganizationId",
            table: "Prescriptions");

        migrationBuilder.DropIndex(
            name: "IX_Payments_OrganizationId",
            table: "Payments");

        migrationBuilder.DropIndex(
            name: "IX_Patients_OrganizationId",
            table: "Patients");

        migrationBuilder.DropIndex(
            name: "IX_Patients_OrganizationId_Email",
            table: "Patients");

        migrationBuilder.DropIndex(
            name: "IX_Patients_OrganizationId_Status",
            table: "Patients");

        migrationBuilder.DropIndex(
            name: "IX_Invoices_OrganizationId",
            table: "Invoices");

        migrationBuilder.DropIndex(
            name: "IX_Appointments_OrganizationId",
            table: "Appointments");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "Prescriptions");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "Payments");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "Patients");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "Invoices");

        migrationBuilder.DropColumn(
            name: "OrganizationId",
            table: "Appointments");

        migrationBuilder.CreateIndex(
            name: "IX_Patients_Email",
            table: "Patients",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Patients_Status",
            table: "Patients",
            column: "Status");
    }
}
