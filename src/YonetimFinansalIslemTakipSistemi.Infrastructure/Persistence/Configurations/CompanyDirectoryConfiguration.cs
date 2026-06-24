using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class CompanyDirectoryConfiguration : IEntityTypeConfiguration<CompanyDirectory>
{
    public void Configure(EntityTypeBuilder<CompanyDirectory> builder)
    {
        builder.ToTable("company_directories");

        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AddressLine).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContactPerson).HasMaxLength(200);
        builder.Property(x => x.AttentionTo).HasMaxLength(200);
        builder.Property(x => x.District).HasMaxLength(100);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
