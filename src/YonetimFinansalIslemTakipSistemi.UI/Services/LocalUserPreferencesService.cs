using System.IO;
using System.Text.Json;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.UI.Services;

/// <summary>
/// Kullanıcıya özel lokal tercihler — DB'ye yazılmaz, makine başına saklanır.
/// Konum: %AppData%\YonetimFinansalIslemTakipSistemi\user-preferences.json
/// </summary>
public class LocalUserPreferencesService : ILocalUserPreferencesService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YonetimFinansalIslemTakipSistemi",
        "user-preferences.json");

    public async Task<string?> GetLastUsernameAsync()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var json = await File.ReadAllTextAsync(FilePath);
            var doc  = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("LastUsername", out var prop))
                return prop.GetString();
            return null;
        }
        catch
        {
            // Bozuk veya okunamaz dosya — login ekranı patlamasın, tercih yok say
            return null;
        }
    }

    public async Task SaveLastUsernameAsync(string username)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            // Şifre kesinlikle kaydedilmez — sadece kullanıcı adı
            var json = JsonSerializer.Serialize(new { LastUsername = username });
            await File.WriteAllTextAsync(FilePath, json);
        }
        catch
        {
            // Yazma hatası kullanıcı deneyimini engellememeli
        }
    }
}
