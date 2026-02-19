namespace RetailFixIT.Domain.Events;

public record JobCreatedEvent(
    Guid JobId,
    Guid TenantId,
    string JobNumber,
    string Title,
    string ServiceType,
    string CreatedByUserId,
    DateTime CreatedAt);
