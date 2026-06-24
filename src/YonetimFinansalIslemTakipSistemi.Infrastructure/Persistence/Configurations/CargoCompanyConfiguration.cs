using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class CargoCompanyConfiguration : IEntityTypeConfiguration<CargoCompany>
{
    public void Configure(EntityTypeBuilder<CargoCompany> builder)
    {
        builder.ToTable("cargo_companies");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TrackingUrlTemplate).HasMaxLength(500);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Website).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
