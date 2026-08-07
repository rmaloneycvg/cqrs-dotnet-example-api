using System.Data;
using Microsoft.Data.SqlClient;

namespace ClinicApi.Infrastructure.Persistence;

/// <summary>
/// High-performance bulk insert service using SqlBulkCopy.
/// Used for importing large datasets (10k+ rows) where EF Core AddRange is too slow.
/// </summary>
public interface IBulkInsertService
{
    Task BulkInsertAsync<T>(
        DataTable dataTable,
        string tableName,
        int batchSize = 5000,
        int timeoutSeconds = 120,
        CancellationToken cancellationToken = default);
}

public class SqlBulkInsertService : IBulkInsertService
{
    private readonly string _connectionString;

    public SqlBulkInsertService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task BulkInsertAsync<T>(
        DataTable dataTable,
        string tableName,
        int batchSize = 5000,
        int timeoutSeconds = 120,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = tableName,
            BatchSize = batchSize,
            BulkCopyTimeout = timeoutSeconds,
            EnableStreaming = true // ensures inserts happen in batches instead of all at once
        };

        // Map columns from DataTable to destination table
        foreach (DataColumn column in dataTable.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
    }

    public async Task StreamingBulkInsertAsync(
        IDataReader dataReader,
        string tableName,
        int batchSize = 5000,
        int timeoutSeconds = 120,
        CancellationToken cancellationToken = default)
    {
        // 1. Open the connection
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // 2. Configure SqlBulkCopy
        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = tableName,
            BatchSize = batchSize,
            BulkCopyTimeout = timeoutSeconds,
            EnableStreaming = true // True streaming is unlocked by IDataReader
        };

        // 3. Map columns dynamically from the IDataReader schema
        // This ensures data goes into the correct SQL columns regardless of order
        for (int i = 0; i < dataReader.FieldCount; i++)
        {
            string columnName = dataReader.GetName(i);
            bulkCopy.ColumnMappings.Add(columnName, columnName);
        }

        // 4. Execute the stream
        // The data is pulled from the reader and pushed to SQL Server row-by-row
        await bulkCopy.WriteToServerAsync(dataReader, cancellationToken);
    }

    public async Task StreamingBulkUpdateAsync(
        IDataReader dataReader, 
        string tableName, 
        string primaryKeyColumn, 
        int timeoutSeconds = 120, 
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        string tempTableName = $"#TempUpdate_{Guid.NewGuid():N}";

        string createTempSql = $"SELECT TOP 0 * INTO {tempTableName} FROM {tableName}";
        await using (var createCmd = new SqlCommand(createTempSql, connection))
        {
            await createCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // 1. Extract column names dynamically from the stream's schema
        var columnNames = new List<string>();
        for (int i = 0; i < dataReader.FieldCount; i++)
        {
            columnNames.Add(dataReader.GetName(i));
        }

        // 2. Stream the data to SQL Server
        using (var bulkCopy = new SqlBulkCopy(connection))
        {
            bulkCopy.DestinationTableName = tempTableName;
            bulkCopy.BulkCopyTimeout = timeoutSeconds;
            bulkCopy.EnableStreaming = true; // <-- Now this actually works!

            foreach (var columnName in columnNames)
            {
                bulkCopy.ColumnMappings.Add(columnName, columnName);
            }

            // Writes directly from the stream to the database, row by row
            await bulkCopy.WriteToServerAsync(dataReader, cancellationToken);
        }

        // 3. Build the UPDATE query dynamically using the extracted column names
        var columnsToUpdate = columnNames
            .Where(c => !c.Equals(primaryKeyColumn, StringComparison.OrdinalIgnoreCase))
            .ToList();

        string setClause = string.Join(", ", columnsToUpdate.Select(c => $"Target.[{c}] = Source.[{c}]"));

        string updateSql = $@"
            UPDATE Target
            SET {setClause}
            FROM {tableName} Target
            INNER JOIN {tempTableName} Source 
                ON Target.[{primaryKeyColumn}] = Source.[{primaryKeyColumn}];
            
            DROP TABLE {tempTableName};";

        await using (var updateCmd = new SqlCommand(updateSql, connection))
        {
            updateCmd.CommandTimeout = timeoutSeconds;
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
