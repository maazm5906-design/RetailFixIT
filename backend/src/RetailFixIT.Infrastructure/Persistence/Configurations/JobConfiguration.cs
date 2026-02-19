using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailFixIT.Domain.Entities;

namespace RetailFixIT.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToContainer("Jobs");
        builder.HasPartitionKey(j => j.TenantId);
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Status).HasConversion<string>();
        builder.Property(j => j.Priority).HasConversion<string>();

        // Cosmos ETag-based optimistic concurrency (replaces SQL RowVersion)
        builder.UseETagConcurrency();

        // Navigation properties are queried separately in Cosmos
        builder.Ignore(j => j.Assignments);
        builder.Ignore(j => j.AIRecommendations);
    }
}
