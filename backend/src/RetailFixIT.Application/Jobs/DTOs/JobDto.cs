using RetailFixIT.Domain.Enums;

namespace RetailFixIT.Application.Jobs.DTOs;

public class JobDto
{
    public Guid Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string ServiceAddress { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public AssignmentSummaryDto? ActiveAssignment { get; set; }
    public bool HasAIRecommendation { get; set; }
}

public class AssignmentSummaryDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

public class JobSummaryDto
{
    public Guid Id { get; set; }
    public string JobNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public JobPriority Priority { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? AssignedVendorName { get; set; }
}

public class CreateJobRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string ServiceAddress { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public JobPriority Priority { get; set; } = JobPriority.Medium;
    public DateTime? ScheduledAt { get; set; }
}

public class UpdateJobRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string ServiceAddress { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public JobPriority Priority { get; set; } = JobPriority.Medium;
    public DateTime? ScheduledAt { get; set; }
}
