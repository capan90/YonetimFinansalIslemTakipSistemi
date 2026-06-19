using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("user_permissions");

        // Birleşik PK: aynı kullanıcıya aynı yetki birden fazla kez eklenemez (DB seviyesinde)
        builder.HasKey(p => new { p.UserId, p.Permission });

        builder.Property(p => p.UserId)
               .HasColumnName("user_id");

        builder.Property(p => p.Permission)
               .HasConversion<int>()
               .HasColumnName("permission");

        builder.HasOne(p => p.User)
               .WithMany()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
