namespace RetailFixIT.Infrastructure.AI;

public record AIJobContext(
    Guid JobId,
    string Title,
    string Description,
    string ServiceType,
    string ServiceAddress,
    List<VendorContext> AvailableVendors);

public record VendorContext(
    Guid VendorId,
    string Name,
    string? ServiceArea,
    string? Specializations,
    decimal? Rating,
    int AvailableCapacity);

public record AIRecommendationResult(
    bool Success,
    List<Guid>? RecommendedVendorIds,
    string? Reasoning,
    string? JobSummary,
    string ProviderName,
    string ModelVersion,
    int LatencyMs,
    string? ErrorMessage = null);

public interface IAIProvider
{
    Task<AIRecommendationResult> GenerateRecommendationAsync(AIJobContext context, CancellationToken ct = default);
}
