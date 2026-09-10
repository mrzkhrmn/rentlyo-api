using Microsoft.EntityFrameworkCore;
using Rentlyo.Domain.Constants;
using Rentlyo.Domain.Entities;

namespace Rentlyo.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<VehicleCategory> VehicleCategories => Set<VehicleCategory>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        SeedAuthData(modelBuilder);
        SeedPlans(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void SeedPlans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubscriptionPlan>().HasData(
            new SubscriptionPlan
            {
                Id = SystemPlans.FreeId,
                Code = SystemPlans.Free,
                Name = "Free",
                MaxVehicles = 5,
                MaxUsers = 2,
                IsActive = true
            },
            new SubscriptionPlan
            {
                Id = SystemPlans.StarterId,
                Code = SystemPlans.Starter,
                Name = "Starter",
                MaxVehicles = 25,
                MaxUsers = 10,
                IsActive = true
            },
            new SubscriptionPlan
            {
                Id = SystemPlans.ProId,
                Code = SystemPlans.Pro,
                Name = "Pro",
                MaxVehicles = 100,
                MaxUsers = 50,
                IsActive = true
            });
    }

    private static void SeedAuthData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = SystemRoles.OwnerId, Name = SystemRoles.Owner },
            new Role { Id = SystemRoles.AdminId, Name = SystemRoles.Admin },
            new Role { Id = SystemRoles.ManagerId, Name = SystemRoles.Manager },
            new Role { Id = SystemRoles.EmployeeId, Name = SystemRoles.Employee });

        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = SystemPermissions.VehiclesReadId, Code = SystemPermissions.VehiclesRead, Name = "Read vehicles" },
            new Permission { Id = SystemPermissions.VehiclesWriteId, Code = SystemPermissions.VehiclesWrite, Name = "Write vehicles" },
            new Permission { Id = SystemPermissions.ReservationsReadId, Code = SystemPermissions.ReservationsRead, Name = "Read reservations" },
            new Permission { Id = SystemPermissions.ReservationsWriteId, Code = SystemPermissions.ReservationsWrite, Name = "Write reservations" },
            new Permission { Id = SystemPermissions.CustomersReadId, Code = SystemPermissions.CustomersRead, Name = "Read customers" },
            new Permission { Id = SystemPermissions.CustomersWriteId, Code = SystemPermissions.CustomersWrite, Name = "Write customers" },
            new Permission { Id = SystemPermissions.SettingsManageId, Code = SystemPermissions.SettingsManage, Name = "Manage settings" },
            new Permission { Id = SystemPermissions.EmployeesManageId, Code = SystemPermissions.EmployeesManage, Name = "Manage employees" });

        var allPermissionIds = new[]
        {
            SystemPermissions.VehiclesReadId,
            SystemPermissions.VehiclesWriteId,
            SystemPermissions.ReservationsReadId,
            SystemPermissions.ReservationsWriteId,
            SystemPermissions.CustomersReadId,
            SystemPermissions.CustomersWriteId,
            SystemPermissions.SettingsManageId,
            SystemPermissions.EmployeesManageId
        };

        var rolePermissions = new List<RolePermission>();

        foreach (var permissionId in allPermissionIds)
        {
            rolePermissions.Add(new RolePermission { RoleId = SystemRoles.OwnerId, PermissionId = permissionId });
            rolePermissions.Add(new RolePermission { RoleId = SystemRoles.AdminId, PermissionId = permissionId });
        }

        var managerPermissions = new[]
        {
            SystemPermissions.VehiclesReadId,
            SystemPermissions.VehiclesWriteId,
            SystemPermissions.ReservationsReadId,
            SystemPermissions.ReservationsWriteId,
            SystemPermissions.CustomersReadId,
            SystemPermissions.CustomersWriteId
        };

        foreach (var permissionId in managerPermissions)
        {
            rolePermissions.Add(new RolePermission { RoleId = SystemRoles.ManagerId, PermissionId = permissionId });
        }

        var employeePermissions = new[]
        {
            SystemPermissions.VehiclesReadId,
            SystemPermissions.ReservationsReadId,
            SystemPermissions.ReservationsWriteId,
            SystemPermissions.CustomersReadId
        };

        foreach (var permissionId in employeePermissions)
        {
            rolePermissions.Add(new RolePermission { RoleId = SystemRoles.EmployeeId, PermissionId = permissionId });
        }

        modelBuilder.Entity<RolePermission>().HasData(rolePermissions);
    }
}
