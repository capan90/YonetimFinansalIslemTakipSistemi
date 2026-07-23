using YonetimFinansalIslemTakipSistemi.Application.Features.Users;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.CreateUser;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.UpdateUser;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Parola politikası (min 8 karakter) ve kullanıcı handler'larına entegrasyonu — Sprint 17.
/// </summary>
public class PasswordPolicyTests
{
    /// <summary>Test için hash maliyeti gereksiz — düz işaretli sahte hasher.</summary>
    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "HASH:" + password;
        public bool Verify(string password, string hash) => hash == "HASH:" + password;
    }

    private static FakeUserContext AdminUser()
    {
        var user = new FakeUserContext();
        user.SetUser(user.UserId, user.FullName,
            new HashSet<PermissionType> { PermissionType.CanManageUsers });
        return user;
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("kisa")]
    [InlineData("1234567")] // 7 karakter — sınırın hemen altı
    public void Validate_GecersizParolalar_HataDondurur(string password)
        => Assert.NotNull(PasswordPolicy.Validate(password));

    [Theory]
    [InlineData("12345678")] // tam sınır
    [InlineData("Uzun-Ve-Guvenli-Parola-2026")]
    public void Validate_GecerliParolalar_NullDondurur(string password)
        => Assert.Null(PasswordPolicy.Validate(password));

    [Fact]
    public async Task CreateUser_KisaParola_Reddedilir()
    {
        var repo    = new FakeUserRepository();
        var handler = new CreateUserHandler(repo, new FakePasswordHasher(), new FakeAuditLogService(), AdminUser());

        var result = await handler.HandleAsync(new CreateUserRequest
        {
            FullName = "Yeni Kullanıcı", UserName = "yeni", Password = "kisa"
        });

        Assert.False(result.Success);
        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task CreateUser_GecerliParola_Basarili()
    {
        var repo    = new FakeUserRepository();
        var handler = new CreateUserHandler(repo, new FakePasswordHasher(), new FakeAuditLogService(), AdminUser());

        var result = await handler.HandleAsync(new CreateUserRequest
        {
            FullName = "Yeni Kullanıcı", UserName = "yeni", Password = "Gecerli-123"
        });

        Assert.True(result.Success);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task UpdateUser_KisaYeniParola_Reddedilir_HashVeAdDegismez()
    {
        var repo = new FakeUserRepository();
        var user = new User
        {
            Id = Guid.NewGuid(), FullName = "Mevcut Ad", UserName = "mevcut",
            PasswordHash = "HASH:eski", IsActive = true
        };
        repo.Items.Add(user);
        // "Son aktif kullanıcı pasifleştirilemez" kuralına takılmamak için ikinci aktif kullanıcı
        repo.Items.Add(new User { Id = Guid.NewGuid(), FullName = "Diğer", UserName = "diger", IsActive = true });

        var handler = new UpdateUserHandler(repo, new FakePasswordHasher(), new FakeAuditLogService(), AdminUser());

        var result = await handler.HandleAsync(new UpdateUserRequest
        {
            Id = user.Id, FullName = "Değişen Ad", IsActive = true, NewPassword = "kisa"
        });

        Assert.False(result.Success);
        // Fail dönüşünde entity üzerinde kaydedilmemiş değişiklik bırakılmamalı
        Assert.Equal("Mevcut Ad", user.FullName);
        Assert.Equal("HASH:eski", user.PasswordHash);
    }

    [Fact]
    public async Task UpdateUser_ParolaBosBirakilirsa_MevcutHashKorunur()
    {
        var repo = new FakeUserRepository();
        var user = new User
        {
            Id = Guid.NewGuid(), FullName = "Mevcut Ad", UserName = "mevcut",
            PasswordHash = "HASH:eski", IsActive = true
        };
        repo.Items.Add(user);

        var handler = new UpdateUserHandler(repo, new FakePasswordHasher(), new FakeAuditLogService(), AdminUser());

        var result = await handler.HandleAsync(new UpdateUserRequest
        {
            Id = user.Id, FullName = "Yeni Ad", IsActive = true, NewPassword = null
        });

        Assert.True(result.Success);
        Assert.Equal("HASH:eski", user.PasswordHash);
        Assert.Equal("Yeni Ad", user.FullName);
    }
}
