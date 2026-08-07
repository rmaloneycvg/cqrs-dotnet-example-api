using ClinicApi.Infrastructure.Persistence;
using Dapper;

namespace ClinicApi.Features.Patients.Queries;

/// <summary>
/// Query handlers use Dapper for read-side performance.
/// No change tracking, no proxy generation — just fast SQL.
/// </summary>
public static class GetPatientByIdHandler
{
    public static async Task<PatientDto?> Handle(
        GetPatientById query,
        IDbConnectionFactory connectionFactory)
    {
        const string sql = """
            SELECT Id, FirstName, LastName, FirstName + ' ' + LastName AS FullName,
                   DateOfBirth, Email, PhoneNumber, Status, AddressCity AS City,
                   AddressState AS State, CreatedAt, UpdatedAt
            FROM Patients
            WHERE Id = @Id
            """;

        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<PatientDto>(sql, new { query.Id });
    }
}

public static class ListPatientsHandler
{
    public static async Task<PatientListResponse> Handle(
        ListPatients query,
        IDbConnectionFactory connectionFactory)
    {
        var offset = (query.Page - 1) * query.PageSize;

        var whereClause = "WHERE 1=1";
        if (query.Status is not null)
            whereClause += " AND Status = @Status";
        if (query.SearchTerm is not null)
            whereClause += " AND (FirstName LIKE @Search OR LastName LIKE @Search OR Email LIKE @Search)";

        var countSql = $"SELECT COUNT(*) FROM Patients {whereClause}";
        var dataSql = $"""
            SELECT Id, FirstName, LastName, FirstName + ' ' + LastName AS FullName,
                   DateOfBirth, Email, PhoneNumber, Status, AddressCity AS City,
                   AddressState AS State, CreatedAt, UpdatedAt
            FROM Patients
            {whereClause}
            ORDER BY LastName, FirstName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var parameters = new
        {
            query.Status,
            Search = query.SearchTerm is not null ? $"%{query.SearchTerm}%" : null,
            Offset = offset,
            query.PageSize
        };

        using var connection = connectionFactory.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await connection.QueryAsync<PatientDto>(dataSql, parameters)).ToList();

        return new PatientListResponse(
            items,
            totalCount,
            query.Page,
            query.PageSize,
            (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }
}
