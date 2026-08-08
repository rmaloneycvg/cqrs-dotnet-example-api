using ClinicApi.Infrastructure.Persistence;
using ClinicApi.Infrastructure.Security;
using Dapper;

namespace ClinicApi.Features.Patients.Queries;

/// <summary>
/// Query handlers use Dapper for read-side performance.
/// Always Encrypted compatible: no server-side operations on encrypted columns.
/// FullName computed client-side. Search uses in-memory filtering post-decryption.
/// </summary>
public static class GetPatientByIdHandler
{
    public static async Task<PatientDto?> Handle(
        GetPatientById query,
        IDbConnectionFactory connectionFactory,
        IPhiAuditLogger audit)
    {
        const string sql = """
            SELECT Id, FirstName, LastName, DateOfBirth, Email, PhoneNumber,
                   CASE Status
                       WHEN 0 THEN 'Active'
                       WHEN 1 THEN 'Inactive'
                       WHEN 2 THEN 'Deceased'
                       WHEN 3 THEN 'Transferred'
                       ELSE 'Unknown'
                   END AS Status,
                   AddressCity AS City, AddressState AS State,
                   CreatedAt, UpdatedAt
            FROM Patients
            WHERE Id = @Id
            """;

        using var connection = connectionFactory.CreateConnection();
        var raw = await connection.QuerySingleOrDefaultAsync<PatientRawDto>(sql, new { query.Id });

        if (raw is null) return null;

        audit.LogAccess("system", "Read", "Patient", raw.Id, "GetPatientById");

        return raw.ToDto();
    }
}

public static class ListPatientsHandler
{
    public static async Task<PatientListResponse> Handle(
        ListPatients query,
        IDbConnectionFactory connectionFactory,
        IPhiAuditLogger audit)
    {
        var offset = (query.Page - 1) * query.PageSize;

        // Always Encrypted: cannot use LIKE on encrypted columns.
        // Filter by Status (unencrypted) server-side, search term applied client-side.
        var whereClause = "WHERE 1=1";
        if (query.Status is not null)
            whereClause += " AND Status = @StatusInt";

        var countSql = $"SELECT COUNT(*) FROM Patients {whereClause}";

        // Fetch a larger window for client-side search filtering
        var fetchSize = query.SearchTerm is not null ? query.PageSize * 5 : query.PageSize;
        var dataSql = $"""
            SELECT Id, FirstName, LastName, DateOfBirth, Email, PhoneNumber,
                   CASE Status
                       WHEN 0 THEN 'Active'
                       WHEN 1 THEN 'Inactive'
                       WHEN 2 THEN 'Deceased'
                       WHEN 3 THEN 'Transferred'
                       ELSE 'Unknown'
                   END AS Status,
                   AddressCity AS City, AddressState AS State,
                   CreatedAt, UpdatedAt
            FROM Patients
            {whereClause}
            ORDER BY CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @FetchSize ROWS ONLY
            """;

        var statusInt = query.Status switch
        {
            "Active" => 0, "Inactive" => 1, "Deceased" => 2, "Transferred" => 3, _ => (int?)null
        };

        var parameters = new
        {
            StatusInt = statusInt,
            Offset = offset,
            FetchSize = fetchSize
        };

        using var connection = connectionFactory.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var rawItems = (await connection.QueryAsync<PatientRawDto>(dataSql, parameters)).ToList();

        var items = rawItems.Select(r => r.ToDto()).ToList();

        // Client-side search on decrypted fields (Always Encrypted decrypts at the driver)
        if (query.SearchTerm is not null)
        {
            var search = query.SearchTerm;
            items = items.Where(p =>
                p.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Take(query.PageSize)
                .ToList();
        }

        audit.LogBulkAccess("system", "List", "Patient", items.Count);

        return new PatientListResponse(
            items,
            totalCount,
            query.Page,
            query.PageSize,
            (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }
}

/// <summary>
/// Internal DTO for Dapper mapping. FullName computed client-side
/// because SQL Server cannot concatenate Always Encrypted columns.
/// </summary>
internal record PatientRawDto(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string PhoneNumber,
    string Status,
    string City,
    string State,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public PatientDto ToDto() => new(
        Id, FirstName, LastName,
        $"{FirstName} {LastName}",
        DateOfBirth, Email, PhoneNumber,
        Status, City, State, CreatedAt, UpdatedAt);
}
