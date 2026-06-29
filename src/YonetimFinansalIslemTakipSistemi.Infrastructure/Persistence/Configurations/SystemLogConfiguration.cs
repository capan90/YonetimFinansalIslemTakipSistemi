using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("system_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Level).IsRequired();
        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Message).IsRequired().HasColumnType("text");
        builder.Property(x => x.ExceptionType).HasMaxLength(500);
        builder.Property(x => x.StackTrace).HasColumnType("text");
        builder.Property(x => x.InnerExceptionMessage).HasColumnType("text");
        builder.Property(x => x.Source).HasMaxLength(500);
        builder.Property(x => x.Username).HasMaxLength(200);
        builder.Property(x => x.MachineName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.AppVersion).HasMaxLength(50);
        builder.Property(x => x.ResolutionNote).HasColumnType("text");
        builder.Property(x => x.IsCritical).IsRequired();
        builder.Property(x => x.IsResolved).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Level);
        builder.HasIndex(x => x.IsCritical);
        builder.HasIndex(x => x.IsResolved);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.UserId);
    }
}
