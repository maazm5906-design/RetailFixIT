using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailFixIT.Application.Vendors.DTOs;
using RetailFixIT.Application.Vendors.Queries;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.API.Controllers;

[ApiController]
[Route("api/v1/vendors")]
[Authorize]
public class VendorsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IVendorRepository _vendors;
    private readonly ICurrentTenantService _tenant;

    public VendorsController(IMediator mediator, IVendorRepository vendors, ICurrentTenantService tenant)
    {
        _mediator = mediator;
        _vendors = vendors;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetVendors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? hasCapacity = null,
        [FromQuery] string? serviceType = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetVendorsQuery(page, pageSize, isActive, hasCapacity, serviceType), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetVendor(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVendorByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request, CancellationToken ct)
    {
        var vendor = new Vendor
        {
            TenantId = _tenant.TenantId,
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            ServiceArea = request.ServiceArea,
            Specializations = request.Specializations,
            CapacityLimit = request.CapacityLimit
        };
        var created = await _vendors.AddAsync(vendor, ct);
        return CreatedAtAction(nameof(GetVendor), new { id = created.Id }, new
        {
            created.Id,
            created.Name,
            created.ContactEmail,
            created.IsActive
        });
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException($"Vendor {id} not found");
        vendor.IsActive = true;
        await _vendors.UpdateAsync(vendor, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var vendor = await _vendors.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException($"Vendor {id} not found");
        vendor.IsActive = false;
        await _vendors.UpdateAsync(vendor, ct);
        return NoContent();
    }
}
