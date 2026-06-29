# Mail Sistemi

## İki Ayrı Mail Akışı

| Akış | Amaç | Servis |
|------|------|--------|
| Sistem hata bildirimleri | Kritik hataları yöneticiye iletmek | SmtpErrorNotificationService |
| Kargo bildirim maili | Kargo alıcısını bilgilendirmek | MailNotificationComposer |

---

## Sistem Hata Bildirimleri (SMTP)

`SmtpErrorNotificationService`:
- `ISystemLogService.LogCriticalAsync()` çağrıldığında tetiklenir.
- SMTP üzerinden yönetici e-postasına gönderilir.

**Cooldown:** Aynı kategori/mesaj kombinasyonu için belirli bir süre içinde birden fazla mail gönderilmez.

**SMTP Ayarları (ApplicationSettings tablosu):**

| Anahtar | Açıklama |
|---------|----------|
| SMTP:Host | SMTP sunucu adresi |
| SMTP:Port | Port (genellikle 587 veya 465) |
| SMTP:Username | SMTP kullanıcı adı |
| SMTP:Password | AES-256 şifreli parola |
| SMTP:FromEmail | Gönderici adresi |
| SMTP:ToEmail | Alıcı (yönetici) adresi |
| SMTP:EnableSsl | SSL/TLS aktif mi |

Değerler `appsettings.json` veya env var ile override edilebilir.

---

## Kargo Bildirim Maili

`MailNotificationComposer` — `INotificationComposer` implementasyonu:
- `Compose(model)` → mail body (HTML)
- `ComposeSubject(model)` → mail konusu

`CargoNotificationModel` alanları:
- `TargetEmail`: alıcı e-postası
- `Subject`: konu satırı
- `Body`: hazırlanan içerik

**Önemli:** `ReceiverEmailSnapshot` — bildirim hazırlandığı andaki e-posta adresi kargo kaydına kopyalanır. Firma e-postası sonradan değişse bile gönderilen adres değişmez.

**Durum:** Mail önizlemesi aktif, gerçek gönderim V2'de aktif edilecek. UI'de "Mail Gönder" butonu `IsEnabled="False"` olarak gösterilir.

---

## Mail Preview Deadlock Sorunu (Çözüldü)

**Problem:** Kargo mail önizleme ekranı açılırken WPF UI donuyordu.

**Root Cause:** `async Task` metodunda `.Result` veya `.Wait()` çağrısı WPF UI thread sync context'inde deadlock yarattı.

**Çözüm:** Tüm çağrı zinciri `async/await` ile yeniden yazıldı.

Bkz. [`docs/04-Development/LessonsLearned.md`](../04-Development/LessonsLearned.md)

---

## SMTP Tanılama

Ayarlar ekranında SMTP bağlantı testi yapılabilir. Sağlık izleme ekranında SMTP durumu görüntülenir.
