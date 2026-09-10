namespace Rentlyo.Domain.Entities;

public class TenantSettings
{
    public Guid TenantId { get; set; }
    public string Timezone { get; set; } = "Europe/Istanbul";
    public string Currency { get; set; } = "TRY";
    public string Locale { get; set; } = "tr-TR";

    public Tenant Tenant { get; set; } = null!;
}
