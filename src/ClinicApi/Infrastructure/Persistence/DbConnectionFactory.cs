using System.Data;
using Microsoft.Data.SqlClient;

namespace ClinicApi.Infrastructure.Persistence;

/// <summary>
/// Factory for creating Dapper database connections (read-side).
/// Dapper is used for query operations where performance matters over change tracking.
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
