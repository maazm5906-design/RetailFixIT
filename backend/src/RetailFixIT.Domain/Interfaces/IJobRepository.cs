using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Enums;

namespace RetailFixIT.Domain.Interfaces;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Job> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        JobStatus[]? statuses = null,
        JobPriority[]? priorities = null,
        string? search = null,
        string? serviceType = null,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken ct = default);
    Task<Job> AddAsync(Job job, CancellationToken ct = default);
    Task UpdateAsync(Job job, CancellationToken ct = default);
    Task<string> GenerateJobNumberAsync(CancellationToken ct = default);
}
