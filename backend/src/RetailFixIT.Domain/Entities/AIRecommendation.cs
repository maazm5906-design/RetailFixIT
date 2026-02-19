using RetailFixIT.Domain.Common;
using RetailFixIT.Domain.Enums;

namespace RetailFixIT.Domain.Entities;

public class AIRecommendation : BaseEntity
{
    public Guid JobId { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public AIRecommendationStatus Status { get; set; } = AIRecommendationStatus.Pending;
    public string? PromptSummary { get; set; } // Sanitized prompt sent to AI
    public string? RecommendedVendorIds { get; set; } // JSON array of VendorIds
    public string? Reasoning { get; set; } // AI-generated explanation
    public string? JobSummary { get; set; } // AI-generated job summary
    public string? AIProvider { get; set; } // Gemini | AzureOpenAI
    public string? ModelVersion { get; set; }
    public int? LatencyMs { get; set; }
    public string? ErrorMessage { get; set; } // Populated when Status = Failed
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Job Job { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
