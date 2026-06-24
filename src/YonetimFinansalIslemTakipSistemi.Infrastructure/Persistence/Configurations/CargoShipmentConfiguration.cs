using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class CargoShipmentConfiguration : IEntityTypeConfiguration<CargoShipment>
{
    public void Configure(EntityTypeBuilder<CargoShipment> builder)
    {
        builder.ToTable("cargo_shipments");

        builder.Property(x => x.ShipmentNumber).HasMaxLength(100);
        builder.Property(x => x.SenderName).HasMaxLength(200);
        builder.Property(x => x.ReceiverName).HasMaxLength(200);
        builder.Property(x => x.DeliveredBy).HasMaxLength(200);
        builder.Property(x => x.ReceivedBy).HasMaxLength(200);
        builder.Property(x => x.VehiclePlate).HasMaxLength(20);
        builder.Property(x => x.TrackingNumber).HasMaxLength(100);
        builder.Property(x => x.TrackingUrl).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.Direction).HasConversion<int>();
        builder.Property(x => x.ShipmentType).HasConversion<int?>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.NotificationStatus).HasConversion<int>();

        // CargoCompany: opsiyonel — gelen kargoda firma bilinmeyebilir
        builder.HasOne(x => x.CargoCompany)
            .WithMany(c => c.CargoShipments)
            .HasForeignKey(x => x.CargoCompanyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // CompanyDirectory: opsiyonel — gelen kargoda manuel gönderen yazılabilir
        builder.HasOne(x => x.CompanyDirectory)
            .WithMany(d => d.CargoShipments)
            .HasForeignKey(x => x.CompanyDirectoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
