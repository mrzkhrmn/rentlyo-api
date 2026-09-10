using Rentlyo.Domain.Enums;

namespace Rentlyo.Application.DTOs.Vehicles;

public class VehicleImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class VehicleListItem
{
    public Guid Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public decimal DailyPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? PrimaryImageUrl { get; set; }
}

public class VehicleResponse
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public decimal DailyPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyList<VehicleImageDto> Images { get; set; } = [];
}

public class CreateVehicleRequest
{
    public Guid? CategoryId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public decimal DailyPrice { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;
}

public class UpdateVehicleRequest
{
    public Guid? CategoryId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public decimal DailyPrice { get; set; }
    public VehicleStatus Status { get; set; }
}

public class AddVehicleImageRequest
{
    public string Url { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class VehicleCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int VehicleCount { get; set; }
}

public class CreateVehicleCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateVehicleCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
