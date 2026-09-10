using Rentlyo.Application.Interfaces;

namespace Rentlyo.Infrastructure.Tenancy;

public class TenantContext : ITenantContext
{
    public bool IsAuthenticated { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = string.Empty;

    public void Set(Guid tenantId, Guid userId, string role)
    {
        TenantId = tenantId;
        UserId = userId;
        Role = role;
        IsAuthenticated = true;
    }
}
