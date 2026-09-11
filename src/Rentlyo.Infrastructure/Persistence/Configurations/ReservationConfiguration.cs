using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentlyo.Domain.Entities;

namespace Rentlyo.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.DailyPriceSnapshot).HasPrecision(18, 2);
        builder.Property(x => x.TotalPrice).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasIndex(x => new { x.TenantId, x.VehicleId, x.StartAt });
        builder.HasIndex(x => new { x.TenantId, x.Status });

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PickupLocation)
            .WithMany()
            .HasForeignKey(x => x.PickupLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DropoffLocation)
            .WithMany()
            .HasForeignKey(x => x.DropoffLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
