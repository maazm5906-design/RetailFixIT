using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Infrastructure.Persistence;

namespace RetailFixIT.API.Middleware;

/// <summary>
/// Runs after authentication. Looks up the authenticated user's email in the
/// UserRoles table and injects the app role + internal tenant into the claims.
/// Result is cached in IMemoryCache for 10 minutes so Cosmos DB is not
/// queried on every single API request.
/// </summary>
public class UserRoleEnrichmentMiddleware
{
    private static readonly Guid InternalTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly RequestDelegate _next;

    public UserRoleEnrichmentMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        AppDbContext db,
        ICurrentTenantService tenantService,
        IMemoryCache cache)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Dev JWT path: role is already embedded in the token — skip DB lookup
            var existingRole = context.User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrEmpty(existingRole))
            {
                // Azure AD path: look up role from UserRoles table (cached per email)
                var email = context.User.FindFirstValue("preferred_username")
                         ?? context.User.FindFirstValue(ClaimTypes.Email)
                         ?? string.Empty;

                if (!string.IsNullOrEmpty(email))
                {
                    var cacheKey = $"user_role:{email}";
                    if (!cache.TryGetValue(cacheKey, out (string Role, Guid TenantId) cached))
                    {
                        try
                        {
                            var userRole = await db.UserRoles
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync(u =>
                                    u.TenantId == InternalTenantId &&
                                    u.Email == email &&
                                    u.IsActive,
                                    context.RequestAborted);

                            if (userRole is not null)
                            {
                                cached = (userRole.Role.ToString(), userRole.TenantId);
                                cache.Set(cacheKey, cached, TimeSpan.FromMinutes(10));
                            }
                        }
                        catch
                        {
                            // DB lookup failed — continue without role enrichment.
                            // The request will proceed; [Authorize(Roles=...)] may reject it.
                        }
                    }

                    if (cached != default)
                    {
                        var identity = new ClaimsIdentity();
                        identity.AddClaim(new Claim(ClaimTypes.Role, cached.Role));
                        identity.AddClaim(new Claim("tenant_id", cached.TenantId.ToString()));
                        context.User.AddIdentity(identity);
                        tenantService.SetTenantId(cached.TenantId);
                    }
                }
            }
        }

        await _next(context);
    }
}
