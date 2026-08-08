using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ClinicApi.Infrastructure.Security;

/// <summary>
/// Authorization policies for HIPAA-compliant access control.
/// §164.312(a)(1): Access controls — only authorized users access PHI.
///
/// In production, JWT claims from the Istio Gateway (Ring 2) provide identity.
/// In development, a permissive policy allows all authenticated requests.
/// </summary>
public static class AuthorizationConfig
{
    public const string PolicyPhiRead = "PhiRead";
    public const string PolicyPhiWrite = "PhiWrite";
    public const string PolicyBatchOperations = "BatchOperations";
    public const string PolicyAdmin = "Admin";

    public static IServiceCollection AddClinicAuthorization(this IServiceCollection services, IWebHostEnvironment env)
    {
        services.AddAuthorization(options =>
        {
            // PHI Read: any authenticated clinical user
            options.AddPolicy(PolicyPhiRead, policy =>
                policy.RequireAuthenticatedUser());

            // PHI Write: requires clinical role
            options.AddPolicy(PolicyPhiWrite, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "clinician", "admin", "nurse", "physician"));

            // Batch operations: restricted to admin/system
            options.AddPolicy(PolicyBatchOperations, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "admin", "system"));

            // Admin: full access
            options.AddPolicy(PolicyAdmin, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("role", "admin"));

            // Default: require authentication for all endpoints
            options.FallbackPolicy = env.IsDevelopment()
                ? null  // Dev: no auth required (easier debugging)
                : new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
        });

        // In development, add a fake authentication handler for testing
        if (env.IsDevelopment())
        {
            services.AddAuthentication("Dev")
                .AddScheme<DevAuthOptions, DevAuthHandler>("Dev", _ => { });
        }
        else
        {
            // Production: JWT Bearer from Istio Gateway or direct
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    // Configured via environment variables:
                    // Authentication__Authority, Authentication__Audience
                });
        }

        return services;
    }
}

/// <summary>
/// Development-only auth handler that auto-authenticates all requests.
/// Role can be overridden via X-Dev-Role header for testing permission boundaries.
/// Examples: curl -H "X-Dev-Role: nurse" localhost:5000/api/patients
/// NEVER used in staging/production (FallbackPolicy enforces real JWT).
/// </summary>
public class DevAuthOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions { }

public class DevAuthHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<DevAuthOptions>
{
    public DevAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<DevAuthOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        // Allow role override via header for testing permission boundaries
        var role = Request.Headers["X-Dev-Role"].FirstOrDefault() ?? "admin";
        var userId = Request.Headers["X-Dev-User"].FirstOrDefault() ?? "dev-user";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, $"Dev User ({role})"),
            new Claim("role", role),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}
