using System.Data;
using ClinicApi.Domain.Entities;
using ClinicApi.Domain.Events;
using ClinicApi.Domain.ValueObjects;
using ClinicApi.Infrastructure.Persistence;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace ClinicApi.Features.Billing.Commands;

// ─── Commands ────────────────────────────────────────────────────────────────

public record CreateInvoice(Guid PatientId, decimal Amount, string Description, DateTime DueDate);
public record CreateInvoiceResponse(Guid Id, string InvoiceNumber, decimal Amount, DateTime DueDate);

public record BatchCreateInvoices(decimal MonthlyAmount, string Description);
public record BatchCreateInvoicesResponse(int CreatedCount, decimal TotalAmount);

public record BatchUpdateInvoiceStatuses(int OverdueDaysThreshold = 30);
public record BatchUpdateInvoiceStatusesResponse(int UpdatedCount);

// ─── Handlers ────────────────────────────────────────────────────────────────

public static class CreateInvoiceHandler
{
    public static async Task<(CreateInvoiceResponse, InvoiceCreated)> Handle(
        CreateInvoice command,
        ClinicDbContext db,
        CancellationToken cancellationToken)
    {
        var invoice = new Invoice
        {
            PatientId = command.PatientId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            Amount = new Money(command.Amount),
            PaidAmount = Money.Zero,
            Status = InvoiceStatus.Issued,
            Description = command.Description,
            DueDate = command.DueDate,
            IssuedAt = DateTime.UtcNow
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);

        var response = new CreateInvoiceResponse(invoice.Id, invoice.InvoiceNumber, command.Amount, command.DueDate);
        var @event = new InvoiceCreated(invoice.Id, command.PatientId, command.Amount, command.DueDate);

        return (response, @event);
    }
}

/// <summary>
/// Batch invoice creation using SqlBulkCopy.
/// Generates monthly invoices for all active patients at once.
/// </summary>
public static class BatchCreateInvoicesHandler
{
    public static async Task<(BatchCreateInvoicesResponse, BatchInvoicesCreated)> Handle(
        BatchCreateInvoices command,
        IDbConnectionFactory connectionFactory,
        IBulkInsertService bulkInsertService,
        CancellationToken cancellationToken)
    {
        // First, get all active patient IDs
        using var connection = connectionFactory.CreateConnection();
        var patientIds = (await connection.QueryAsync<Guid>(
            "SELECT Id FROM Patients WHERE Status = @Status",
            new { Status = (int)PatientStatus.Active })).ToList();

        if (patientIds.Count == 0)
            return (new BatchCreateInvoicesResponse(0, 0), new BatchInvoicesCreated(0, 0));

        // Build DataTable for bulk insert
        var dataTable = new DataTable();
        dataTable.Columns.Add("Id", typeof(Guid));
        dataTable.Columns.Add("PatientId", typeof(Guid));
        dataTable.Columns.Add("InvoiceNumber", typeof(string));
        dataTable.Columns.Add("Amount", typeof(decimal));
        dataTable.Columns.Add("Currency", typeof(string));
        dataTable.Columns.Add("PaidAmount", typeof(decimal));
        dataTable.Columns.Add("PaidCurrency", typeof(string));
        dataTable.Columns.Add("Status", typeof(int));
        dataTable.Columns.Add("IssuedAt", typeof(DateTime));
        dataTable.Columns.Add("DueDate", typeof(DateTime));
        dataTable.Columns.Add("Description", typeof(string));
        dataTable.Columns.Add("CreatedAt", typeof(DateTime));
        dataTable.Columns.Add("UpdatedAt", typeof(DateTime));

        var now = DateTime.UtcNow;
        var dueDate = now.AddDays(30);
        var batchId = now.ToString("yyyyMMdd");

        foreach (var patientId in patientIds)
        {
            dataTable.Rows.Add(
                Guid.NewGuid(),
                patientId,
                $"INV-{batchId}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                command.MonthlyAmount,
                "USD",
                0m,
                "USD",
                (int)InvoiceStatus.Issued,
                now,
                dueDate,
                command.Description,
                now,
                now);
        }

        await bulkInsertService.BulkInsertAsync<Invoice>(
            dataTable, "Invoices", cancellationToken: cancellationToken);

        var totalAmount = command.MonthlyAmount * patientIds.Count;
        return (
            new BatchCreateInvoicesResponse(patientIds.Count, totalAmount),
            new BatchInvoicesCreated(patientIds.Count, totalAmount));
    }
}

/// <summary>
/// Batch update invoice statuses using EF Core ExecuteUpdate.
/// Marks overdue invoices without loading them into memory.
/// </summary>
public static class BatchUpdateInvoiceStatusesHandler
{
    public static async Task<BatchUpdateInvoiceStatusesResponse> Handle(
        BatchUpdateInvoiceStatuses command,
        ClinicDbContext db,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-command.OverdueDaysThreshold);

        var updated = await db.Invoices
            .Where(i => i.Status == InvoiceStatus.Issued)
            .Where(i => i.DueDate < cutoff)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Status, InvoiceStatus.Overdue)
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

        // Publish events for each overdue invoice (for notification purposes)
        if (updated > 0)
        {
            var overdueInvoices = await db.Invoices
                .Where(i => i.Status == InvoiceStatus.Overdue)
                .Where(i => i.UpdatedAt >= DateTime.UtcNow.AddSeconds(-5))
                .Select(i => new { i.Id, i.PatientId, i.DueDate })
                .ToListAsync(cancellationToken);

            foreach (var invoice in overdueInvoices)
            {
                var daysOverdue = (DateTime.UtcNow - invoice.DueDate).Days;
                await messageBus.PublishAsync(
                    new InvoiceOverdue(invoice.Id, invoice.PatientId, daysOverdue));
            }
        }

        return new BatchUpdateInvoiceStatusesResponse(updated);
    }
}
