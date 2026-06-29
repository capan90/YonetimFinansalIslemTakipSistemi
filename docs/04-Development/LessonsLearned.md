# Öğrenilen Dersler

Projede yaşanan büyük problemler, kök nedenleri ve çözümleri.

---

## 1. Async-Over-Sync Deadlock (Mail Preview Dondurma)

**Problem:** Kargo Mail Preview ekranı açılırken WPF UI tamamen donuyordu. Hiçbir kullanıcı etkileşimi çalışmıyordu.

**Root Cause:**  
WPF UI thread'inde `async Task` metodunu `.Result` veya `.Wait()` ile senkron çağırmak deadlock yaratır.

```csharp
// YANLIŞ — deadlock
var result = SomeAsyncMethod().Result;  // UI thread'i bloke eder
// Aynı zamanda SomeAsyncMethod() UI thread'ine dönmeye çalışır → çıkmaz
```

WPF'in `SynchronizationContext`'i, devam (continuation) kodunun UI thread'inde çalışmasını bekler. `.Result` ile UI thread bloke edilince, devam kodu hiçbir zaman çalışamaz → sonsuz bekleme.

**Çözüm:** Tüm async zinciri `async/await` ile yeniden yazıldı. `.Result` ve `.Wait()` kaldırıldı.

```csharp
// DOĞRU
var result = await SomeAsyncMethod();
```

**Kural:** `async/await` all the way down. Hiçbir noktada zinciri senkronize etme.

**Commit:** `8a94daf`

---

## 2. ClickOnce Masaüstü Kısayol İkonu

**Problem:** ClickOnce ile kurulan uygulamanın masaüstü kısayolu varsayılan Windows ikonunu gösteriyordu.

**Araştırma ve Denenen Yaklaşımlar:**

| Yaklaşım | Sonuç |
|----------|-------|
| `dotnet-mage -New Deployment -IconFile` | "can only be used with Application type" hatası |
| `dotnet-mage -New Application -IconFile C:\mutlak\yol` | Manifest'e mutlak yol yazıldı → runtime "hatalı biçimlendirilmiş" |
| Deployment manifest XML post-processing (`asmv2:iconFile`) | Runtime hatası: "Dağıtım bildirimi simge dosyasının belirtimini kabul etmez." |

**Root Cause:**  
ClickOnce runtime bu özelliği desteklemiyor. `asmv2:iconFile` attribute'u deployment manifest'te kabul edilmiyor. Application manifest'te ise mutlak yol geçersiz.

**Karar:** Won't Fix.

**Durum:** EXE ikonu ✅, taskbar ikonu ✅, pencere ikonu ✅, masaüstü kısayol ikonu ❌

---

## 3. ClickOnce Türkçe Karakter Encoding (Mojibake)

**Problem:** ClickOnce kurulumda "Uygulama hatalı biçimlendirilmiş" hatası alındı.

**Root Cause:**  
`Publish-ClickOnce.ps1`'deki `$AppName = "Yönetim Finansal İşlem Takip Sistemi"` değişkeni `dotnet-mage`'e geçirilirken PowerShell console encoding uyumsuzluğu nedeniyle mojibake oluştu:

```
YÃ¶netim Finansal Ä°ÅŸlem Takip Sistemi
```

ClickOnce manifest XML'de geçersiz encoding → "hatalı biçimlendirilmiş".

**Çözüm:**
```powershell
# ASCII-only — Türkçe karakter yok
$AppName = "Yonetim Finansal Islem Takip Sistemi"
```

Tüm `dotnet-mage` parametre değerleri ASCII-only yapıldı.

**Ek Önlem:**
- Publish öncesi publish klasörü temizleniyor (bayat manifest kalma riski).
- Doğrulama bloğu: deployment manifest'te `[^\x00-\x7F]` regex ile non-ASCII kontrolü.

**Kural:** `dotnet-mage`'e asla Türkçe veya özel karakter geçirme.

---

## 4. Koyu Tema WPF DynamicResource Sorunları

**Problem:** Koyu tema uygulandığında bazı WPF kontrolleri düzgün güncellenmedi; renk tutarsızlıkları, hover durumları hatalı.

**Root Cause:**  
WPF kontrolleri (özellikle DataGrid, ComboBox, TextBox) genellikle ControlTemplate'in iç kısımlarını da özelleştirmeyi gerektirir. `DynamicResource` kök değerleri değiştirir ama template'in derinliklerindeki hard-coded renklere ulaşamaz.

**Karar:** Koyu tema `DarkTheme.xaml` olarak hazır ama devre dışı bırakıldı. `AppearanceSettingsWindow`'da "Koyu Tema (Yakında)" olarak gösterilir.

**Not:** Bu bir teknik borç; gelecekte tam WPF tema desteği için tüm ControlTemplate'lerin override edilmesi gerekecek.

---

## 5. DataGrid Seçili Satır Görünürlüğü

**Problem:** Custom tema ile DataGrid'in seçili satırı görünmüyordu (koyu mavi üzerine koyu metin gibi).

**Root Cause:**  
WPF DataGrid seçili satır rengi ve metin rengi ayrı ayrı kontrol edilir. Genel tema rengi değiştiğinde seçim rengi güncellenmez; özel stil gereklidir.

**Çözüm:** Açık tema DataGrid stiline explicit `SelectionBackground` ve `SelectionForeground` eklendi.

**Commit:** `31b3ffe`

---

## 6. Application Namespace Çakışması

**Problem:**  
```
Hata CS0234: 'Application' içinde 'Current' adında bir tür veya ad alanı bulunamadı.
```

**Root Cause:**  
`using System.Windows;` ve proje namespace'i `YonetimFinansalIslemTakipSistemi.Application` aynı anda bulunduğunda `Application` kelimesi belirsizleşir.

**Çözüm:** Tam niteleme kullanılır:
```csharp
System.Windows.Application.Current.Resources...
```

---

## 7. SMTP Cooldown — Spam Mail Sorunu

**Problem:** Sistemde tekrarlayan bir hata oluştuğunda her hata döngüsünde mail gönderildi; yönetici kutusuna yüzlerce mail düştü.

**Root Cause:**  
Global exception handler her çağrısında mail gönderiyordu; cooldown mekanizması yoktu.

**Çözüm:**  
`SmtpErrorNotificationService`'e cooldown mekanizması eklendi. Aynı kategori+mesaj kombinasyonu için belirli süre içinde yalnızca bir mail gönderilir.

---

## 8. AES Şifreleme — Ayar Güvenliği

**Gereksinim:** SMTP şifresi ve diğer hassas ayarlar veritabanında plain-text saklanamaz.

**Çözüm:**  
`AesEncryptionService` ile AES-256 şifreleme. Değerler `ApplicationSettings` tablosunda şifreli saklanır. Uygulama okurken çözer.

**AES Secret Key:** Şifreleme anahtarı uygulama içinde güvenli şekilde saklanır; `appsettings.json`'a veya DB'ye yazılmaz.

---

## 9. dotnet publish ClickOnce Kısıtı

**Problem:** `dotnet publish /p:PublishProfile=ClickOnce` çalışmadı.

**Hata:** MSB3964 — `Engine\Launcher.exe` dosyası bulunamadı.

**Root Cause:**  
.NET 9 CLI ClickOnce publish için `GenerateLauncher` MSBuild görevini kullanır. Bu görev `Engine\Launcher.exe` şablon dosyasına ihtiyaç duyar. Bu dosya yalnızca Visual Studio kurulumunda bulunur.

**Çözüm:**  
`microsoft.dotnet.mage` global tool ile `Publish-ClickOnce.ps1` scripti. Visual Studio gerekmez.

---

## 10. EF Core DbContext Lifetime (Scoped Kural)

**Problem:** Singleton servislerde `AppDbContext` doğrudan inject edildiğinde "A second operation was started on this context" veya stale data hataları.

**Root Cause:**  
`AppDbContext` Scoped lifetime'dır. Singleton servise inject edilirse uygulamanın ömrü boyunca tek instance kullanılır — bu EF Core için hatalı.

**Çözüm:**  
Singleton servislerde `IServiceScopeFactory` kullanılır:
```csharp
using var scope = _scopeFactory.CreateScope();
var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```

`MainWindow.Closed` olayında scope dispose edilir — DbContext pencere ömrünü aşmaz.
