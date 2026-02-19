namespace RetailFixIT.Domain.Events;

public record JobAssignedEvent(
    Guid JobId,
    Guid TenantId,
    Guid AssignmentId,
    Guid VendorId,
    string VendorName,
    string AssignedByUserId,
    DateTime AssignedAt);
