using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentlyo.Domain.Entities;

namespace Rentlyo.Infrastructure.Persistence.Configurations;

public class VehicleImageConfiguration : IEntityTypeConfiguration<VehicleImage>
{
    public void Configure(EntityTypeBuilder<VehicleImage> builder)
    {
        builder.ToTable("VehicleImages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => x.VehicleId);

        builder.HasOne(x => x.Vehicle)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
