namespace Rentlyo.Domain.Entities;

public class VehicleImage
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
