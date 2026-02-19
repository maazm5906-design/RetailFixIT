using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailFixIT.Domain.Entities;

namespace RetailFixIT.Infrastructure.Persistence.Configurations;

public class AIRecommendationConfiguration : IEntityTypeConfiguration<AIRecommendation>
{
    public void Configure(EntityTypeBuilder<AIRecommendation> builder)
    {
        builder.ToContainer("AIRecommendations");
        builder.HasPartitionKey(r => r.TenantId);
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Status).HasConversion<string>();
    }
}
