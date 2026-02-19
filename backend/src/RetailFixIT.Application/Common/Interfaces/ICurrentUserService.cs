using RetailFixIT.Domain.Enums;

namespace RetailFixIT.Application.Common.Interfaces;

/// <summary>
/// Provides information about the currently authenticated user.
/// Implemented in Infrastructure layer via HTTP context claims.
/// </summary>
public interface ICurrentUserService
{
    string UserId { get; }
    string Email { get; }
    string DisplayName { get; }
    UserRoleType Role { get; }
    Guid TenantId { get; }
    bool IsAuthenticated { get; }
}
