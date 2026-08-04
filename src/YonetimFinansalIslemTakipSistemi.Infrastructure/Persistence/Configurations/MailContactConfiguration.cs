using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class MailContactConfiguration : IEntityTypeConfiguration<MailContact>
{
    public void Configure(EntityTypeBuilder<MailContact> builder)
    {
        builder.ToTable("mail_contacts");

        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();   // RFC 5321 üst sınırı
        builder.Property(x => x.Company).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);

        // WhatsApp rehberiyle aynı kural: filtresiz unique index — soft delete edilmiş
        // kayıt da adresi rezerve eder; aynı adres yeniden eklenmek istendiğinde
        // Create handler kaydı geri yükler (mükerrer satır oluşmaz).
        builder.HasIndex(x => x.Email).IsUnique();

        // Varsayılan CC listesi her mail ekranı açılışında sorgulanır — küçük ama sıcak sorgu
        builder.HasIndex(x => x.IsDefaultCc);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
