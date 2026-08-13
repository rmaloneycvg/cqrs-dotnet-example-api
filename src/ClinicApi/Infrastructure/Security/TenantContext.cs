using System.Security.Claims;

namespace ClinicApi.Infrastructure.Security;

/// <summary>
/// BOLA (Broken Object-Level Authorization) protection via organization tenancy.
///
/// Every authenticated user belongs to an organization (clinic). Every domain entity
/// is scoped to an organization. This service extracts the user's org from their JWT
/// claims and makes it available to EF Core global query filters and Dapper queries.
///
/// Without this, any authenticated clinician could access any patient in the system
/// by enumerating IDs — the #1 API vulnerability (OWASP API1:2023).
///
/// Architecture:
///   JWT claim "org_id" → TenantContext → EF Core HasQueryFilter → SQL WHERE
///   JWT claim "org_id" → TenantContext → Dapper @OrganizationId parameter
///
/// Admin/system users can set org_id to "*" for cross-tenant operations (e.g., migrations).
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The authenticated user's organization ID. Null only in development without auth.
    /// </summary>
    Guid? OrganizationId { get; }

    /// <summary>
    /// The authenticated user's ID (sub claim).
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Whether the current user has cross-tenant access (admin/system role with org_id = "*").
    /// </summary>
    bool IsCrossTenant { get; }
}

/// <summary>
/// Resolves tenant context from the current HTTP request's JWT claims.
///
/// Expected claims:
///   - "org_id" (GUID): The organization the user belongs to
///   - "sub" or ClaimTypes.NameIdentifier: The user's unique ID
///   - "role": Used to determine if cross-tenant access is permitted
///
/// In development (no auth), returns a default dev organization ID for testing.
/// </summary>
public class HttpTenantContext : ITenantContext
{
    /// <summary>
    /// Default organization ID used in development when no auth is configured.
    /// Matches the dev seed data.
    /// </summary>
    public static readonly Guid DevOrganizationId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
    }

    public Guid? OrganizationId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                // In development, default to the dev org for unauthenticated requests
                return _environment.IsDevelopment() ? DevOrganizationId : null;
            }

            // Check for cross-tenant wildcard
            var orgClaim = user.FindFirst("org_id")?.Value;
            if (orgClaim == "*") return null; // Cross-tenant — no filter applied

            if (Guid.TryParse(orgClaim, out var orgId))
                return orgId;

            // Development fallback: use dev org when claim is missing
            return _environment.IsDevelopment() ? DevOrganizationId : null;
        }
    }

    public string? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("sub")?.Value;
        }
    }

    public bool IsCrossTenant
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var orgClaim = user?.FindFirst("org_id")?.Value;
            if (orgClaim != "*") return false;

            // Only admin/system roles are allowed cross-tenant access
            var role = user?.FindFirst("role")?.Value;
            return role is "admin" or "system";
        }
    }
}

/// <summary>
/// Cross-tenant context used during migrations and CLI operations.
/// No query filters applied — migrations need full access to all data.
/// </summary>
public class MigrationTenantContext : ITenantContext
{
    public Guid? OrganizationId => null;
    public string? UserId => "migration";
    public bool IsCrossTenant => true;
}
