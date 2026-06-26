using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class ApplicationSettingConfiguration : IEntityTypeConfiguration<ApplicationSetting>
{
    public void Configure(EntityTypeBuilder<ApplicationSetting> builder)
    {
        builder.ToTable("application_settings");

        builder.Property(x => x.Key).HasMaxLength(200);
        builder.Property(x => x.Value).HasMaxLength(4000);

        // Aktif kayıtlar arasında Key benzersiz olmalı (soft-delete ile birden fazla silinmiş kayıt olabilir)
        builder.HasIndex(x => x.Key)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
