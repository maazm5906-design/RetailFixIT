using RetailFixIT.Domain.Entities;

namespace RetailFixIT.Domain.Interfaces;

public interface IAIRecommendationRepository
{
    Task<AIRecommendation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<AIRecommendation>> GetByJobIdAsync(Guid jobId, CancellationToken ct = default);
    Task<AIRecommendation> AddAsync(AIRecommendation recommendation, CancellationToken ct = default);
    Task UpdateAsync(AIRecommendation recommendation, CancellationToken ct = default);
}
