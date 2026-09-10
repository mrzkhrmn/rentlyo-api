using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentlyo.Domain.Entities;

namespace Rentlyo.Infrastructure.Persistence.Configurations;

public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.ToTable("TenantSettings");
        builder.HasKey(x => x.TenantId);
        builder.Property(x => x.Timezone).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Locale).HasMaxLength(20).IsRequired();

        builder.HasOne(x => x.Tenant)
            .WithOne(x => x.Settings)
            .HasForeignKey<TenantSettings>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
