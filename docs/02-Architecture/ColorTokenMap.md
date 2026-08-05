# Renk Token Eşleme Tablosu

Faz B'de `UI/**/*.xaml` içindeki **304 ham hex kullanımı (93 farklı renk)**
`Theme.*` token'larına eşlendi. Bu dosya hangi hex'in neye gittiğini kaydeder —
ileride bir renk sapması görülürse buradan geri izlenebilir.

**Kural:** XAML'de ham hex yazılmaz. Yalnızca `{DynamicResource Theme.*}`.
Tema değiştiğinde güncellenmeyen tek şey `StaticResource` ve gömülü hex'lerdi;
koyu temanın kapalı kalma sebebi buydu.

---

## Marka / birincil

| Hex | Token | Not |
|-----|-------|-----|
| `#1E3A5F` | `Theme.Primary` | |
| `#1A3354` | `Theme.Primary` | Primary'nin bir tık koyusu; ayrı token açılmadı |
| `#2E5F9F` | `Theme.PrimaryHover` | |
| `#152C4A` | `Theme.PrimaryPressed` | |
| `#7BA7D9` | `Theme.PrimaryHover` | nav hover varyantı |

## Metin

| Hex | Token | Not |
|-----|-------|-----|
| `#1A202C`, `#222222` | `Theme.Text` | |
| `#475569`, `#374151`, `#5D6D7E` | `Theme.LabelText` | etiket/başlık rolü |
| `#64748B`, `#6B7280`, `#555555` | `Theme.MutedText` | dekoratif soluk metin |
| `#94A3B8`, `#AAAAAA` | `Theme.MutedText` | |

## Kenarlık / yüzey

| Hex | Token | Not |
|-----|-------|-----|
| `#E2E8F0`, `#DDDDDD`, `#E5E7EB`, `#D1D5DB` | `Theme.Border` | |
| `#CBD5E1` | `Theme.InputBorder` | |
| `White` | `Theme.Surface` | kart/panel zemini |
| `#F8FAFC`, `#FAFAFA`, `#F8F8F8`, `#F5F6FA`, `#F5F7FA`, `#F8F9FA` | `Theme.SurfaceSubtle` | |
| `#F1F5F9`, `#F5F5F5`, `#F0F0F0`, `#EEF4FA` | `Theme.SurfaceAlt` | araç çubuğu / alt şerit |

## Durum — bilgi (mavi)

| Hex | Token |
|-----|-------|
| `#EFF6FF`, `#F0F4FF`, `#EEF4FF`, `#DCE9FA`, `#E3F2FD` | `Theme.Info.Background` |
| `#DBEAFE` | `Theme.Info.BackgroundStrong` |
| `#B0C4F0`, `#90CAF9`, `#BFDBFE` | `Theme.Info.Border` |
| `#1E40AF`, `#1D4ED8`, `#2563EB`, `#1565C0` | `Theme.Info.Text` |

## Durum — başarı (yeşil)

| Hex | Token |
|-----|-------|
| `#F0FDF4`, `#E8F5E9`, `#F1F8E9`, `#F0FFF0` | `Theme.Success.Background` |
| `#86EFAC`, `#81C784`, `#BBF7D0`, `#A8D8A8` | `Theme.Success.Border` |
| `#166534`, `#15803D`, `#16A34A`, `#2E7D32` | `Theme.Success.Text` / `Theme.Success` |

> `#2E7D32` iki rolde: buton/metin vurgusu → `Theme.Success`,
> yumuşak zemin üzerindeki metin → `Theme.Success.Text`.

## Durum — uyarı (turuncu/sarı)

| Hex | Token |
|-----|-------|
| `#FFF7ED`, `#FFF8E1`, `#FFF3E0`, `#FEF3C7` | `Theme.Warning.Background` |
| `#FFEDD5` | `Theme.Warning.BackgroundStrong` |
| `#FED7AA` | `Theme.Warning.Border` |
| `#9A3412`, `#92400E`, `#C2410C`, `#EA580C`, `#D97706` | `Theme.Warning.Text` / `Theme.Warning` |

## Durum — hata (kırmızı)

| Hex | Token |
|-----|-------|
| `#FEF2F2`, `#FDECEA`, `#FFF5F5`, `#FFF1F2` | `Theme.Danger.Background` |
| `#FEE2E2` | `Theme.Danger.BackgroundStrong` |
| `#FECDD3`, `#FCA5A5` | `Theme.Danger.Border` |
| `#991B1B`, `#B91C1C`, `#C62828`, `#DC2626`, `#BE123C` | `Theme.Danger.Text` / `Theme.Danger` |
| `#B71C1C` | `Theme.DangerHover` |
| `#7F1010` | `Theme.DangerPressed` |

## Durum — mor vurgu

| Hex | Token |
|-----|-------|
| `#F5F3FF` | `Theme.Accent.Background` |
| `#EDE9FE` | `Theme.Accent.BackgroundStrong` |
| `#DDD6FE` | `Theme.Accent.Border` |
| `#5B21B6`, `#6D28D9`, `#7C3AED` | `Theme.Accent.Text` |

## Navigasyon şeridi (Kargo Dashboard)

| Hex | Token |
|-----|-------|
| `#1E3A5F` (NavBar zemini) | `Theme.Nav.Background` |
| `#2E5F9F` (nav buton) | `Theme.Nav.ItemBackground` |
| `#3B7BC8` (nav buton kenarlık) | `Theme.Nav.ItemBorder` |
| `White` (nav metin) | `Theme.Nav.Foreground` |

## Buton metin renkleri

Beyaz metin sabitlenemez — koyu temada `Theme.Primary` açık maviye,
`Theme.Danger` açık kırmızıya dönüyor ve beyaz metin **2.5:1**'e düşüyordu.

| Rol | Light | Dark |
|-----|-------|------|
| `Theme.OnPrimary` | White | `#0F172A` |
| `Theme.OnSecondary` | White | `#F1F5F9` |
| `Theme.OnDanger` | White | `#0F172A` |
| `Theme.OnDisabled` | White | `#CBD5E1` |

---

## Faz B düzeltmesinde eklenen roller (2026-08-05)

Faz B ham hex'leri temizledi ama **kontrollerin çoğu token'ları hiç kullanmıyordu**;
rengi WPF'in yerleşik şablonundan alıyorlardı. O şablonları temaya bağlamak için
eksik semantik roller açıldı. Gerekçe: `docs/05-ADR/ADR-005-ThemeSystem.md`.

### Menü

| Token | Light | Dark |
|-------|-------|------|
| `Theme.Menu.Background` | `#FFFFFF` | `#1E293B` |
| `Theme.Menu.Foreground` | `#1A202C` | `#F1F5F9` |
| `Theme.Menu.HoverBackground` | `#E2E8F0` | `#334155` |
| `Theme.Menu.HoverForeground` | `#1A202C` | `#F1F5F9` |
| `Theme.Menu.SelectedBackground` | `#DBEAFE` | `#223449` |
| `Theme.Menu.SelectedForeground` | `#1E40AF` | `#A9C6E6` |
| `Theme.Menu.DisabledForeground` | `#6B7688` | `#8A99AD` |
| `Theme.Menu.Separator` | `#E2E8F0` | `#334155` |

Aero2'nin `MenuItem` stili `Foreground`'u `#212121`'e **setter olarak sabitler**
(miras değil). Koyu menü zemini + koyu metin = görünmez menü.

### Popup

| Token | Light | Dark |
|-------|-------|------|
| `Theme.Popup.Background` | `#FFFFFF` | `#263348` |
| `Theme.Popup.Foreground` | `#1A202C` | `#F1F5F9` |
| `Theme.Popup.Border` | `#CBD5E1` | `#475569` |

Popup ayrı bir visual tree'dedir; pencerenin `Background`/`Foreground`'unu **miras
almaz**. Koyu temada popup yüzeyi `Theme.Surface`'ten bir kademe açıktır — üst üste
binen katmanlar yükseklik farkını gölgeyle değil açıklıkla anlatır.

### Input (mevcut `Theme.InputBackground/InputBorder/InputBorderFocus`'a ek)

| Token | Light | Dark |
|-------|-------|------|
| `Theme.Input.Placeholder` | `#64748B` | `#94A3B8` |
| `Theme.Input.DisabledBackground` | `#F1F5F9` | `#1A2436` |
| `Theme.Input.DisabledForeground` | `#596579` | `#9AA8BC` |
| `Theme.Input.ReadOnlyBackground` | `#F8FAFC` | `#1A2436` |
| `Theme.Input.SelectionBackground` | `#1E3A5F` | `#2E5F9F` |
| `Theme.Input.SelectionForeground` | `#FFFFFF` | `#F1F5F9` |

Seçim rengi tanımlı olmadığı için yazılan metin seçilince sistem mavisi + koyu metin
çakışıyor, "yazdığım kayboldu" görüntüsü oluşuyordu. Salt okunur alan devre dışı
**değildir**: metin tam kontrastta kalır, yalnızca zemin düzenlenemez olduğunu söyler.

### ComboBox

| Token | Light | Dark |
|-------|-------|------|
| `Theme.ComboBox.DropdownBackground` | `#FFFFFF` | `#263348` |
| `Theme.ComboBox.DropdownForeground` | `#1A202C` | `#F1F5F9` |
| `Theme.ComboBox.ItemHoverBackground` | `#EFF6FF` | `#334155` |
| `Theme.ComboBox.ItemHoverForeground` | `#1A202C` | `#F1F5F9` |
| `Theme.ComboBox.ItemSelectedBackground` | `#DBEAFE` | `#1E3A5F` |
| `Theme.ComboBox.ItemSelectedForeground` | `#1E40AF` | `#BFDBFE` |

Kapalı kutu ile açılır liste **ayrı yüzeylerdir**. WPF varsayılanında açılır liste
`SystemColors.WindowBrush` (beyaz) iken item metni ComboBox'tan miras alınıyordu →
koyu temada beyaz üstüne beyaz.

### Navigasyon (mevcut `Theme.Nav.*`'a ek)

| Token | Light | Dark |
|-------|-------|------|
| `Theme.Nav.HoverBackground` | `#2F6BB0` | `#2A4E7A` |
| `Theme.Nav.HoverForeground` | `#FFFFFF` | `#F1F5F9` |
| `Theme.Nav.ActiveBackground` | `#5391D6` | `#5391D6` |
| `Theme.Nav.ActiveForeground` | `#0B1B2E` | `#0B1B2E` |

Navigasyon şeridi her iki temada da **koyu kalır** — marka bandıdır, yüzey değildir.

### Kritik (dördüncü durum seviyesi)

| Token | Light | Dark |
|-------|-------|------|
| `Theme.Critical.Background` | `#FEE2E2` | `#3B1717` |
| `Theme.Critical.BackgroundStrong` | `#C62828` | `#7F1D1D` |
| `Theme.Critical.Border` | `#991B1B` | `#DC2626` |
| `Theme.Critical.Text` | `#FFFFFF` | `#FEE2E2` |

Sistem log seviyeleri dört basamaklıdır (Info < Warning < Error < Critical); Faz B'de
Critical'ın kendi rol seti yoktu ve Error ile aynı görünüyordu. Tek **dolu** seviye
budur — listede ilk görülmesi gereken satır.

### Nötr buton

| Token | Light | Dark |
|-------|-------|------|
| `Theme.Button.Background` | `#F1F5F9` | `#334155` |
| `Theme.Button.Foreground` | `#1A202C` | `#F1F5F9` |
| `Theme.Button.Border` | `#CBD5E1` | `#475569` |
| `Theme.Button.HoverBackground` | `#E2E8F0` | `#3F5069` |
| `Theme.Button.PressedBackground` | `#CBD5E1` | `#4A5D78` |
| `Theme.Button.DisabledBackground` | `#F1F5F9` | `#263348` |
| `Theme.Button.DisabledForeground` | `#6B7688` | `#8A99AD` |
| `Theme.Button.DisabledBorder` | `#E2E8F0` | `#334155` |

149 butonun ~50'si `Primary/Secondary/Danger` stillerinden hiçbirini kullanmıyor,
WPF'in sabit gradyanlı varsayılan şablonuna düşüyordu.

### ToolTip / ScrollBar

| Token | Light | Dark |
|-------|-------|------|
| `Theme.ToolTip.Background` | `#FFFFFF` | `#263348` |
| `Theme.ToolTip.Foreground` | `#1A202C` | `#F1F5F9` |
| `Theme.ToolTip.Border` | `#CBD5E1` | `#475569` |
| `Theme.ScrollBar.Track` | `#F1F5F9` | `#1A2436` |
| `Theme.ScrollBar.Thumb` | `#CBD5E1` | `#475569` |
| `Theme.ScrollBar.ThumbHover` | `#94A3B8` | `#64748B` |

### Sistem rengi override'ları

Kendi şablonunu yazmadığımız kontroller (Calendar, ScrollViewer köşesi, popup iç
parçaları) `SystemColors.*` anahtarlarına bakar. Override edilmezlerse Windows'un
beyaz/siyah sistem renkleri gelir ve koyu temada açık zemin üstünde açık metin oluşur.

Override edilenler: `Window`, `WindowText`, `Control`, `ControlText`, `GrayText`,
`Menu`, `MenuText`, `Info`, `InfoText` (+ Faz B'den gelen `Highlight`,
`HighlightText`, `InactiveSelectionHighlight`, `InactiveSelectionHighlightText`).

---

## Baskı katmanı — iki temada da AYNI değer

`Theme.PrintPreview.*` token'ları `LightTheme.xaml` ve `DarkTheme.xaml` içinde
**birebir aynı** değerlerle tanımlıdır. Kasıtlı tekrardır: yazdırılacak kâğıt ekran
temasından bağımsızdır.

| Token | Değer (her iki tema) | Rol |
|-------|----------------------|-----|
| `Theme.PrintPreview.PaperBackground` | `#FFFFFF` | Kâğıt |
| `Theme.PrintPreview.Text` | `#111827` | Gövde metni |
| `Theme.PrintPreview.MutedText` | `#4B5563` | Tarih aralığı, filtre özeti |
| `Theme.PrintPreview.Border` | `#D1D5DB` | Tablo çizgileri |
| `Theme.PrintPreview.HeaderBackground` | `#F3F4F6` | Sütun başlığı |
| `Theme.PrintPreview.AltRow` | `#F9FAFB` | Alternatif satır |
| `Theme.PrintPreview.SelectedBackground` | `#DBEAFE` | Seçili satır |
| `Theme.PrintPreview.SelectedText` | `#111827` | Seçili satır metni |
| `Theme.PrintPreview.Positive` | `#14532D` | Giriş rakamı (baskıda okunan koyu yeşil) |
| `Theme.PrintPreview.Negative` | `#7F1D1D` | Çıkış rakamı (koyu kırmızı) |

Son ikisi ekrandaki `Theme.Success` / `Theme.Danger` **değildir**. Parite testi bu
bloğu anahtar düzeyinde değil **değer** düzeyinde karşılaştırır.

---

## Ölçülüp düzeltilen değerler (2026-08-05)

Faz B'de göz kararı verilmiş, otomatik kontrast testi eklenince AA eşiğinin altında
çıkan altı token:

| Token | Eski → Yeni | Ölçüm |
|-------|-------------|-------|
| Light `Theme.DisabledBackground` | `#64748B` → `#616D7D` | 4.34 → **5.26** (beyaz metinle) |
| Light `Theme.Nav.HoverBackground` | `#3B7BC8` → `#2F6BB0` | 4.33 → **5.45** (beyaz metinle) |
| Dark `Theme.SecondaryPressed` | `#6B7C93` → `#5F7089` | 3.89 → **4.70** |
| Dark `Theme.DangerHover` | `#EF4444` → `#FA8B8B` | 4.74 → **7.4** |
| Dark `Theme.DangerPressed` | `#DC2626` → `#FCA5A5` | 3.70 → **9.41** |
| Dark `Theme.Nav.ActiveBackground` | `#3B7BC8` → `#5391D6` | 4.01 → **5.28** |

Koyu temada Danger butonu **açık** kırmızıdır ve metni koyudur; bu yüzden
hover/pressed koyulaşmaz, **açılır**. Aynı mantık Primary için de geçerlidir.

Ayrıca koyu tema `Theme.Info.*` desatüre edildi — eski `#1E3A5F` dolgu doygun bir
lacivert blok gibi duruyor ve log listesinde diğer seviyeleri bastırıyordu
(manuel test bulgusu). Yeni: `Background #1C2C3E`, `BackgroundStrong #223449`,
`Border #35506E`, `Text #A9C6E6` (8.05:1).

---

## Bilinçli istisnalar (ham hex kalan yerler)

| Yer | Neden |
|-----|-------|
| `LightTheme.xaml` / `DarkTheme.xaml` | Token tanımlarının kendisi |
| `DropShadowEffect Color=` | `Effect.Color` bir `Color`, `Brush` değil — `DynamicResource` bir `SolidColorBrush` döndürdüğü için doğrudan bağlanamaz. Çözüm: `Theme.ShadowColor` `Color` kaynağı olarak tanımlı |
| `MessageDialog.xaml.cs` başlık bandı | Dört diyalog tipini ayıran marka renkleri; yüzey değil. İki temada da doygun kalır, üzerindeki metin `Theme.OnSecondary` |
| Grafik seri renkleri | Application katmanından (`CargoDashboardChartItem.Color`) geliyor; Faz C'de `ChartPalette.xaml`'a taşınacak |
| `Common/ThemeBrush.cs` | Kaynak bulunamazsa dönen yedek değer |

Bu liste `RawColorScanTests.IsKnownException` içinde kodla da sabitlenmiştir —
listeye ekleme yapmak bir karar olmalı, kaza olmamalı.

---

## Kaldırılanlar

`AppStyles.xaml` içindeki 11 adet `Brush.*` sabit fırçası silindi
(`Brush.Primary`, `Brush.Success`, `Brush.Danger`, `Brush.Secondary`,
`Brush.Disabled`, `Brush.Border`, `Brush.GridAlt`, `Brush.GridSelected`,
`Brush.GridHover`, `Brush.TextPrimary`, `Brush.TextSecondary`).

Hiçbir referansı yoktu ve `StaticResource` oldukları için tema değişiminde
güncellenmiyorlardı — kullanılsalardı koyu temayı bozacaklardı.
