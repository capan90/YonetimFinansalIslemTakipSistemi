# Tema Sistemi

## Genel Yapı

WPF'in `DynamicResource` mekanizması üzerine kurulu, çalışma zamanında anlık tema değişimi sağlayan sistem.

---

## Bileşenler

### IThemeService (Application Katmanı)

```csharp
public interface IThemeService
{
    string CurrentTheme { get; }
    void ApplyTheme(string theme);
}
```

### ThemeService (UI Katmanı)

`IThemeService`'i implement eder. **UI katmanında** bulunur çünkü `System.Windows.Application.Current` erişimi gerektirir — bu WPF'e özgüdür, Infrastructure'a taşınamaz.

**Singleton** olarak kayıt edilir.

```csharp
// App.xaml.cs
services.AddSingleton<IThemeService, ThemeService>();
```

---

## Tema Dosyaları

| Dosya | Konum |
|-------|-------|
| `LightTheme.xaml` | `UI/Themes/LightTheme.xaml` |
| `DarkTheme.xaml` | `UI/Themes/DarkTheme.xaml` |

Her tema dosyası `ResourceDictionary` içerir: renkler, fırçalar, stiller.

---

## Tema Değişim Mekanizması

`DynamicResource` kullanılan kontroller tema dosyası değiştiğinde otomatik güncellenir:

```csharp
// ThemeService.ApplyTheme()
var mergedDicts = System.Windows.Application.Current.Resources.MergedDictionaries;
var themeDict = mergedDicts.FirstOrDefault(d => IsThemeDictionary(d));
if (themeDict != null) mergedDicts.Remove(themeDict);
mergedDicts.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Themes/{theme}Theme.xaml") });
```

---

## Tema Saklama

Aktif tema `ApplicationSettings` tablosunda `UI:Theme` anahtarıyla saklanır.

```
Key:   "UI:Theme"
Value: "Light"    (veya "Dark" — şimdilik devre dışı)
```

Uygulama başlangıcında:
1. `ApplicationSettings`'ten `UI:Theme` okunur.
2. `ThemeService.ApplyTheme(savedTheme)` çağrılır.
3. Geçersiz değer varsa `"Light"` varsayılanı kullanılır.

---

## Geçerli Tema Kısıtı

```csharp
private static bool IsValidTheme(string? theme) => theme == "Light";
```

Koyu tema `DarkTheme.xaml` teknik olarak hazırdır ancak WPF DynamicResource tutarsızlıkları ve stil sorunları nedeniyle devre dışı bırakılmıştır.

`AppearanceSettingsWindow`'da "Koyu Tema (Yakında)" olarak gösterilir ve `IsEnabled="False"` ile kilitlidir.

---

## Görünüm Ayarları Ekranı

`AppearanceSettingsWindow`:
- Tüm kullanıcılar erişebilir (yetki gerektirmez).
- Yalnızca "Açık Tema" seçilebilir.
- Değişiklik anında uygulanır ve ayarlar kaydedilir.

---

## WPF Namespace Çakışması

`ThemeService.cs`'te `System.Windows.Application` ile `YonetimFinansalIslemTakipSistemi.Application` namespace'i çakışır. Tam niteleme zorunludur:

```csharp
// Yanlış (derleme hatası)
Application.Current.Resources...

// Doğru
System.Windows.Application.Current.Resources...
```
