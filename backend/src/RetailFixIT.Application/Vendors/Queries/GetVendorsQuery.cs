using MediatR;
using RetailFixIT.Application.Common.Models;
using RetailFixIT.Application.Vendors.DTOs;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Application.Vendors.Queries;

public record GetVendorsQuery(
    int Page = 1, int PageSize = 50,
    bool? IsActive = null, bool? HasCapacity = null,
    string? ServiceType = null) : IRequest<PagedResult<VendorDto>>;

public class GetVendorsQueryHandler : IRequestHandler<GetVendorsQuery, PagedResult<VendorDto>>
{
    private readonly IVendorRepository _vendors;

    public GetVendorsQueryHandler(IVendorRepository vendors) => _vendors = vendors;

    public async Task<PagedResult<VendorDto>> Handle(GetVendorsQuery request, CancellationToken ct)
    {
        var (items, total) = await _vendors.GetPagedAsync(
            request.Page, request.PageSize,
            request.IsActive, request.HasCapacity,
            request.ServiceType, ct);

        return new PagedResult<VendorDto>
        {
            Items = items.Select(v => new VendorDto
            {
                Id = v.Id, Name = v.Name, ContactEmail = v.ContactEmail,
                ContactPhone = v.ContactPhone, ServiceArea = v.ServiceArea,
                Specializations = v.Specializations, CapacityLimit = v.CapacityLimit,
                CurrentCapacity = v.CurrentCapacity, Rating = v.Rating,
                IsActive = v.IsActive, CreatedAt = v.CreatedAt
            }),
            TotalCount = total, Page = request.Page, PageSize = request.PageSize
        };
    }
}

public record GetVendorByIdQuery(Guid VendorId) : IRequest<VendorDto>;

public class GetVendorByIdQueryHandler : IRequestHandler<GetVendorByIdQuery, VendorDto>
{
    private readonly IVendorRepository _vendors;

    public GetVendorByIdQueryHandler(IVendorRepository vendors) => _vendors = vendors;

    public async Task<VendorDto> Handle(GetVendorByIdQuery request, CancellationToken ct)
    {
        var v = await _vendors.GetByIdAsync(request.VendorId, ct)
            ?? throw new KeyNotFoundException($"Vendor {request.VendorId} not found");

        return new VendorDto
        {
            Id = v.Id, Name = v.Name, ContactEmail = v.ContactEmail,
            ContactPhone = v.ContactPhone, ServiceArea = v.ServiceArea,
            Specializations = v.Specializations, CapacityLimit = v.CapacityLimit,
            CurrentCapacity = v.CurrentCapacity, Rating = v.Rating,
            IsActive = v.IsActive, CreatedAt = v.CreatedAt
        };
    }
}
