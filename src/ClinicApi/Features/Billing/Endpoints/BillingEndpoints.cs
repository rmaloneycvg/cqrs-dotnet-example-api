using ClinicApi.Features.Billing.Commands;
using ClinicApi.Features.Billing.Queries;
using ClinicApi.Features.Billing.Saga;
using Wolverine;
using Wolverine.Http;

namespace ClinicApi.Features.Billing.Endpoints;

/// <summary>
/// HTTP endpoints for Billing operations.
/// Covers invoice CRUD, batch operations, payment saga, and read-side queries.
/// </summary>
public static class BillingEndpoints
{
    // ─── Invoices ────────────────────────────────────────────────────────────

    [WolverinePost("/api/invoices")]
    public static Task<CreateInvoiceResponse> CreateInvoice(CreateInvoice command, IMessageBus bus)
        => bus.InvokeAsync<CreateInvoiceResponse>(command);

    [WolverineGet("/api/patients/{patientId}/billing")]
    public static Task<BillingHistoryResponse> GetPatientBilling(
        Guid patientId, int page, int pageSize, IMessageBus bus)
        => bus.InvokeAsync<BillingHistoryResponse>(new GetPatientBillingHistory(
            patientId, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize));

    [WolverineGet("/api/billing/outstanding")]
    public static Task<OutstandingBalancesResponse> GetOutstandingBalances(
        decimal minBalance, int page, int pageSize, IMessageBus bus)
        => bus.InvokeAsync<OutstandingBalancesResponse>(new GetOutstandingBalances(
            minBalance == 0 ? 0.01m : minBalance,
            page == 0 ? 1 : page,
            pageSize == 0 ? 50 : pageSize));

    // ─── Batch Operations ────────────────────────────────────────────────────

    [WolverinePost("/api/invoices/batch-create")]
    public static Task<BatchCreateInvoicesResponse> BatchCreateInvoices(
        BatchCreateInvoices command, IMessageBus bus)
        => bus.InvokeAsync<BatchCreateInvoicesResponse>(command);

    [WolverinePost("/api/invoices/mark-overdue")]
    public static Task<BatchUpdateInvoiceStatusesResponse> MarkOverdueInvoices(
        BatchUpdateInvoiceStatuses command, IMessageBus bus)
        => bus.InvokeAsync<BatchUpdateInvoiceStatusesResponse>(command);

    // ─── Payments ────────────────────────────────────────────────────────────

    [WolverinePost("/api/payments")]
    public static Task SubmitPayment(SubmitPaymentCommand command, IMessageBus bus)
        => bus.InvokeAsync(command);
}
