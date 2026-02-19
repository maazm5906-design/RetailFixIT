using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Enums;

namespace RetailFixIT.Infrastructure.Persistence;

public static class SeedData
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Stable IDs so every restart is idempotent via point reads (no cross-partition queries)
    private static readonly Guid Vendor1Id = Guid.Parse("aaaaaaaa-0001-0000-0000-000000000000");
    private static readonly Guid Job1Id = Guid.Parse("bbbbbbbb-0001-0000-0000-000000000000");
    private static readonly Guid UserRole1Id = Guid.Parse("cccccccc-0001-0000-0000-000000000000");

    public static async Task SeedAsync(AppDbContext db)
    {
        // FindAsync = point read: bypasses Cosmos query plan compiler entirely (no "root" issue).
        // TenantId must be set on ICurrentTenantService before calling this method so that
        // the global query filter routes single-partition reads to the correct partition.

        // Seed tenant (partition key == Id → pure point read)
        if (await db.Tenants.FindAsync(TenantId) is null)
        {
            db.Tenants.Add(new Tenant
            {
                Id = TenantId,
                Name = "Acme Services",
                Slug = "acme",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // Seed vendors (partition key = TenantId, checked via first vendor's stable Id)
        if (await db.Vendors.FindAsync(Vendor1Id) is null)
        {
            var vendors = new[]
            {
                new Vendor { Id = Vendor1Id, TenantId = TenantId, Name = "FastFix Electric", ContactEmail = "ops@fastfix.com", ServiceArea = "Downtown, Midtown", Specializations = "[\"Electrical\",\"Wiring\",\"Panel Upgrades\"]", CapacityLimit = 5, Rating = 4.8m },
                new Vendor { Id = Guid.Parse("aaaaaaaa-0002-0000-0000-000000000000"), TenantId = TenantId, Name = "PlumbRight Co", ContactEmail = "jobs@plumbright.com", ServiceArea = "Citywide", Specializations = "[\"Plumbing\",\"Drainage\",\"Water Heaters\"]", CapacityLimit = 8, Rating = 4.5m },
                new Vendor { Id = Guid.Parse("aaaaaaaa-0003-0000-0000-000000000000"), TenantId = TenantId, Name = "CoolAir HVAC", ContactEmail = "service@coolair.com", ServiceArea = "North District", Specializations = "[\"HVAC\",\"AC Repair\",\"Duct Cleaning\"]", CapacityLimit = 4, Rating = 4.7m },
                new Vendor { Id = Guid.Parse("aaaaaaaa-0004-0000-0000-000000000000"), TenantId = TenantId, Name = "AllFix Solutions", ContactEmail = "dispatch@allfix.com", ServiceArea = "Citywide", Specializations = "[\"Electrical\",\"Plumbing\",\"General Repair\"]", CapacityLimit = 12, Rating = 4.2m },
                new Vendor { Id = Guid.Parse("aaaaaaaa-0005-0000-0000-000000000000"), TenantId = TenantId, Name = "Sunrise Electrical", ContactEmail = "hello@sunrise-elec.com", ServiceArea = "East Side", Specializations = "[\"Electrical\",\"Smart Home\",\"Solar\"]", CapacityLimit = 6, Rating = 4.9m },
            };
            db.Vendors.AddRange(vendors);
            await db.SaveChangesAsync();
        }

        // Seed user roles — maps Azure AD user emails to app roles and internal tenant
        if (await db.UserRoles.FindAsync(UserRole1Id) is null)
        {
            var userRoles = new[]
            {
                new UserRole { Id = UserRole1Id,                                               TenantId = TenantId, Email = "dispatcher@maazm5906gmail.onmicrosoft.com", DisplayName = "Dispatcher",     Role = UserRoleType.Dispatcher,    IsActive = true },
                new UserRole { Id = Guid.Parse("cccccccc-0002-0000-0000-000000000000"), TenantId = TenantId, Email = "admin@maazm5906gmail.onmicrosoft.com",      DisplayName = "Admin",          Role = UserRoleType.Admin,         IsActive = true },
                new UserRole { Id = Guid.Parse("cccccccc-0003-0000-0000-000000000000"), TenantId = TenantId, Email = "support@maazm5906gmail.onmicrosoft.com",    DisplayName = "Support Agent",  Role = UserRoleType.SupportAgent,  IsActive = true },
                new UserRole { Id = Guid.Parse("cccccccc-0004-0000-0000-000000000000"), TenantId = TenantId, Email = "vendormgr@maazm5906gmail.onmicrosoft.com",  DisplayName = "Vendor Manager", Role = UserRoleType.VendorManager, IsActive = true },
            };
            db.UserRoles.AddRange(userRoles);
            await db.SaveChangesAsync();
        }

        // Seed sample jobs
        if (await db.Jobs.FindAsync(Job1Id) is null)
        {
            var jobs = new[]
            {
                new Job { Id = Job1Id, TenantId = TenantId, JobNumber = "JOB-2024-00001", Title = "Electrical panel upgrade", Description = "Customer reports flickering lights and needs a 200A panel upgrade.", CustomerName = "John Smith", CustomerEmail = "john@example.com", ServiceAddress = "123 Main St, Downtown", ServiceType = "Electrical", Status = JobStatus.New, Priority = JobPriority.High, CreatedByUserId = "dispatcher@acme.com" },
                new Job { Id = Guid.Parse("bbbbbbbb-0002-0000-0000-000000000000"), TenantId = TenantId, JobNumber = "JOB-2024-00002", Title = "Burst pipe emergency", Description = "Water pipe burst in basement. Flooding risk.", CustomerName = "Sarah Johnson", CustomerPhone = "555-0102", ServiceAddress = "456 Oak Ave, Midtown", ServiceType = "Plumbing", Status = JobStatus.InReview, Priority = JobPriority.Critical, CreatedByUserId = "dispatcher@acme.com" },
                new Job { Id = Guid.Parse("bbbbbbbb-0003-0000-0000-000000000000"), TenantId = TenantId, JobNumber = "JOB-2024-00003", Title = "AC not cooling", Description = "HVAC system blowing warm air. Unit is 5 years old.", CustomerName = "Mike Davis", CustomerEmail = "mike.davis@example.com", ServiceAddress = "789 Pine Rd, North District", ServiceType = "HVAC", Status = JobStatus.Assigned, Priority = JobPriority.Medium, ScheduledAt = DateTime.UtcNow.AddDays(1), CreatedByUserId = "dispatcher@acme.com" },
                new Job { Id = Guid.Parse("bbbbbbbb-0004-0000-0000-000000000000"), TenantId = TenantId, JobNumber = "JOB-2024-00004", Title = "Outlet installation", Description = "Need 4 new outlets in home office for equipment.", CustomerName = "Lisa Chen", CustomerEmail = "lisa.chen@example.com", ServiceAddress = "321 Elm St, East Side", ServiceType = "Electrical", Status = JobStatus.Completed, Priority = JobPriority.Low, CompletedAt = DateTime.UtcNow.AddDays(-1), CreatedByUserId = "dispatcher@acme.com" },
                new Job { Id = Guid.Parse("bbbbbbbb-0005-0000-0000-000000000000"), TenantId = TenantId, JobNumber = "JOB-2024-00005", Title = "Water heater replacement", Description = "Old water heater leaking. Needs full replacement.", CustomerName = "Robert Williams", ServiceAddress = "654 Maple Dr, South End", ServiceType = "Plumbing", Status = JobStatus.New, Priority = JobPriority.High, CreatedByUserId = "dispatcher@acme.com" },
            };
            db.Jobs.AddRange(jobs);
            await db.SaveChangesAsync();
        }
    }
}
