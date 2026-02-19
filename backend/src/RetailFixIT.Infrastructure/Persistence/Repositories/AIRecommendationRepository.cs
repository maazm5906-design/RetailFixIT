using Microsoft.EntityFrameworkCore;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Infrastructure.Persistence.Repositories;

public class AIRecommendationRepository : IAIRecommendationRepository
{
    private readonly AppDbContext _db;

    public AIRecommendationRepository(AppDbContext db) => _db = db;

    public async Task<AIRecommendation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.AIRecommendations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IEnumerable<AIRecommendation>> GetByJobIdAsync(Guid jobId, CancellationToken ct = default)
        => await _db.AIRecommendations
            .Where(r => r.JobId == jobId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

    public async Task<AIRecommendation> AddAsync(AIRecommendation recommendation, CancellationToken ct = default)
    {
        _db.AIRecommendations.Add(recommendation);
        await _db.SaveChangesAsync(ct);
        return recommendation;
    }

    public async Task UpdateAsync(AIRecommendation recommendation, CancellationToken ct = default)
    {
        _db.AIRecommendations.Update(recommendation);
        await _db.SaveChangesAsync(ct);
    }
}
