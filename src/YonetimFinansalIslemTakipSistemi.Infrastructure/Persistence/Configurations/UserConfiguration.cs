using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(x => x.UserName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        // BCrypt hash çıktısı sabit 60 karakterdir
        builder.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();

        // Giriş sorgusunda kullanılan unique index
        builder.HasIndex(x => x.UserName).IsUnique();

        // Soft delete: IsDeleted=true olan kullanıcılar tüm sorgulardan otomatik filtrelenir
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
