namespace ClinicApi.Infrastructure.Caching;

/// <summary>
/// Configuration-driven caching. Reads from appsettings "Caching" section.
/// Each endpoint can be independently enabled/disabled with its own TTL and key pattern.
///
/// Key patterns support placeholders that are resolved at runtime:
///   {orgId}  → TenantContext.OrganizationId
///   {Id}     → Primary entity ID from the query
///
/// Example appsettings.json:
/// "Caching": {
///   "Enabled": true,
///   "Endpoints": {
///     "GetPatientById": { "Enabled": true, "TtlSeconds": 300, "KeyPattern": "cache:patient:{orgId}:{Id}" },
///     "ListPatients": { "Enabled": false }
///   }
/// }
/// </summary>
public class CachingOptions
{
    public const string SectionName = "Caching";

    /// <summary>Global kill switch for all caching.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Per-endpoint caching configuration.</summary>
    public Dictionary<string, EndpointCacheOptions> Endpoints { get; set; } = new();
}

public class EndpointCacheOptions
{
    /// <summary>Whether this specific endpoint is cached.</summary>
    public bool Enabled { get; set; }

    /// <summary>Cache TTL in seconds.</summary>
    public int TtlSeconds { get; set; } = 300;

    /// <summary>
    /// Redis key pattern with placeholders.
    /// Example: "cache:patient:{orgId}:{Id}"
    /// </summary>
    public string KeyPattern { get; set; } = string.Empty;
}
