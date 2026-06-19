using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("exchange_rates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RateDate)
               .HasColumnName("rate_date")
               .HasColumnType("timestamp with time zone")
               .IsRequired();

        builder.Property(e => e.CurrencyType)
               .HasConversion<int>()
               .HasColumnName("currency_type")
               .IsRequired();

        builder.Property(e => e.ForexBuying)
               .HasColumnName("forex_buying")
               .HasColumnType("numeric(18,4)")
               .IsRequired();

        builder.Property(e => e.ForexSelling)
               .HasColumnName("forex_selling")
               .HasColumnType("numeric(18,4)")
               .IsRequired();

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .HasColumnType("timestamp with time zone");

        builder.Property(e => e.CreatedByUserId)
               .HasColumnName("created_by_user_id");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .HasColumnType("timestamp with time zone");

        builder.Property(e => e.UpdatedByUserId)
               .HasColumnName("updated_by_user_id");

        // Soft-delete alanları BaseEntity'den gelir; V1'de kullanılmaz
        builder.Property(e => e.IsDeleted)
               .HasColumnName("is_deleted")
               .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
               .HasColumnName("deleted_at")
               .HasColumnType("timestamp with time zone");

        builder.Property(e => e.DeletedByUserId)
               .HasColumnName("deleted_by_user_id");

        // DB seviyesinde duplicate koruması: aynı tarih + para birimi için tek kayıt
        builder.HasIndex(e => new { e.RateDate, e.CurrencyType })
               .IsUnique()
               .HasDatabaseName("ix_exchange_rates_date_currency");
    }
}
