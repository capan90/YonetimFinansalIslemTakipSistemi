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

**Durum:** Gönderim aktiftir (`CargoSmtpMailSenderService`, SMTP ayarları `application_settings` tablosundan okunur). Başarılı gönderimde bildirim durumu otomatik `Mail Hazır` olur.

### Çoklu Alıcı ve Adres Doğrulama (2026-08-04)

- **Alıcı ve CC alanları birden fazla adres kabul eder.** Ayraç: `;`, `,` veya boşluk.
- Ayrıştırma/normalize/doğrulama tek noktadadır: `EmailAddressHelper` (Application/Common).
  UI, mail rehberi ve SMTP gönderici aynı kuralı kullanır.
- Adresler küçük harfe normalize edilir ve mükerrerler elenir.
- `ICargoMailSenderService.SendAsync` artık `IReadOnlyCollection<string> to/cc` alır;
  her adres ayrı `MailAddress` olarak eklenir (tek string'i `;` ile geçmek bazı SMTP
  sunucularında sessizce tek alıcıya düşüyordu).
- Geçersiz adres yazıldığında gönder butonu pasifleşir ve **hangi adresin hatalı olduğu**
  alanın altında gösterilir.

---

## Mail Rehberi (Ortak) — 2026-08-04

Kargo bildirim maillerinde alıcı/CC adreslerinin her seferinde elle yazılmasını önler.
WhatsApp rehberinin kardeşidir; aynı kuralları izler.

**Entity:** `MailContact` → `mail_contacts` tablosu

| Alan | Açıklama |
|------|----------|
| FullName | Ad Soyad / Kayıt Adı (zorunlu) |
| Email | Normalize (küçük harf) e-posta — unique index |
| Company | Firma (opsiyonel) |
| Description | Açıklama (opsiyonel) |
| IsDefaultCc | true ise mail ekranı açılışında CC'ye otomatik eklenir |
| LastUsedAt | Son başarılı gönderimde kullanıldığı an — liste sıralaması |
| IsActive | Pasif kayıtlar seçim listelerinde görünmez |

**Kurallar:**
- Unique index filtresizdir: soft delete edilmiş kayıt da adresi rezerve eder. Aynı adres
  yeniden eklenmek istendiğinde `CreateMailContactHandler` kaydı **geri yükler**
  (WhatsApp rehberiyle aynı davranış).
- Okuma tüm oturumlu kullanıcılara açıktır. Yazma yetkisi `MailContactPermissions.CanModify`:
  `CanManageIncomingCargo` **veya** `CanManageOutgoingCargo` **veya** `CanManageCompanyDirectory`.
- Liste sıralaması: son kullanılan önce, hiç kullanılmamışlar ada göre arkada.

**Ekranlar:**

| Ekran | Yol |
|-------|-----|
| Mail Rehberi (yönetim) | Kargo Takip → Mail Rehberi |
| Rehberden seçim | Mail Hazırla → Alıcı/CC yanındaki "📇 Rehber" butonu |
| Hızlı ekleme | Seçim penceresindeki "+ Yeni Kişi" |

**Akış:**
1. Mail Hazırla açılır → Alıcı firma e-postasıyla, CC "Varsayılan CC" işaretli kişilerle dolar.
2. "📇 Rehber" ile çoklu seçim yapılır; seçilenler mevcut metnin **üzerine eklenir**
   (elle yazılmış adresler kaybolmaz).
3. Gönderim başarılı olursa kullanılan adreslerin `LastUsedAt` değeri tazelenir
   (`TouchMailContactsHandler`) — sık kullanılanlar listede üste çıkar.

**Audit:** `MailContactCreated`, `MailContactUpdated`, `MailContactDeleted`.
Gönderim başına adres bazlı audit yazılmaz; gönderimin kendisi `CargoMailPrepared`
olarak zaten denetlenir.

---

## Mail Preview Deadlock Sorunu (Çözüldü)

**Problem:** Kargo mail önizleme ekranı açılırken WPF UI donuyordu.

**Root Cause:** `async Task` metodunda `.Result` veya `.Wait()` çağrısı WPF UI thread sync context'inde deadlock yarattı.

**Çözüm:** Tüm çağrı zinciri `async/await` ile yeniden yazıldı.

Bkz. [`docs/04-Development/LessonsLearned.md`](../04-Development/LessonsLearned.md)

---

## SMTP Tanılama

Ayarlar ekranında SMTP bağlantı testi yapılabilir. Sağlık izleme ekranında SMTP durumu görüntülenir.
