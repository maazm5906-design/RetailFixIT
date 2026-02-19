using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailFixIT.Domain.Entities;

namespace RetailFixIT.Infrastructure.Persistence.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToContainer("Vendors");
        builder.HasPartitionKey(v => v.TenantId);
        builder.HasKey(v => v.Id);
    }
}
