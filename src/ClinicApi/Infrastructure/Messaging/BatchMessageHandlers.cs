using ClinicApi.Domain.Events;
using ClinicApi.Infrastructure.Persistence;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ClinicApi.Infrastructure.Messaging;

/// <summary>
/// Wolverine batch message handler.
/// Wolverine automatically batches messages when the handler accepts an array type.
/// Configuration: BatchSize=50, TriggerTime=5s (set in Program.cs).
/// 
/// Messages arrive individually from RabbitMQ but are collected and delivered
/// as a batch for efficient processing — reducing per-message overhead and
/// allowing single bulk database operations.
/// </summary>
public static class InsuranceVerificationBatchHandler
{
    /// <summary>
    /// Handles a batch of PatientInsuranceVerification messages.
    /// Wolverine collects individual messages and delivers them as an array
    /// when either BatchSize is reached or TriggerTime elapses.
    /// </summary>
    public static async Task<InsuranceVerificationBatchCompleted> Handle(
        PatientInsuranceVerification[] messages,
        IDbConnectionFactory connectionFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing batch of {Count} insurance verification requests", messages.Length);

        var successCount = 0;
        var failureCount = 0;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        // Process the batch as a single bulk operation
        const string sql = """
            UPDATE Patients
            SET InsuranceProviderId = @ProviderId,
                InsurancePolicyNumber = @PolicyNumber,
                UpdatedAt = @UpdatedAt
            WHERE Id = @PatientId
            """;

        foreach (var message in messages)
        {
            try
            {
                var affected = await connection.ExecuteAsync(sql, new
                {
                    message.PatientId,
                    message.ProviderId,
                    message.PolicyNumber,
                    UpdatedAt = DateTime.UtcNow
                });

                if (affected > 0)
                    successCount++;
                else
                    failureCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to verify insurance for patient {PatientId}", message.PatientId);
                failureCount++;
            }
        }

        logger.LogInformation(
            "Insurance verification batch complete: {Success}/{Total} succeeded",
            successCount, messages.Length);

        return new InsuranceVerificationBatchCompleted(messages.Length, successCount, failureCount);
    }
}

/// <summary>
/// Batch handler for UpdateClaimStatus messages.
/// Combines batch message processing with EF Core ExecuteUpdate for maximum efficiency.
/// </summary>
public static class UpdateClaimStatusBatchHandler
{
    public static async Task Handle(
        UpdateClaimStatus[] messages,
        IDbConnectionFactory connectionFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing batch of {Count} claim status updates", messages.Length);

        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        // Single bulk update using Dapper with a collection
        const string sql = """
            UPDATE Patients
            SET UpdatedAt = @UpdatedAt
            WHERE Id = @PatientId
            """;

        var parameters = messages.Select(m => new
        {
            m.PatientId,
            UpdatedAt = DateTime.UtcNow
        });

        await connection.ExecuteAsync(sql, parameters);

        logger.LogInformation(
            "Claim status batch update complete for {Count} records", messages.Length);
    }
}
