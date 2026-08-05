# Tema Sistemi

## Genel Yapı

WPF'in `DynamicResource` mekanizması üzerine kurulu, çalışma zamanında anlık tema değişimi sağlayan sistem.

---

## Bileşenler

### IThemeService (Application Katmanı)

```csharp
public interface IThemeService
{
    /// <summary>DB'den kaydedilmiş temayı döner. Kayıt yoksa "Light".</summary>
    Task<string> GetCurrentThemeAsync();

    /// <summary>Temayı DB'ye kaydeder ve anında uygular.</summary>
    Task SetThemeAsync(string theme);

    /// <summary>ResourceDictionary'yi takas eder. UI thread'inden çağrılmalıdır.</summary>
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

## Kaynak Sözlükleri

`App.xaml` beş sözlük yükler. **Sıra önemlidir** — sonra gelen öncekini ezer ve
`Controls.xaml`, `AppStyles.xaml`'daki ortak kaynaklara (`AppFocusVisual`) başvurur.

| Sıra | Dosya | İçerik |
|------|-------|--------|
| 1 | `Resources/AppStyles.xaml` | Tipografi ölçeği, buton stilleri, DataGrid, input şablonu, converter |
| 2 | `Resources/Controls.xaml` | Kontrol kabuğu — Menu, ComboBox, DatePicker, CheckBox, GroupBox, ScrollBar… |
| 3 | `Resources/PrintStyles.xaml` | Rapor önizlemesi için temadan bağımsız baskı katmanı |
| 4 | `Resources/Icons.xaml` | Vektör ikon geometrileri |
| 5 | `Resources/Themes/{Light,Dark}Theme.xaml` | Renk token'ları — çalışma zamanında takas edilen tek sözlük |

Yalnızca 5. sıradaki sözlük değişir; 1–4 sabit kalır ve renkleri
`{DynamicResource Theme.*}` ile o sözlükten okur.

### Neden ayrı bir `Controls.xaml` var?

WPF'in yerleşik (Aero2) kontrol şablonları renkleri **sabit hex ve sabit
SystemColors** olarak taşır; tema sözlüğünü hiç görmezler. Token setini
tamamlamak tek başına yetmez — kontrol şablonlarının da yeniden yazılması gerekir.
Gerekçe ve tuzak listesi: `docs/05-ADR/ADR-005-ThemeSystem.md`.

Şablon yazarken WPF'in **adıyla aradığı** parçalar korunur: `PART_Popup`,
`PART_EditableTextBox`, `PART_ContentHost`, `PART_TextBox`, `PART_Button`,
`PART_Track`, `PART_Indicator`. Değiştirilirse klavye navigasyonu ve seçim
mantığı sessizce bozulur.

---

## Tema Değişim Mekanizması

`DynamicResource` kullanılan kontroller tema dosyası değiştiğinde otomatik güncellenir:

```csharp
// ThemeService.ApplyTheme()
var dicts = System.Windows.Application.Current.Resources.MergedDictionaries;
var old = dicts.FirstOrDefault(d => IsThemeDictionary(d));

// Aynı tema zaten yüklüyse dokunma: sözlüğü çıkarıp yeniden eklemek
// tüm DynamicResource'ları gereksiz yere yeniden çözdürür
if (old is not null && old.Source!.OriginalString.Contains(themeName)) return;

if (old is not null) dicts.Remove(old);
dicts.Add(new ResourceDictionary
{
    Source = new Uri($"pack://application:,,,/Resources/Themes/{themeName}.xaml")
});
```

---

## Tema Saklama

Aktif tema `ApplicationSettings` tablosunda `UI:Theme` anahtarıyla saklanır.

```
Key:   "UI:Theme"
Value: "Light" | "Dark" | "System"
```

Uygulama başlangıcında:
1. `ApplicationSettings`'ten `UI:Theme` okunur.
2. `ThemeService.ApplyTheme(savedTheme)` çağrılır.
3. Geçersiz değer varsa `"Light"` varsayılanı kullanılır.

**Saklanan tercih ile uygulanan tema ayrıdır.** `System` saklanır; uygulanacak
gerçek tema her açılışta ve Windows ayarı değiştiğinde yeniden çözülür.

---

## Geçerli Tema Kısıtı

```csharp
private static bool IsValidTheme(string? theme) =>
    theme is "Light" or "Dark" or "System";
```

Üç değer de geçerlidir. Koyu tema 2026-08-05'te üretime açıldı — kontrol
şablonları temaya bağlandıktan sonra (bkz. ADR-005).

---

## Sistem Teması Takibi

`System` seçiliyken Windows'un kişiselleştirme ayarı okunur:

```
HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
  AppsUseLightTheme  →  0 = koyu, 1 = açık
```

Anahtar yoksa veya okunamıyorsa açık temaya düşülür (uyarı loglanır).

`SystemEvents.UserPreferenceChanged` aboneliği **yalnızca `System` seçiliyken**
açık tutulur. Diğer modlarda OS değişikliği kullanıcının bilinçli seçimini
ezmemelidir. Olay UI thread'inde gelmeyebileceği için `Dispatcher.Invoke` ile
sarmalanır — `ResourceDictionary` değişimi UI thread ister.

---

## Görünüm Ayarları Ekranı

`AppearanceSettingsWindow`:
- Tüm kullanıcılar erişebilir (yetki gerektirmez).
- Üç seçenek: Açık Tema / Koyu Tema / Sistem temasını takip et.
- Değişiklik anında uygulanır; pencereler yeniden açılmadan güncellenir.

---

## Renk Verirken Uyulacak Kurallar

| Bağlam | Doğru | Yanlış |
|--------|-------|--------|
| XAML | `{DynamicResource Theme.X}` | ham hex, `White`/`Black`, `StaticResource` |
| Code-behind | `ThemeBrush.Apply(el, dp, "Theme.X")` | `el.Foreground = ThemeBrush.Get("Theme.X")` |
| Koşullu renk | `DataTrigger` + `DynamicResource` | `Brush` döndüren converter |
| Yerel stil | `BasedOn="{StaticResource {x:Type X}}"` | `BasedOn` yok |
| Kod stili | `new Style(typeof(X), temelStil)` | `new Style(typeof(X))` |

Son ikisi en sinsi olanıdır: `BasedOn` almayan bir stil kontrolü uygulamanın
temasından **tamamen** koparıp WPF'in yerleşik stiline düşürür. Ham renk
taramasıyla yakalanmaz — ortada yanlış token yoktur, hiç token yoktur.

`ThemeBrush.Get()` yalnızca tek seferlik/değişmeyen kullanımlar için durur
(ör. iki temada da aynı olan baskı renkleri). Dinamik davranış gerekiyorsa
`Apply()` kullanılır.

---

## Baskı Katmanı (Rapor Önizleme)

Rapor önizlemesi uygulama temasından **bağımsızdır**:

```text
Pencere çerçevesi, başlık, araç çubuğu  →  Theme.*        (aktif tema)
Kâğıt ve üzerindeki her şey             →  Theme.PrintPreview.*  (sabit)
```

`Theme.PrintPreview.*` token'ları `LightTheme.xaml` ve `DarkTheme.xaml` içinde
**birebir aynı** değerlerle tanımlıdır. Kasıtlı tekrardır: baskı katmanı iki
sözlükte de aynı kâğıdı tanımlar. Parite testi bunu değer düzeyinde doğrular.

Rapor gövdesinde `Theme.Surface` / `Theme.Text` gibi dinamik token kullanılmaz.

---

## Regresyon Koruması

`tests/YonetimFinansalIslemTakipSistemi.UiTests` — 197 test. Parite, kontrast,
XAML parse, kontrol durum matrisi, pencere ağacı taraması, stil zinciri ve ham
renk denetimi. Ayrıntı için ADR-005 → *Nasıl Korunuyor*.

> **Not:** WPF'te `StaticResource`/`DynamicResource` hataları derleme zamanında
> yakalanmaz. "Build yeşil" bir pencerenin açılırken `XamlParseException`
> atmayacağı anlamına gelmez — parse testi bu boşluğu kapatır.

---

## WPF Namespace Çakışması

`ThemeService.cs`'te `System.Windows.Application` ile `YonetimFinansalIslemTakipSistemi.Application` namespace'i çakışır. Tam niteleme zorunludur:

```csharp
// Yanlış (derleme hatası)
Application.Current.Resources...

// Doğru
System.Windows.Application.Current.Resources...
```
