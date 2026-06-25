using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

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
        // HasDefaultValue enum tipiyle verilmeli — int vermek CLR tipi uyumsuzluğu hatası üretir
        builder.Property(x => x.Priority).HasConversion<int>().HasDefaultValue(CargoShipmentPriority.Normal);
        builder.Property(x => x.CreatedFrom).HasConversion<int>().HasDefaultValue(CargoShipmentCreatedFrom.Manual);

        // Snapshot alanları: oluşturma anındaki firma bilgileri; sonradan değişmez
        builder.Property(x => x.ReceiverCompanyNameSnapshot).HasMaxLength(200);
        builder.Property(x => x.ReceiverAddressSnapshot).HasMaxLength(500);
        builder.Property(x => x.ReceiverAttentionSnapshot).HasMaxLength(200);
        builder.Property(x => x.ReceiverCitySnapshot).HasMaxLength(100);
        builder.Property(x => x.ReceiverDistrictSnapshot).HasMaxLength(100);
        builder.Property(x => x.ReceiverPhoneSnapshot).HasMaxLength(50);
        builder.Property(x => x.ReceiverEmailSnapshot).HasMaxLength(200);

        // ShipmentNumber: null olmayan değerler benzersiz olmalı; null kayıtlar kısıtlamadan muaf
        builder.HasIndex(x => x.ShipmentNumber)
            .IsUnique()
            .HasFilter("\"ShipmentNumber\" IS NOT NULL");

        // CargoCompany: opsiyonel — gelen kargoda firma bilinmeyebilir
        builder.HasOne(x => x.CargoCompany)
            .WithMany(c => c.CargoShipments)
            .HasForeignKey(x => x.CargoCompanyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // CompanyDirectory: FK tarihsel referans; operasyon verisi Snapshot'ta
        builder.HasOne(x => x.CompanyDirectory)
            .WithMany(d => d.CargoShipments)
            .HasForeignKey(x => x.CompanyDirectoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
