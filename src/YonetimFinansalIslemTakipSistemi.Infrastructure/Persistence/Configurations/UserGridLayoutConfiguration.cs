using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class UserGridLayoutConfiguration : IEntityTypeConfiguration<UserGridLayout>
{
    public void Configure(EntityTypeBuilder<UserGridLayout> builder)
    {
        builder.ToTable("user_grid_layouts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ScreenKey).IsRequired();
        builder.Property(x => x.LayoutJson).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // Her kullanıcı için her ekranda tek bir düzen kaydı
        builder.HasIndex(x => new { x.UserId, x.ScreenKey }).IsUnique();
    }
}
