using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailFixIT.Application.AI.Commands;
using RetailFixIT.Application.Assignments.Commands;
using RetailFixIT.Application.Assignments.DTOs;
using RetailFixIT.Application.Jobs.Commands;
using RetailFixIT.Application.Jobs.DTOs;
using RetailFixIT.Application.Jobs.Queries;
using RetailFixIT.Application.Vendors.DTOs;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.API.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAIRecommendationRepository _aiRepo;
    private readonly IAssignmentRepository _assignmentRepo;

    public JobsController(IMediator mediator, IAIRecommendationRepository aiRepo, IAssignmentRepository assignmentRepo)
    {
        _mediator = mediator;
        _aiRepo = aiRepo;
        _assignmentRepo = assignmentRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? search = null,
        [FromQuery] string? serviceType = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetJobsQuery(
            page, pageSize, status, priority, search, serviceType, sortBy, sortDesc), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetJobByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateJobCommand(request), ct);
        return CreatedAtAction(nameof(GetJob), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateJobCommand(id, request), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<JobStatus>(request.Status, true, out var status))
            return BadRequest(new { error = $"Invalid status: {request.Status}" });

        var result = await _mediator.Send(new UpdateJobStatusCommand(id, status), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/cancel")]
    public async Task<IActionResult> CancelJob(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateJobStatusCommand(id, JobStatus.Cancelled), ct);
        return Ok(result);
    }

    // --- Assignments ---

    [HttpGet("{jobId:guid}/assignments")]
    public async Task<IActionResult> GetAssignments(Guid jobId, CancellationToken ct)
    {
        var items = await _assignmentRepo.GetByJobIdAsync(jobId, ct);
        return Ok(items.Select(a => new
        {
            id = a.Id,
            jobId = a.JobId,
            vendorId = a.VendorId,
            vendorName = a.Vendor?.Name,
            status = a.Status.ToString(),
            notes = a.Notes,
            assignedAt = a.AssignedAt,
            revokedAt = a.RevokedAt,
            completedAt = a.CompletedAt
        }));
    }

    [HttpPost("{jobId:guid}/assignments")]
    public async Task<IActionResult> AssignVendor(Guid jobId, [FromBody] AssignVendorRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignVendorCommand(jobId, request), ct);
        return Created($"/api/v1/jobs/{jobId}/assignments/{result.Id}", result);
    }

    [HttpDelete("{jobId:guid}/assignments/{assignmentId:guid}")]
    public async Task<IActionResult> RevokeAssignment(Guid jobId, Guid assignmentId, CancellationToken ct)
    {
        await _mediator.Send(new RevokeAssignmentCommand(jobId, assignmentId), ct);
        return NoContent();
    }

    // --- AI Recommendations ---

    [HttpPost("{jobId:guid}/recommendations")]
    public async Task<IActionResult> RequestRecommendation(Guid jobId, CancellationToken ct)
    {
        var result = await _mediator.Send(new RequestAIRecommendationCommand(jobId), ct);
        return Accepted(result);
    }

    [HttpGet("{jobId:guid}/recommendations")]
    public async Task<IActionResult> GetRecommendations(Guid jobId, CancellationToken ct)
    {
        var items = await _aiRepo.GetByJobIdAsync(jobId, ct);
        return Ok(items.Select(r => new
        {
            id = r.Id,
            jobId = r.JobId,
            status = r.Status.ToString(),
            reasoning = r.Reasoning,
            jobSummary = r.JobSummary,
            errorMessage = r.ErrorMessage,
            recommendedVendorIds = string.IsNullOrEmpty(r.RecommendedVendorIds)
                ? new List<Guid>()
                : System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(r.RecommendedVendorIds),
            aiProvider = r.AIProvider,
            modelVersion = r.ModelVersion,
            latencyMs = r.LatencyMs,
            requestedAt = r.RequestedAt,
            completedAt = r.CompletedAt
        }));
    }
}

public record UpdateStatusRequest(string Status);
