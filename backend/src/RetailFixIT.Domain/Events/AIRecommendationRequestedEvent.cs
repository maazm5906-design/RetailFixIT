namespace RetailFixIT.Domain.Events;

public record AIRecommendationRequestedEvent(
    Guid RecommendationId,
    Guid JobId,
    Guid TenantId,
    string JobTitle,
    string JobDescription,
    string ServiceType,
    string ServiceAddress,
    string RequestedByUserId,
    DateTime RequestedAt);
