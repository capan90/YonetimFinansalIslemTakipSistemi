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

## Entegrasyon Durumu

WhatsApp resmi API entegrasyonu V2 planında. V1'de sadece metin kopyalama akışı aktiftir.
