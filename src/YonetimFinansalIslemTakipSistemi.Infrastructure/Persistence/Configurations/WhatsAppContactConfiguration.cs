using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class WhatsAppContactConfiguration : IEntityTypeConfiguration<WhatsAppContact>
{
    public void Configure(EntityTypeBuilder<WhatsAppContact> builder)
    {
        builder.ToTable("whatsapp_contacts");

        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Company).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);

        // Filtresiz unique index: soft delete edilmiş kayıt da numarayı rezerve eder.
        // Aynı numara yeniden eklenmek istendiğinde Create handler kaydı geri yükler.
        builder.HasIndex(x => x.Phone).IsUnique();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
