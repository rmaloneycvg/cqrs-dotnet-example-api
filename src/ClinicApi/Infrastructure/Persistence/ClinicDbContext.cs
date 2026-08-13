using ClinicApi.Domain.Entities;
using ClinicApi.Domain.ValueObjects;
using ClinicApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ClinicApi.Infrastructure.Persistence;

public class ClinicDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public ClinicDbContext(DbContextOptions<ClinicDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─── BOLA Protection: Global Query Filters ───────────────────────────
        // Every query automatically scoped to the authenticated user's org.
        // Cross-tenant users (admin with org_id="*") get OrganizationId=null → no filter.
        // This prevents OWASP API1:2023 (Broken Object-Level Authorization).

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasQueryFilter(e => _tenantContext.OrganizationId == null
                || e.OrganizationId == _tenantContext.OrganizationId);

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OrganizationId, e.Email }).IsUnique();
            entity.HasIndex(e => e.ExternalId).IsUnique().HasFilter("[ExternalId] IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.HasIndex(e => e.LastAppointmentAt);
            entity.HasIndex(e => e.OrganizationId);

            entity.OwnsOne(e => e.Phone, phone =>
            {
                phone.Property(p => p.Number).HasColumnName("PhoneNumber").HasMaxLength(20);
                phone.Property(p => p.Type).HasColumnName("PhoneType");
            });

            entity.OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.Street).HasColumnName("AddressStreet").HasMaxLength(200);
                address.Property(a => a.City).HasColumnName("AddressCity").HasMaxLength(100);
                address.Property(a => a.State).HasColumnName("AddressState").HasMaxLength(50);
                address.Property(a => a.ZipCode).HasColumnName("AddressZipCode").HasMaxLength(10);
                address.Property(a => a.Country).HasColumnName("AddressCountry").HasMaxLength(50);
            });

            entity.OwnsOne(e => e.Insurance, ins =>
            {
                ins.Property(i => i.ProviderId).HasColumnName("InsuranceProviderId").HasMaxLength(50);
                ins.Property(i => i.ProviderName).HasColumnName("InsuranceProviderName").HasMaxLength(200);
                ins.Property(i => i.PolicyNumber).HasColumnName("InsurancePolicyNumber").HasMaxLength(50);
                ins.Property(i => i.GroupNumber).HasColumnName("InsuranceGroupNumber").HasMaxLength(50);
                ins.Property(i => i.EffectiveDate).HasColumnName("InsuranceEffectiveDate");
                ins.Property(i => i.ExpirationDate).HasColumnName("InsuranceExpirationDate");
            });

            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ExternalId).HasMaxLength(100);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasQueryFilter(e => _tenantContext.OrganizationId == null
                || e.OrganizationId == _tenantContext.OrganizationId);

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => new { e.ProviderName, e.ScheduledAt });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OrganizationId);

            entity.Property(e => e.ProviderName).HasMaxLength(200);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(2000);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasQueryFilter(e => _tenantContext.OrganizationId == null
                || e.OrganizationId == _tenantContext.OrganizationId);

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.OrganizationId);

            entity.Property(e => e.InvoiceNumber).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.OwnsOne(e => e.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            entity.OwnsOne(e => e.PaidAmount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("PaidAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("PaidCurrency").HasMaxLength(3);
            });
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasQueryFilter(e => _tenantContext.OrganizationId == null
                || e.OrganizationId == _tenantContext.OrganizationId);

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.InvoiceId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.OrganizationId);

            entity.Property(e => e.TransactionId).HasMaxLength(100);
            entity.Property(e => e.FailureReason).HasMaxLength(500);

            entity.OwnsOne(e => e.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasQueryFilter(e => _tenantContext.OrganizationId == null
                || e.OrganizationId == _tenantContext.OrganizationId);

            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpirationDate);
            entity.HasIndex(e => e.OrganizationId);

            entity.Property(e => e.MedicationName).HasMaxLength(200);
            entity.Property(e => e.Dosage).HasMaxLength(100);
            entity.Property(e => e.Frequency).HasMaxLength(100);
            entity.Property(e => e.PrescriberName).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(2000);
        });
    }

    /// <summary>
    /// Intercept SaveChanges to automatically stamp OrganizationId on new entities.
    /// Prevents developers from forgetting to set the org — defense in depth.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampOrganizationId();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampOrganizationId();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampOrganizationId()
    {
        var orgId = _tenantContext.OrganizationId;
        if (orgId is null) return; // Cross-tenant or unauthenticated — don't auto-stamp

        foreach (var entry in ChangeTracker.Entries<IOrganizationScoped>())
        {
            if (entry.State == EntityState.Added && entry.Entity.OrganizationId == Guid.Empty)
            {
                entry.Entity.OrganizationId = orgId.Value;
            }

            // Prevent modifying entities that belong to a different org
            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                if (entry.Entity.OrganizationId != orgId.Value)
                {
                    throw new UnauthorizedAccessException(
                        $"Cannot modify entity belonging to organization {entry.Entity.OrganizationId}. " +
                        $"Current user belongs to {orgId.Value}.");
                }
            }
        }
    }
}
