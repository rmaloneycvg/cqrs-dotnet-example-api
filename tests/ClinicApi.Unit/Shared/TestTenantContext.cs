using ClinicApi.Infrastructure.Security;

namespace ClinicApi.Unit.Shared;

/// <summary>
/// Test ITenantContext that simulates a cross-tenant admin (no filter applied).
/// For tests that need org isolation, set OrganizationId to a specific Guid.
/// </summary>
public class TestTenantContext : ITenantContext
{
    public Guid? OrganizationId { get; set; }
    public string? UserId { get; set; } = "test-user";
    public bool IsCrossTenant { get; set; } = true;

    /// <summary>Cross-tenant (no filters) — default for unit tests.</summary>
    public static TestTenantContext CrossTenant() => new() { OrganizationId = null, IsCrossTenant = true };

    /// <summary>Scoped to a specific org — use for BOLA isolation tests.</summary>
    public static TestTenantContext ForOrganization(Guid orgId) => new()
    {
        OrganizationId = orgId,
        IsCrossTenant = false
    };
}
