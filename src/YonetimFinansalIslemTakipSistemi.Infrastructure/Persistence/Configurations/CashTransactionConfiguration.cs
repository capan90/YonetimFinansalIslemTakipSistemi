using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class CashTransactionConfiguration : IEntityTypeConfiguration<CashTransaction>
{
    public void Configure(EntityTypeBuilder<CashTransaction> builder)
    {
        builder.ToTable("cash_transactions");

        // Finansal hesaplamalar için yeterli hassasiyet
        builder.Property(x => x.Amount)
            .HasPrecision(18, 4);

        // Soft delete: IsDeleted=true olan kayıtlar tüm sorgulardan otomatik filtrelenir
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
