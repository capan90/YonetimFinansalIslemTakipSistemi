# WhatsApp Bildirim Modülü

## Genel Bakış

Kargo sevkiyatları için hazır WhatsApp bildirim metni üretir. Doğrudan mesaj göndermez — metin kopyalanarak WhatsApp'tan manuel gönderilir.

---

## Akış

1. Kargo listesinde kargo seçilir.
2. Operasyon Merkezi → "WhatsApp" butonu.
3. `GenerateCargoNotificationHandler`: bildirim modeli oluşturulur.
4. `CargoNotificationPreviewWindow` (WhatsApp modu): mesaj önizlenir.
5. "Kopyala" → metin panoya alınır.
6. WhatsApp Web veya mobil uygulamada yapıştırılır ve gönderilir.
7. `MarkCargoNotificationPreparedHandler`: `NotificationStatus → WhatsApp Hazır`, `CargoWhatsAppPrepared` audit.

---

## WhatsApp Notification Composer

Bildirim içeriği şablon tabanlı üretilir:

```
Sayın [Firma],

[ShipmentNumber] numaralı kargunuz [TrackingNumber] takip numarası ile yola çıkmıştır.
Takip linki: [TrackingUrl]

Saygılarımızla.
```

---

## Durum Takibi

`CargoNotificationStatus.WhatsAppHazir` atandıktan sonra bildirim durumu liste ekranında renkli gösterilir.

---

## Ortak WhatsApp Rehberi (2026-07-22)

Tüm kullanıcıların ortak kullandığı merkezi alıcı rehberi. Kullanıcı bazlı değildir;
ayrı permission yoktur — oturum açan kullanıcılar görüntüleyip yönetebilir.

- **Entity:** `WhatsAppContact` (`whatsapp_contacts`) — Ad Soyad, Telefon, Firma,
  Açıklama, Aktif/Pasif + standart audit/soft delete alanları.
- **Telefon normalizasyonu:** `PhoneNumberNormalizer.NormalizeTr` —
  `0532 123 45 67`, `5321234567`, `+90 532...`, `0090 532...` yazımlarının tümü
  `+905321234567` olarak saklanır. TR mobil (5xx) doğrulaması yapılır.
- **Mükerrer koruması:** Application seviyesinde kontrol + DB'de filtresiz unique index
  (soft delete edilmiş kayıt da numarayı rezerve eder). Aynı numara yeniden eklenmek
  istenirse silinmiş kayıt geri yüklenir; aktif kayıt varsa kullanıcıya
  "Bu telefon numarası WhatsApp rehberinde zaten kayıtlıdır. (Kayıt: ...)" uyarısı gösterilir.
- **Yönetim ekranı:** Kargo Takip → WhatsApp Rehberi. Arama (ad/telefon/firma),
  firma filtresi, "Pasifleri göster", çift tıkla düzenleme.
- **Gönder ekranı entegrasyonu:** `CargoNotificationPreviewWindow` (WhatsApp modu) —
  aranabilir çoklu seçim listesi (`WhatsAppContactSearch` ile "m/mu/mur" kısmi eşleşme),
  seçilen kişiler chip olarak gösterilir ve çıkarılabilir, `+` butonu hızlı ekleme yapar
  (kayıt rehbere yazılır, liste yenilenir, kişi otomatik seçilir).
- **Salt okunur telefon:** Rehberden kişi seçiliyken telefon alanı düzenlenemez;
  düzeltme WhatsApp Rehberi ekranından yapılır. Hiç kişi seçilmemişse manuel numara
  girişi (eski akış) çalışmaya devam eder.
- **Toplu gönderim:** wa.me toplu gönderimi desteklemediği için her kişi için ayrı
  WhatsApp Web açılışı yapılır; başarılı/başarısız alıcılar kullanıcıya raporlanır ve
  işlenen alıcılar `CargoWhatsAppPrepared` audit kaydına yazılır.

---

## Entegrasyon Durumu

WhatsApp resmi API entegrasyonu V2 planında. V1'de sadece metin kopyalama akışı aktiftir.
