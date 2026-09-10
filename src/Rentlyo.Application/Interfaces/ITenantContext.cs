namespace Rentlyo.Application.Interfaces;

public interface ITenantContext
{
    bool IsAuthenticated { get; }
    Guid TenantId { get; }
    Guid UserId { get; }
    string Role { get; }
    void Set(Guid tenantId, Guid userId, string role);
}
