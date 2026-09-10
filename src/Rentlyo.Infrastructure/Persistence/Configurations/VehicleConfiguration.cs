using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentlyo.Domain.Entities;

namespace Rentlyo.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Brand).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Model).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PlateNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DailyPrice).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.PlateNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
