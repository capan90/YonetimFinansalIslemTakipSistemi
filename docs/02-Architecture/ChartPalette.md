# Grafik Paleti ve Kuralları

Faz C'de eklendi. Grafik motoru: **LiveChartsCore.SkiaSharpView.WPF 2.0.5**.

---

## Temel kural: grafikler DynamicResource görmez

LiveCharts SkiaSharp ile çizer, WPF fırçalarını kullanmaz. Tema sözlüğü
değiştiğinde grafik **kendiliğinden güncellenmez** — bir kez boyanan seri eski
renginde kalır. Faz B'de converter'larda çözülen sorunun aynısıdır, daha sert
biçimde.

Bu yüzden:

| Katman | Sorumluluk |
|--------|-----------|
| `Themes/{Light,Dark}Theme.xaml` | `Chart.*` renk token'ları |
| `Common/ChartPalette.cs` | Token'ları **çizim anında** okur, `SolidColorPaint`'e çevirir |
| `Services/ThemeService.cs` | Tema uygulandıktan sonra `ChartPalette.NotifyThemeChanged()` çağırır |
| Grafik barındıran pencereler | `ChartPalette.ThemeChanged`'e abone olup serilerini yeniden kurar |

Abonelik pencere kapanırken çözülür (`Unloaded`).

**Grafik kodunda hex yazılmaz.** Renk tek noktadan, `ChartPalette` üzerinden gelir.

### Palet neden tema sözlüğünde, `ChartPalette.xaml`'de değil?

Tek bir dosya iki temanın değerlerini taşıyamaz. Token'lar tema sözlüğünde
durunca hem tema takası kendiliğinden çalışır hem de Faz B'nin parite ve
kontrast testleri grafik paletini **otomatik kapsar**.

`Resources/ChartPalette.xaml` grafik **kap ve kontrol** stillerini taşır
(`Chart.Card`, `Chart.Cartesian`, `Chart.Sparkline`, `BalanceSparkline`).
Grafik **metin** stilleri (`Chart.Title`, `Chart.Caption`, `Chart.EmptyState`)
`AppStyles.xaml`'dedir: `Text.*` ölçeğine `BasedOn` ile dayanıyorlar ve `BasedOn`
kardeş sözlükler arası `StaticResource`'u güvenilir çözemiyor (`BasedOn` bir
`DependencyProperty` değil, bu yüzden `DynamicResource` de kabul etmiyor).

---

## Renk kuralları

### 1. Kategorik seriler — sırayla, en fazla üç

```
1. seri  Chart.Series1  mavi
2. seri  Chart.Series2  turuncu
3. seri  Chart.Series3  yeşil
```

Döngüye sokulmaz. **Dördüncü renk yoktur** — `ChartPalette.Categorical(3)`
hata verir. Sessizce döngüye girmek iki farklı kategoriyi aynı renkle göstermek
demektir.

Üçten fazla kategori varsa renk eklenmez, veri azaltılır:

```csharp
var data = ChartPalette.GroupSmall(items);          // en büyük 3 + "Diğer"
var data = ChartPalette.GroupSmall(items, keep: 5); // büyüklük skalasında 5'e kadar
```

Tek kalem artarsa "Diğer" yapılmaz, adıyla bırakılır — bir kalemi "Diğer" diye
göstermek bilgi kaybıdır.

### 2. Büyüklük verisi kategorik palet KULLANMAZ

"Durum Dağılımı", "Top 5 Firma" gibi tek boyutlu büyüklük verisinde renk
**kimlik değil miktar** taşır. Tek hue'nun açık→koyu adımları kullanılır
(`Chart.Sequential.1..5`), veri çoktan aza sıralanır.

Skalanın açık ucu yüzeyle düşük kontrastlıdır (açık temada 1.5:1) — bu bir hata
değil, skalanın doğasıdır: büyüklüğü zaten **bar uzunluğu** taşır. Görünürlük
`Chart.Sequential.Stroke` kenarlığıyla sağlanır; kenarlığın eşiği tutması
zorunludur ve test edilir.

### 3. Yön verisinde kırmızı/yeşil yasak

Borç/alacak, giriş/çıkış gibi yön gösteren veride **mavi–turuncu** kullanılır
(`Chart.Direction.Inflow` / `Outflow`). Kırmızı–yeşil, renk körlüğünün en zayıf
eksenidir.

### 4. Legend

İki ve daha fazla seride legend **zorunlu**. Tek serili büyüklük grafiklerinde
kapatılır (kategori adları zaten eksende yazılı).

### 5. Renk tek anlam taşıyıcı olamaz

Kategorik renkler **hue** ile ayrışır, parlaklıkla değil — birbirlerine karşı
kontrastları ~1.1–1.4:1'dir ve **gri baskıda ayrışmazlar**. Bu kategorik
paletlerin bilinen sınırıdır. Bu yüzden her grafiğin altında ne anlattığını
yazan bir açıklama satırı bulunur (`Chart.Caption`) ve legend zorunludur.

---

## Ölçülen değerler

Eşik: grafik işaretleri için **3:1** (WCAG non-text contrast), kart yüzeyine karşı.

| Token | Light | ölçüm | Dark | ölçüm |
|-------|-------|-------|------|-------|
| `Chart.Series1` (mavi) | `#2A78D6` | 4.42 | `#5B9EE8` | 5.23 |
| `Chart.Series2` (turuncu) | `#EB6834` | 3.20 | `#F2894F` | 5.90 |
| `Chart.Series3` (yeşil) | `#128F63` | 4.09 | `#35C994` | 6.92 |
| `Chart.Sequential.Stroke` | `#475569` | 7.58 | `#CBD5E1` | 9.85 |

**Yeşil brief'teki `#1BAF7A`'dan koyulaştırıldı**: o değer beyaz yüzeyde 2.82:1
veriyordu, eşiğin altındaydı ve ince çizgi serilerinde kayboluyordu.

**Koyu tema açık temanın otomatik tersi DEĞİL.** Aynı üç hue koyu yüzey
(`#1E293B`) üzerinde ayrı ölçüldü. Açık tema değerleriyle koyu zeminde mavi
3.31'e düşüyordu; üçü birlikte dengelendi ki hiçbir seri diğerini bastırmasın.

---

## Ekranlar

| Ekran | Grafik | Veri türü | Renk |
|-------|--------|-----------|------|
| Finans Analiz Merkezi | Günlük Trend (çizgi/alan) | yön + kümülatif | mavi / turuncu / yeşil |
| Kargo Dashboard | Gelen / Giden | kategorik (yön) | mavi–turuncu |
| Kargo Dashboard | Durum Dağılımı | büyüklük | tek hue skala |
| Kargo Dashboard | Top 5 Kargo Firması | büyüklük | tek hue skala |
| Nakit İşlemler | Bakiye kartı sparkline | tek seri | `Chart.Sparkline` |

Analiz ekranındaki **tablo silinmedi**: grafiğin altında varsayılan kapalı bir
`Expander` içinde duruyor. Rakamı okumak isteyen açar, hiçbir veri kaybolmaz.

### Bilinen sınır: sparkline filtreye tabidir

Son 30 günün para birimi bazında bakiye serisi Application katmanında **yok**.
Sparkline, ekrandaki işlem satırlarının taşıdığı `*BalanceAfter` alanlarından
türetilir — yani liste filtrelenirse eğilim de filtrelenir. Bu sessizce olmasın
diye kartların altında kapsam yazılır ("Son 30 gün · filtrelenmiş görünüm").

Filtreden bağımsız gerçek seri için Application katmanında yeni bir sorgu gerekir.

### Bilinen sınır: sayaç kartlarının yalnızca ikisi tıklanabilir

Kargo dashboard'daki altı karttan yalnızca "Bugün Gelen" ve "Bugün Giden"
tıklanabilir. Sebep doğruluk: rapor filtresi yalnızca bu ikisinin saydığı kümeyi
birebir üretebiliyor. Diğerleri (Toplam Bekleyen, Bildirim Bekleyen, Acil
Bekleyen, Bugün Teslim) mevcut filtre alanlarıyla ifade edilemiyor; tıklanabilir
yapmak karttaki sayıdan **farklı** bir liste açardı ve rakama olan güveni bozardı.

---

## ClickOnce

SkiaSharp **native** bileşen taşır (`libSkiaSharp`, `libHarfBuzzSharp`) ve
bunlar `runtimes/<rid>/native/` alt klasörlerinde durur. ClickOnce
paketlemesinde en sık atlanan yer burasıdır: yönetilen dll'ler pakete girer,
native dosyalar girmez ve uygulama yalnızca **müşteride**, grafik ilk çizilirken
patlar.

`Publish-ClickOnce.ps1` çıktısı doğrulandı — manifest `win-x86`, `win-x64` ve
`win-arm64` native dosyalarını içeriyor.

Regresyon koruması: `UiTests/ChartRuntimeTests.cs` native yüklemeyi build
çıktısı üzerinde doğrular. Bu test aynı zamanda `NU1701` bastırmasının
meşruiyet dayanağıdır (bkz. UI csproj).
