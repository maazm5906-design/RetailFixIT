namespace RetailFixIT.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Cosmos DB uses string/Guid PKs
    public Guid TenantId { get; set; } // No FK - immutable audit trail
    public string EntityName { get; set; } = string.Empty; // "Job" | "Assignment" | etc.
    public string EntityId { get; set; } = string.Empty; // String to accommodate any PK type
    public string Action { get; set; } = string.Empty; // Created | Updated | Assigned | etc.
    public string ChangedByUserId { get; set; } = string.Empty;
    public string ChangedByEmail { get; set; } = string.Empty; // Denormalized for immutability
    public string? OldValues { get; set; } // JSON snapshot before change
    public string? NewValues { get; set; } // JSON snapshot after change
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
