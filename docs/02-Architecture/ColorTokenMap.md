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

## Bilinçli istisnalar (ham hex kalan yerler)

| Yer | Neden |
|-----|-------|
| `LightTheme.xaml` / `DarkTheme.xaml` | Token tanımlarının kendisi |
| `DropShadowEffect Color=` | `Effect.Color` bir `Color`, `Brush` değil — `DynamicResource` bir `SolidColorBrush` döndürdüğü için doğrudan bağlanamaz |
| Grafik seri renkleri | Application katmanından (`CargoDashboardChartItem.Color`) geliyor; Faz C'de `ChartPalette.xaml`'a taşınacak |

---

## Kaldırılanlar

`AppStyles.xaml` içindeki 11 adet `Brush.*` sabit fırçası silindi
(`Brush.Primary`, `Brush.Success`, `Brush.Danger`, `Brush.Secondary`,
`Brush.Disabled`, `Brush.Border`, `Brush.GridAlt`, `Brush.GridSelected`,
`Brush.GridHover`, `Brush.TextPrimary`, `Brush.TextSecondary`).

Hiçbir referansı yoktu ve `StaticResource` oldukları için tema değişiminde
güncellenmiyorlardı — kullanılsalardı koyu temayı bozacaklardı.
