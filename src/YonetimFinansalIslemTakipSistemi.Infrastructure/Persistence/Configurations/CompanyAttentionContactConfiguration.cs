using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class CompanyAttentionContactConfiguration : IEntityTypeConfiguration<CompanyAttentionContact>
{
    public void Configure(EntityTypeBuilder<CompanyAttentionContact> builder)
    {
        builder.ToTable("company_attention_contacts");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastUsedAt).IsRequired();

        builder.HasOne(x => x.CompanyDirectory)
               .WithMany(d => d.AttentionContacts)
               .HasForeignKey(x => x.CompanyDirectoryId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CompanyDirectoryId);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
