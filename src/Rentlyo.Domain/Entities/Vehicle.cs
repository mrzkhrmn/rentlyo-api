using Rentlyo.Domain.Enums;
using Rentlyo.Domain.Interfaces;

namespace Rentlyo.Domain.Entities;

public class Vehicle : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public decimal DailyPrice { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public VehicleCategory? Category { get; set; }
    public ICollection<VehicleImage> Images { get; set; } = new List<VehicleImage>();
}
