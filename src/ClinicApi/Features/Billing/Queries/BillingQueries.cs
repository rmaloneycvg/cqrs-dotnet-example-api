using ClinicApi.Infrastructure.Persistence;
using Dapper;

namespace ClinicApi.Features.Billing.Queries;

public record GetPatientBillingHistory(Guid PatientId, int Page = 1, int PageSize = 20);
public record GetOutstandingBalances(decimal MinimumBalance = 0.01m);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    decimal Amount,
    decimal PaidAmount,
    decimal OutstandingBalance,
    string Status,
    DateTime IssuedAt,
    DateTime DueDate,
    string? Description);

public record BillingHistoryResponse(List<InvoiceDto> Invoices, int TotalCount, decimal TotalOutstanding);

public record OutstandingBalanceDto(Guid PatientId, string PatientName, decimal TotalOutstanding, int InvoiceCount);

public static class GetPatientBillingHistoryHandler
{
    public static async Task<BillingHistoryResponse> Handle(
        GetPatientBillingHistory query,
        IDbConnectionFactory connectionFactory)
    {
        const string sql = """
            SELECT Id, InvoiceNumber, Amount, PaidAmount,
                   (Amount - PaidAmount) AS OutstandingBalance,
                   Status, IssuedAt, DueDate, Description
            FROM Invoices
            WHERE PatientId = @PatientId
            ORDER BY IssuedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*) FROM Invoices WHERE PatientId = @PatientId;
            SELECT ISNULL(SUM(Amount - PaidAmount), 0) FROM Invoices
            WHERE PatientId = @PatientId AND Status IN (1, 3, 4);
            """;

        var parameters = new
        {
            query.PatientId,
            Offset = (query.Page - 1) * query.PageSize,
            query.PageSize
        };

        using var connection = connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var invoices = (await multi.ReadAsync<InvoiceDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();
        var totalOutstanding = await multi.ReadSingleAsync<decimal>();

        return new BillingHistoryResponse(invoices, totalCount, totalOutstanding);
    }
}

public static class GetOutstandingBalancesHandler
{
    public static async Task<List<OutstandingBalanceDto>> Handle(
        GetOutstandingBalances query,
        IDbConnectionFactory connectionFactory)
    {
        const string sql = """
            SELECT p.Id AS PatientId,
                   p.FirstName + ' ' + p.LastName AS PatientName,
                   SUM(i.Amount - i.PaidAmount) AS TotalOutstanding,
                   COUNT(i.Id) AS InvoiceCount
            FROM Patients p
            INNER JOIN Invoices i ON p.Id = i.PatientId
            WHERE i.Status IN (1, 3, 4)
            GROUP BY p.Id, p.FirstName, p.LastName
            HAVING SUM(i.Amount - i.PaidAmount) >= @MinimumBalance
            ORDER BY TotalOutstanding DESC
            """;

        using var connection = connectionFactory.CreateConnection();
        return (await connection.QueryAsync<OutstandingBalanceDto>(sql, new { query.MinimumBalance })).ToList();
    }
}
