using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Rentlyo.Application.Interfaces;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;

namespace Rentlyo.API.Middleware;

public class TenantContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        ITenantContext tenantContext,
        ApplicationDbContext db)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userIdValue = httpContext.User.FindFirstValue("sub")
                ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tenantIdValue = httpContext.User.FindFirstValue("tenant_id");
            var role = httpContext.User.FindFirstValue("role")
                ?? httpContext.User.FindFirstValue(ClaimTypes.Role)
                ?? string.Empty;

            if (!Guid.TryParse(userIdValue, out var userId) || !Guid.TryParse(tenantIdValue, out var tenantId))
            {
                throw new UnauthorizedAppException("Invalid tenant claims.");
            }

            var tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == tenantId);

            if (tenant is null)
            {
                throw new UnauthorizedAppException("Tenant not found.");
            }

            if (tenant.Status is TenantStatus.Suspended or TenantStatus.Cancelled)
            {
                throw new ForbiddenException("Tenant is not active.");
            }

            tenantContext.Set(tenantId, userId, role);
        }

        await next(httpContext);
    }
}
