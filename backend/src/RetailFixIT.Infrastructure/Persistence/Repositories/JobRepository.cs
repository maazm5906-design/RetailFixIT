using Microsoft.EntityFrameworkCore;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Infrastructure.Persistence.Repositories;

public class JobRepository : IJobRepository
{
    private readonly AppDbContext _db;

    public JobRepository(AppDbContext db) => _db = db;

    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Jobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<(IEnumerable<Job> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        JobStatus[]? statuses = null,
        JobPriority[]? priorities = null,
        string? search = null,
        string? serviceType = null,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken ct = default)
    {
        var query = _db.Jobs.AsQueryable();

        if (statuses?.Length > 0) query = query.Where(j => statuses.Contains(j.Status));
        if (priorities?.Length > 0) query = query.Where(j => priorities.Contains(j.Priority));
        if (!string.IsNullOrEmpty(serviceType)) query = query.Where(j => j.ServiceType == serviceType);
        if (!string.IsNullOrEmpty(search))
            query = query.Where(j => j.Title.Contains(search) || j.CustomerName.Contains(search) || j.JobNumber.Contains(search));

        query = (sortBy?.ToLower(), sortDesc) switch
        {
            ("priority", false) => query.OrderBy(j => j.Priority),
            ("priority", true) => query.OrderByDescending(j => j.Priority),
            ("status", false) => query.OrderBy(j => j.Status),
            ("status", true) => query.OrderByDescending(j => j.Status),
            (_, true) => query.OrderByDescending(j => j.CreatedAt),
            _ => query.OrderByDescending(j => j.CreatedAt),
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<Job> AddAsync(Job job, CancellationToken ct = default)
    {
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task UpdateAsync(Job job, CancellationToken ct = default)
    {
        _db.Jobs.Update(job);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<string> GenerateJobNumberAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Jobs.IgnoreQueryFilters().CountAsync(ct) + 1;
        return $"JOB-{year}-{count:D5}";
    }
}
