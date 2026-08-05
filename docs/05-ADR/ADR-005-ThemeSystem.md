# ADR-005: Tema Sistemi Kararları

**Tarih:** 2026-06-29  
**Güncelleme:** 2026-08-05 — koyu tema açıldı, kontrol şablonları yeniden yazıldı  
**Durum:** Kabul Edildi

---

## Karar

WPF `DynamicResource` mekanizması üzerine `IThemeService` + `ThemeService` ile runtime tema değişimi. Üç geçerli tercih: `Light`, `Dark`, `System`.

Koyu temanın çalışması için token setinin yanı sıra **kontrol şablonlarının da yeniden yazılması** gerekti; bu ADR'nin ilk sürümünde öngörülen "büyük iş" 2026-08-05'te yapıldı (bkz. *Koyu Tema Nasıl Açıldı*).

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

## Koyu Tema Nasıl Açıldı

Bu ADR'nin ilk sürümü doğru teşhis koymuştu: WPF kontrolleri (DataGrid, ComboBox,
ScrollBar, ContextMenu) iç `ControlTemplate`'e sahiptir ve o şablonlardaki renkler
sabittir, `DynamicResource` oralara ulaşamaz. Eksik olan tek şey işin yapılmasıydı.

Faz B (2026-08-03) yalnızca ilk yarıyı yaptı — 304 ham hex `Theme.*` token'larına
taşındı ve koyu tema açıldı. Manuel testte tema üretim seviyesinde kullanılamadı:
menüler görünmüyordu, alt ekranlarda metin kayboluyordu, ComboBox listeleri
okunmuyordu. Ham hex taraması ise tertemizdi.

**Öğrenilen:** Sorun token'larda değil, **token'ların hiç kullanılmadığı yerdeydi.**
Ham renk taraması bu hatayı yakalayamaz; ortada yanlış token yoktur, hiç token yoktur.

Faz B düzeltmesinde (2026-08-05) dört kök neden kapatıldı:

**1. `Window.Background` bağlanmamıştı.** Global `Window` stili `Foreground`'u temaya
bağlamış, `Background`'u bağlamamıştı. Zemin verilmeyen bir Window
`SystemColors.WindowBrush` = **beyaz** kullanır. 43 XAML'in 31'i kök panelinde zemin
vermiyordu; hepsi koyu temada beyaz zemine açık metin çiziyordu. En büyük tekil neden.

**2. Yerleşik (Aero2) şablonlar tema körüdür.** `Resources/Controls.xaml` ile yeniden
yazıldılar. Öne çıkan tuzaklar:

| Kontrol | Tuzak |
|---|---|
| `MenuItem` | Aero2 stili `Foreground`'u `#212121`'e **setter olarak sabitler** — miras değil, bu yüzden pencereden devralınmaz |
| `Popup` / `ContextMenu` | Ayrı visual tree'dedir; pencerenin `Background`/`Foreground`'unu **miras almaz** |
| `ComboBox` | Kapalı kutu sabit gradyan; açılır liste `SystemColors.WindowBrush` (beyaz) iken item metni ComboBox'tan miras alır → beyaz üstüne beyaz |
| `TextBox` | Devre dışı durumda zemin `#F0F0F0`, metin `#838383` sabittir; `SelectionBrush` hiç tanımlı değildi |
| `DatePickerTextBox` | Ayrı bir tiptir; örtük `TextBox` stili ona uygulanmaz |
| `Button` | 149 butonun ~50'si hiçbir stil almıyor, sabit gradyan şablona düşüyordu |

Şablon yazarken `PART_Popup`, `PART_EditableTextBox`, `PART_ContentHost`,
`PART_TextBox`, `PART_Button` adları korunur — WPF bu parçaları **adıyla** arar;
değiştirilirse klavye navigasyonu ve seçim mantığı bozulur.

**3. Fırça döndüren converter'lar.** Converter bağlama başına **bir kez** çalışır;
döndürdüğü `Brush` örneği o bağlamada donar. Tema değiştiğinde sözlükteki fırça
değişir ama ekranda duran eski örnek yerinde kalır. Sistem Logları'nda açık listedeki
satırlar eski renkte kalıyordu.

> **Kural:** Converter renk döndürmez. Renk `DataTrigger` + `DynamicResource` ile verilir;
> setter içindeki `DynamicResource` tema değişiminde anında güncellenir.
> Code-behind'da karşılığı `SetResourceReference`'tır (`ThemeBrush.Apply`),
> fırçayı okuyup atamak (`ThemeBrush.Get`) **değil**.

**4. `BasedOn`'suz yerel stiller.** `BasedOn` almayan bir `Style`, kontrolü uygulamanın
örtük tema stilinden **tamamen** koparır ve yerleşik stile düşürür. Kod tarafında da
aynı tuzak vardır: `new Style(typeof(X))` tek argümanlı biçimi BasedOn taşımaz.

---

## Rapor Önizlemesi Neden Temadan İzole?

Rapor önizlemesi, PDF/Excel çıktısının ekrandaki karşılığıdır. Faz B'de gövde
`Theme.Surface` / `Theme.Text` gibi **dinamik** token'lara bağlanmıştı; koyu temada
kâğıt kayboldu ve metin zeminle aynı renge yaklaştı.

**Karar:** Önizleme iki katmandır.

```text
Pencere çerçevesi, başlık, araç çubuğu, Kapat/Export butonları
    → aktif uygulama teması (Theme.*)
Kâğıt ve üzerindeki her şey
    → Theme.PrintPreview.*  (Light ve Dark sözlüklerinde AYNI değer)
```

Baskı katmanı `Theme.Surface` / `Theme.Text` gibi dinamik token kullanamaz. Parite
testi bu token'ları anahtar düzeyinde değil **değer** düzeyinde karşılaştırır — biri
kazara "temaya uydurulursa" test kırılır.

---

## Sistem Teması Takibi

Üçüncü tercih `System`, Windows'un `AppsUseLightTheme` kayıt defteri anahtarını okur.
Abonelik (`SystemEvents.UserPreferenceChanged`) **yalnızca `System` seçiliyken** açık
tutulur — diğer modlarda OS değişikliği kullanıcının bilinçli seçimini ezmemelidir.

Saklanan tercih ile uygulanan tema ayrıdır: `System` saklanır, `Light`/`Dark` uygulanır.

---

## Artılar

- Anlık tema değişimi — restart yok.
- Tema dosyaları ayrı ve yönetilebilir.
- Kontroller artık tek bir rol setine bakıyor; yeni ekran yazarken renk kararı verilmiyor.
- Regresyon otomatik yakalanıyor (bkz. *Nasıl Korunuyor*).

---

## Eksiler

- Kontrol şablonlarının bakımı artık bizde. WPF sürüm yükseltmelerinde yerleşik
  şablonlardaki iyileştirmeler bize otomatik gelmez.
- Yeni bir kontrol tipi kullanıldığında (ör. `TabControl`, `Expander`) şablonu
  yazılana kadar tema körü kalır. Durum matrisi testi bunu hatırlatır.
- Tema başına iki sözlük elle senkron tutuluyor; parite testi zorunluluk.

---

## Nasıl Korunuyor

`tests/YonetimFinansalIslemTakipSistemi.UiTests` (197 test) bu ADR'nin kararlarını
çalıştırılabilir hâle getirir:

| Test | Neyi korur |
|---|---|
| Parite | Light/Dark anahtar seti, zorunlu roller, PrintPreview **değer** eşitliği |
| Kontrast | 68 semantik metin/zemin çifti × 2 tema (WCAG AA) |
| XAML parse | 40 pencere × 2 tema — çözülemeyen kaynak derleme zamanında yakalanmaz |
| Durum matrisi | 19 kontrol tipinde örtük stil + `Template` varlığı; 45 (kontrol, durum) satırı |
| Ağaç taraması | 213 metin öğesinin çözülmüş rengi ile gerçek zemini |
| Stil zinciri | `BasedOn`'suz XAML stili ve tek argümanlı `new Style(typeof(X))` |
| Ham renk | XAML hex, adlandırılmış sistem rengi, code-behind sabit fırça, Brush döndüren converter |

---

## Sonuç

Koyu tema üretime hazır. Bu ADR'nin ilk sürümündeki "V2'de tüm custom ControlTemplate'ler
gözden geçirilecek" maddesi kapandı.

Kalan bilinen sınır: kargo dashboard grafik barlarının rengi Application katmanındaki
DTO'dan gelir (`CargoDashboardChartItem.Color`) ve tema token'ı değildir. Grafik paleti
Faz C'nin konusudur.
