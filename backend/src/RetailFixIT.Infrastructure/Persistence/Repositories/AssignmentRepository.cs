using Microsoft.EntityFrameworkCore;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Infrastructure.Persistence.Repositories;

public class AssignmentRepository : IAssignmentRepository
{
    private readonly AppDbContext _db;

    public AssignmentRepository(AppDbContext db) => _db = db;

    public async Task<Assignment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Assignments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<Assignment>> GetByJobIdAsync(Guid jobId, CancellationToken ct = default)
        => await _db.Assignments
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(ct);

    public async Task<Assignment> AddAsync(Assignment assignment, CancellationToken ct = default)
    {
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(ct);
        return assignment;
    }

    public async Task UpdateAsync(Assignment assignment, CancellationToken ct = default)
    {
        _db.Assignments.Update(assignment);
        await _db.SaveChangesAsync(ct);
    }
}
