using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class CargoNumberCounterConfiguration : IEntityTypeConfiguration<CargoNumberCounter>
{
    public void Configure(EntityTypeBuilder<CargoNumberCounter> builder)
    {
        builder.ToTable("cargo_number_counters");

        // Yön başına tek satır; artış UPDATE ... RETURNING ile atomik yapılır
        builder.HasKey(x => x.Direction);
        builder.Property(x => x.Direction).HasConversion<int>().ValueGeneratedNever();
        builder.Property(x => x.LastValue).IsRequired();
    }
}
