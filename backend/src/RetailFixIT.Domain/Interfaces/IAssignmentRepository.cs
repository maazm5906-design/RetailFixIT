using RetailFixIT.Domain.Entities;

namespace RetailFixIT.Domain.Interfaces;

public interface IAssignmentRepository
{
    Task<Assignment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Assignment>> GetByJobIdAsync(Guid jobId, CancellationToken ct = default);
    Task<Assignment> AddAsync(Assignment assignment, CancellationToken ct = default);
    Task UpdateAsync(Assignment assignment, CancellationToken ct = default);
}
