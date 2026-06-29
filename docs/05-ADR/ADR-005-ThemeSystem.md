# ADR-005: Tema Sistemi Kararları

**Tarih:** 2026-06-29  
**Durum:** Kabul Edildi (Koyu Tema Devre Dışı)

---

## Karar

WPF `DynamicResource` mekanizması üzerine `IThemeService` + `ThemeService` ile runtime tema değişimi. Koyu tema teknik olarak hazır ancak WPF kısıtları nedeniyle devre dışı bırakıldı.

---

## Bağlam

- Kullanıcılar açık/koyu tema seçeneği istedi.
- WPF tema değişimi için iki yaklaşım var: StaticResource (compile-time) veya DynamicResource (runtime).
- `ThemeService` WPF'e özgü (`Application.Current.Resources`) — Infrastructure'a taşınamaz.
- Tema tercihi kullanıcı başlatmalar arasında korunmalı.

---

## Alternatifler

### A: StaticResource (Yalnızca Uygulama Başlangıcında Tema)

Startup'ta tema seçilir; değişiklik için uygulama yeniden başlatılır.

**Sorun:** Kullanıcı deneyimi kötü — her tema değişiminde restart.

### B: DynamicResource + ResourceDictionary Swap (Seçilen)

Çalışma zamanında `App.xaml`'ın `MergedDictionaries`'ı güncellenir.

```csharp
mergedDicts.Remove(currentTheme);
mergedDicts.Add(new ResourceDictionary { Source = themeUri });
```

DynamicResource kullanan kontroller anında güncellenir.

### C: Harici Tema Kütüphanesi (MahApps, HandyControl)

Hazır tema sistemi.

**Sorun:** Dış bağımlılık. Mevcut custom stilleri bütünüyle değiştirmek gerekir. Bu ölçek için fazla.

---

## IThemeService Neden Application Katmanında?

`IThemeService` interface'i Application katmanındadır ama implementasyon UI katmanındadır.

**Gerekçe:** `ThemeService`'in `Apply()` metodu `System.Windows.Application.Current.Resources` erişimi gerektirir — bu WPF'e özgüdür. Infrastructure'a taşımak WPF bağımlılığı getirir (Infrastructure katmanı bunu bilmemeli).

Aynı pattern: `IUpdateService` (Application) → `UpdateService` (UI).

---

## Tema Tercihi Saklama

`ApplicationSettings` tablosunda `UI:Theme` anahtarı.

**Neden DB, neden appsettings.json değil?**
- `appsettings.json` tüm kullanıcılar için ortak.
- DB ayarı her kullanıcı için ayrı saklanabilir (gelecekte per-user tema mümkün).
- AES şifreli ayar altyapısıyla tutarlı.

Not: V1'de per-user değil, global tema var. Anahtar evrensel.

---

## Neden Koyu Tema Devre Dışı?

WPF kontrolleri (DataGrid, ComboBox, ScrollBar, ContextMenu) çoğunlukla iç ControlTemplate'e sahiptir. Bu template'lerdeki renkler hard-coded olup `DynamicResource` tarafından ulaşılamaz.

Bulgular:
- `DarkTheme.xaml` kök renkleri değiştiriyor.
- Ama DataGrid seçili satır, ComboBox dropdown, ScrollBar track gibi yerler hatalı renk gösteriyor.
- Tam destek için tüm ControlTemplate'lerin override edilmesi gerekiyor (büyük iş).

**Karar:** Koyu tema UI hazır ("Yakında" olarak gösterilir), geliştirme V2'ye bırakıldı.

---

## Artılar

- Anlık tema değişimi — restart yok.
- Tema dosyaları ayrı ve yönetilebilir.
- Yeni tema eklemek: yeni `.xaml` dosyası + `IsValidTheme` güncellemesi.

---

## Eksiler

- WPF bazı kontrollerde DynamicResource'u geçemez (ControlTemplate derinliği).
- Koyu tema tam anlamıyla çalışana kadar devre dışı kalacak.

---

## Sonuç

Açık tema stabil çalışıyor. Koyu tema için gerekli iş miktarı tahmin edilenden büyük. V2'de tüm custom ControlTemplate'ler gözden geçirilecek.
