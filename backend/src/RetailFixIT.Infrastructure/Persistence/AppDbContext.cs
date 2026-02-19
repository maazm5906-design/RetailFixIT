using Microsoft.EntityFrameworkCore;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Common;
using RetailFixIT.Domain.Entities;

namespace RetailFixIT.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentTenantService _tenantService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService tenantService)
        : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AIRecommendation> AIRecommendations => Set<AIRecommendation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global tenant query filters (multi-tenancy enforced at ORM level)
        modelBuilder.Entity<UserRole>().HasQueryFilter(e => e.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<Job>().HasQueryFilter(e => e.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<Vendor>().HasQueryFilter(e => e.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<Assignment>().HasQueryFilter(e => e.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<AIRecommendation>().HasQueryFilter(e => e.TenantId == _tenantService.TenantId);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(e => e.TenantId == _tenantService.TenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is BaseEntity baseEntity && entry.State == EntityState.Modified)
                baseEntity.UpdatedAt = now;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
