using MediatR;
using RetailFixIT.Application.Common.Models;
using RetailFixIT.Application.Jobs.DTOs;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Application.Jobs.Queries;

public record GetJobsQuery(
    int Page = 1, int PageSize = 25,
    string? Status = null, string? Priority = null,
    string? Search = null, string? ServiceType = null,
    string? SortBy = null, bool SortDesc = false) : IRequest<PagedResult<JobSummaryDto>>;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, PagedResult<JobSummaryDto>>
{
    private readonly IJobRepository _jobs;

    public GetJobsQueryHandler(IJobRepository jobs) => _jobs = jobs;

    public async Task<PagedResult<JobSummaryDto>> Handle(GetJobsQuery request, CancellationToken ct)
    {
        var statuses = ParseEnums<JobStatus>(request.Status);
        var priorities = ParseEnums<JobPriority>(request.Priority);

        var (items, total) = await _jobs.GetPagedAsync(
            request.Page, request.PageSize,
            statuses, priorities,
            request.Search, request.ServiceType,
            request.SortBy, request.SortDesc, ct);

        return new PagedResult<JobSummaryDto>
        {
            Items = items.Select(j => new JobSummaryDto
            {
                Id = j.Id,
                JobNumber = j.JobNumber,
                Title = j.Title,
                CustomerName = j.CustomerName,
                ServiceType = j.ServiceType,
                Status = j.Status,
                Priority = j.Priority,
                ScheduledAt = j.ScheduledAt,
                CreatedAt = j.CreatedAt,
                AssignedVendorName = j.AssignedVendorName
            }),
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private static T[]? ParseEnums<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value)) return null;
        return value.Split(',')
            .Select(s => Enum.TryParse<T>(s.Trim(), true, out var e) ? (T?)e : null)
            .Where(e => e.HasValue).Select(e => e!.Value).ToArray();
    }
}
