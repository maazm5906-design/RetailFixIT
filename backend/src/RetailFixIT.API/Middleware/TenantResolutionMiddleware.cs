using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using RetailFixIT.Application.Common.Interfaces;

namespace RetailFixIT.API.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService, IConfiguration config)
    {
        // Dev JWT path: custom tenant_id claim issued by JwtTokenService
        var tenantClaim = context.User?.FindFirstValue("tenant_id");

        if (Guid.TryParse(tenantClaim, out var tenantId))
        {
            tenantService.SetTenantId(tenantId);
        }
        else if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Azure AD path: map Entra tenant to our internal app tenant (single-tenant app)
            var internalTenantId = config["Auth:AzureAd:InternalTenantId"];
            if (Guid.TryParse(internalTenantId, out var internalTid))
                tenantService.SetTenantId(internalTid);
        }

        await _next(context);
    }
}
