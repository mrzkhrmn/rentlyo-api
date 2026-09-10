namespace Rentlyo.Domain.Constants;

public static class SystemRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    public static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid ManagerId = Guid.Parse("11111111-1111-1111-1111-111111111103");
    public static readonly Guid EmployeeId = Guid.Parse("11111111-1111-1111-1111-111111111104");
}

public static class SystemPermissions
{
    public const string VehiclesRead = "vehicles.read";
    public const string VehiclesWrite = "vehicles.write";
    public const string ReservationsRead = "reservations.read";
    public const string ReservationsWrite = "reservations.write";
    public const string CustomersRead = "customers.read";
    public const string CustomersWrite = "customers.write";
    public const string SettingsManage = "settings.manage";
    public const string EmployeesManage = "employees.manage";

    public static readonly Guid VehiclesReadId = Guid.Parse("22222222-2222-2222-2222-222222222201");
    public static readonly Guid VehiclesWriteId = Guid.Parse("22222222-2222-2222-2222-222222222202");
    public static readonly Guid ReservationsReadId = Guid.Parse("22222222-2222-2222-2222-222222222203");
    public static readonly Guid ReservationsWriteId = Guid.Parse("22222222-2222-2222-2222-222222222204");
    public static readonly Guid CustomersReadId = Guid.Parse("22222222-2222-2222-2222-222222222205");
    public static readonly Guid CustomersWriteId = Guid.Parse("22222222-2222-2222-2222-222222222206");
    public static readonly Guid SettingsManageId = Guid.Parse("22222222-2222-2222-2222-222222222207");
    public static readonly Guid EmployeesManageId = Guid.Parse("22222222-2222-2222-2222-222222222208");
}
