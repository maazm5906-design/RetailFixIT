using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailFixIT.Domain.Entities;

namespace RetailFixIT.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToContainer("UserRoles");
        builder.HasPartitionKey(u => u.TenantId);
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Role).HasConversion<string>();
    }
}
