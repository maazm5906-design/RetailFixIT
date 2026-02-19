using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailFixIT.Domain.Entities;

namespace RetailFixIT.Infrastructure.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToContainer("Assignments");
        builder.HasPartitionKey(a => a.TenantId);
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Status).HasConversion<string>();
        builder.Ignore(a => a.Job);
        builder.Ignore(a => a.Vendor);
    }
}
