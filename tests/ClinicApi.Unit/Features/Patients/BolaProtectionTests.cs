using ClinicApi.Domain.Entities;
using ClinicApi.Domain.ValueObjects;
using ClinicApi.Infrastructure.Persistence;
using ClinicApi.Infrastructure.Security;
using ClinicApi.Unit.Shared;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Unit.Features.Patients;

/// <summary>
/// Tests verifying BOLA (Broken Object-Level Authorization) protection.
/// Ensures that organization-scoped query filters prevent cross-tenant data access.
/// OWASP API1:2023 — Broken Object-Level Authorization.
/// </summary>
public class BolaProtectionTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    private static readonly Guid OrgB = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");

    private static DbContextOptions<ClinicDbContext> InMemoryOptions() =>
        new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task Query_Should_Only_Return_Patients_From_Users_Organization()
    {
        // Arrange: seed patients in two orgs
        var options = InMemoryOptions();
        var crossTenant = TestTenantContext.CrossTenant();

        await using (var seedDb = new ClinicDbContext(options, crossTenant))
        {
            seedDb.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgA,
                FirstName = "Alice",
                LastName = "OrgA",
                Email = "alice@orga.com",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Phone = new PhoneNumber("555-0001"),
                Address = new Address("1 Main St", "Denver", "CO", "80202"),
            });
            seedDb.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgB,
                FirstName = "Bob",
                LastName = "OrgB",
                Email = "bob@orgb.com",
                DateOfBirth = new DateOnly(1985, 6, 15),
                Phone = new PhoneNumber("555-0002"),
                Address = new Address("2 Main St", "Boulder", "CO", "80301"),
            });
            await seedDb.SaveChangesAsync();
        }

        // Act: query as OrgA user
        var orgATenant = TestTenantContext.ForOrganization(OrgA);
        await using var db = new ClinicDbContext(options, orgATenant);

        var patients = await db.Patients.ToListAsync();

        // Assert: only OrgA patient is visible
        Assert.Single(patients);
        Assert.Equal("Alice", patients[0].FirstName);
        Assert.Equal(OrgA, patients[0].OrganizationId);
    }

    [Fact]
    public async Task Query_Should_Not_Return_Other_Organizations_Patients_By_Id()
    {
        var options = InMemoryOptions();
        var crossTenant = TestTenantContext.CrossTenant();
        var orgBPatientId = Guid.NewGuid();

        await using (var seedDb = new ClinicDbContext(options, crossTenant))
        {
            seedDb.Patients.Add(new Patient
            {
                Id = orgBPatientId,
                OrganizationId = OrgB,
                FirstName = "Secret",
                LastName = "Patient",
                Email = "secret@orgb.com",
                DateOfBirth = new DateOnly(1975, 3, 20),
                Phone = new PhoneNumber("555-9999"),
                Address = new Address("Hidden", "Nowhere", "XX", "00000"),
            });
            await seedDb.SaveChangesAsync();
        }

        // Act: OrgA user tries to find OrgB's patient by ID
        var orgATenant = TestTenantContext.ForOrganization(OrgA);
        await using var db = new ClinicDbContext(options, orgATenant);

        var patient = await db.Patients.FindAsync(orgBPatientId);

        // Assert: not found (filtered out by global query filter)
        Assert.Null(patient);
    }

    [Fact]
    public async Task CrossTenant_Admin_Should_See_All_Patients()
    {
        var options = InMemoryOptions();
        var crossTenant = TestTenantContext.CrossTenant();

        await using (var seedDb = new ClinicDbContext(options, crossTenant))
        {
            seedDb.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgA,
                FirstName = "Alice",
                LastName = "OrgA",
                Email = "alice2@orga.com",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Phone = new PhoneNumber("555-1111"),
                Address = new Address("A St", "Denver", "CO", "80202"),
            });
            seedDb.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrgB,
                FirstName = "Bob",
                LastName = "OrgB",
                Email = "bob2@orgb.com",
                DateOfBirth = new DateOnly(1985, 6, 15),
                Phone = new PhoneNumber("555-2222"),
                Address = new Address("B St", "Boulder", "CO", "80301"),
            });
            await seedDb.SaveChangesAsync();
        }

        // Act: cross-tenant admin queries
        await using var db = new ClinicDbContext(options, crossTenant);
        var patients = await db.Patients.ToListAsync();

        // Assert: sees all patients across orgs
        Assert.Equal(2, patients.Count);
    }

    [Fact]
    public async Task SaveChanges_Should_Reject_Modification_Of_Other_Orgs_Entity()
    {
        var options = InMemoryOptions();
        var crossTenant = TestTenantContext.CrossTenant();
        var orgBPatientId = Guid.NewGuid();

        await using (var seedDb = new ClinicDbContext(options, crossTenant))
        {
            seedDb.Patients.Add(new Patient
            {
                Id = orgBPatientId,
                OrganizationId = OrgB,
                FirstName = "Protected",
                LastName = "Patient",
                Email = "protected@orgb.com",
                DateOfBirth = new DateOnly(1980, 1, 1),
                Phone = new PhoneNumber("555-0000"),
                Address = new Address("Secure St", "Vault", "TX", "75001"),
            });
            await seedDb.SaveChangesAsync();
        }

        // Act: OrgA user somehow gets a reference and tries to modify
        // (bypassing query filter by using IgnoreQueryFilters or direct attach)
        var orgATenant = TestTenantContext.ForOrganization(OrgA);
        await using var db = new ClinicDbContext(options, orgATenant);

        // Simulate getting the entity via IgnoreQueryFilters (e.g., a bug)
        var patient = await db.Patients.IgnoreQueryFilters()
            .FirstAsync(p => p.Id == orgBPatientId);
        patient.FirstName = "HACKED";

        // Assert: SaveChanges blocks the modification
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_Should_AutoStamp_OrganizationId_On_New_Entities()
    {
        var options = InMemoryOptions();
        var orgATenant = TestTenantContext.ForOrganization(OrgA);

        await using var db = new ClinicDbContext(options, orgATenant);

        var patient = new Patient
        {
            FirstName = "New",
            LastName = "Patient",
            Email = "new@orga.com",
            DateOfBirth = new DateOnly(2000, 1, 1),
            Phone = new PhoneNumber("555-3333"),
            Address = new Address("New St", "Denver", "CO", "80202"),
            // OrganizationId intentionally NOT set
        };

        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        // Assert: auto-stamped to the user's org
        Assert.Equal(OrgA, patient.OrganizationId);
    }
}
