using RetailFixIT.Domain.Common;
using RetailFixIT.Domain.Enums;

namespace RetailFixIT.Domain.Entities;

public class Job : BaseEntity
{
    public string JobNumber { get; set; } = string.Empty; // Human-readable: JOB-2024-00001
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string ServiceAddress { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty; // e.g., Electrical, Plumbing, HVAC
    public JobStatus Status { get; set; } = JobStatus.New;
    public JobPriority Priority { get; set; } = JobPriority.Medium;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    // Denormalized for list queries without joins (Cosmos DB pattern)
    public string? AssignedVendorName { get; set; }

    // Navigation properties (not loaded via Include in Cosmos — queried separately)
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<AIRecommendation> AIRecommendations { get; set; } = new List<AIRecommendation>();
}
