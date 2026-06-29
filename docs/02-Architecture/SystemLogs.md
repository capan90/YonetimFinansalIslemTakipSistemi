# Sistem Log ve Hata Bildirimleri

## Audit Log vs Sistem Log

| Özellik | Audit Log | Sistem Log |
|---------|-----------|------------|
| Amaç | İş akışı denetimi | Teknik hata ve olay izleme |
| Kapsam | Kritik iş eylemleri | Uygulama hataları, uyarılar, bilgi |
| Tetikleyen | Handler'lar (kasıtlı) | Exception handler'lar, servisler |
| Tablo | `audit_logs` | `system_logs` |
| UI | AuditLogWindow | SistemLogWindow |
| Mail | Hayır | Kritik seviyede |

---

## Sistem Log Seviyeleri

| Seviye | Kullanım | Mail Gönderimi |
|--------|----------|---------------|
| Info | Bilgilendirici olaylar | Hayır |
| Warning | Önemsiz ama dikkat gerektiren durumlar | Hayır |
| Error | Beklenmeyen hatalar, işlem başarısızlıkları | Hayır |
| Critical | Sistem başlamıyor, DB erişilemiyor, kritik veri hatası | **Evet** |

---

## ISystemLogService

```csharp
public interface ISystemLogService
{
    Task LogInfoAsync(string category, string message, string? source = null);
    Task LogWarningAsync(string category, string message, string? source = null);
    Task LogErrorAsync(string category, string message, Exception? ex = null, string? source = null);
    Task LogCriticalAsync(string category, string message, Exception? ex = null, string? source = null);
}
```

Implementasyon `Infrastructure` katmanındadır. `SystemLog` entity'sine yazılır.

---

## SMTP Mail Bildirimi

Kritik hatalarda `SmtpErrorNotificationService` otomatik mail gönderir.

**Tetiklenme koşulu:** Yalnızca `LogCriticalAsync` çağrıldığında.

**Cooldown mekanizması:** Aynı kategori/mesaj için belirli bir süre içinde birden fazla mail gönderilmez. Bu, hatalı döngülerde spam mail oluşmasını engeller.

**SMTP ayarları:**
- `ApplicationSettings` tablosunda `SMTP:*` anahtarları ile saklanır
- Değerler AES-256 ile şifrelenir
- Env var override desteği

---

## Serilog Dosya Loglama

Sistem log (DB) ile paralel çalışan, dosya tabanlı log sistemi. `ISystemLogService` çağrılarından bağımsız olarak global exception handler'lar tarafından kullanılır.

**Log dosyası:** `<kurulum-dizini>\logs\app-YYYYMMDD.log`  
**Rolling:** Günlük

Hata satırları `[ERR]` veya `[FTL]` ile başlar.

---

## Global Exception Handling

`App.xaml.cs`'te kayıt edilir:

```csharp
DispatcherUnhandledException     // WPF UI thread hataları
AppDomain.UnhandledException     // Genel CLR hataları
TaskScheduler.UnobservedTaskException  // async void'daki yakalanmayan hatalar
```

Her handler:
1. Serilog'a yazar.
2. `ISystemLogService.LogCriticalAsync()` çağırır → DB + Mail.
3. Kullanıcıya anlaşılır hata dialog'u gösterir.

---

## Sistem Log İzleme Ekranı

`SystemLogWindow` (yönetici erişimli):
- Seviye, kategori, tarih filtresi
- Exception stack trace görüntüleme
- Son N kayıt listeleme

---

## Sağlık İzleme Ekranı

`HealthCheckWindow` (yönetici erişimli):
- Veritabanı bağlantı durumu
- Migration durumu (pending migration var mı)
- Log klasörü durumu
- Backup klasörü durumu
- version.json kontrolü
- SMTP bildirim durumu
- Genel sağlık puanı
