namespace RetailFixIT.Domain.Events;

public record AIRecommendationGeneratedEvent(
    Guid RecommendationId,
    Guid JobId,
    Guid TenantId,
    bool Success,
    string? Reasoning,
    string? JobSummary,
    List<Guid>? RecommendedVendorIds,
    DateTime CompletedAt);
