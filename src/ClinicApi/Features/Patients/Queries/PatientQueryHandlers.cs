using ClinicApi.Domain.Events;
using ClinicApi.Infrastructure.Caching;
using ClinicApi.Infrastructure.Persistence;
using ClinicApi.Infrastructure.Security;
using Dapper;

namespace ClinicApi.Features.Patients.Queries;

/// <summary>
/// Query handlers use Dapper for read-side performance.
/// Always Encrypted compatible: no server-side operations on encrypted columns.
/// PHI audit logging handled by PhiAuditMiddleware (no per-handler injection).
///
/// BOLA Protection: All queries are scoped to the authenticated user's OrganizationId.
/// Cross-tenant users (admin with org_id="*") bypass the filter.
///
/// Caching: When configured, GetPatientById checks Redis first (PHI encrypted at rest).
/// On cache miss, queries DB, caches the result, and publishes CachePopulated event.
/// </summary>
public static class GetPatientByIdHandler
{
    public static async Task<(PatientDto?, CachePopulated?)> Handle(
        GetPatientById query,
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        var orgId = tenantContext.OrganizationId ?? Guid.Empty;

        // Check cache first (if configured for this endpoint)
        var cached = await cacheService.GetAsync<PatientDto>("GetPatientById", orgId, query.Id);
        if (cached is not null)
            return (cached, null); // Cache hit — no event needed

        // Cache miss — query database
        var orgFilter = tenantContext.OrganizationId is not null
            ? "AND OrganizationId = @OrganizationId"
            : "";

        var sql = $"""
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
            WHERE Id = @Id {orgFilter}
            """;

        using var connection = connectionFactory.CreateConnection();
        var raw = await connection.QuerySingleOrDefaultAsync<PatientRawDto>(
            sql, new { query.Id, tenantContext.OrganizationId });

        if (raw is null)
            return (null, null);

        var dto = raw.ToDto();

        // Store in cache (encrypted PHI, plaintext metadata)
        await cacheService.SetAsync("GetPatientById", orgId, query.Id, dto);

        // Publish CachePopulated event for SignalR service awareness
        var cacheEvent = new CachePopulated("Patient", query.Id, orgId);
        return (dto, cacheEvent);
    }
}

public static class ListPatientsHandler
{
    public static async Task<PatientListResponse> Handle(
        ListPatients query,
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext)
    {
        var offset = (query.Page - 1) * query.PageSize;

        // BOLA: always scope to organization
        var whereClause = tenantContext.OrganizationId is not null
            ? "WHERE OrganizationId = @OrganizationId"
            : "WHERE 1=1";

        // Always Encrypted: cannot use LIKE on encrypted columns.
        // Filter by Status (unencrypted) server-side, search term applied client-side.
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
            "Active" => 0,
            "Inactive" => 1,
            "Deceased" => 2,
            "Transferred" => 3,
            _ => (int?)null
        };

        var parameters = new
        {
            tenantContext.OrganizationId,
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

        return new PatientListResponse(
            items,
            totalCount,
            query.Page,
            query.PageSize,
            (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }
}

/// <summary>
/// Internal DTO for Dapper mapping. Uses types Dapper can materialize from SQL Server.
/// FullName computed client-side (can't concatenate Always Encrypted columns).
/// </summary>
internal class PatientRawDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PatientDto ToDto() => new(
        Id, FirstName, LastName,
        $"{FirstName} {LastName}",
        DateOnly.FromDateTime(DateOfBirth),
        Email, PhoneNumber,
        Status, City, State, CreatedAt, UpdatedAt);
}
