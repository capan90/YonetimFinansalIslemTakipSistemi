# Kargo Katip Modülü

## Genel Bakış

Gelen ve giden kargo sevkiyatlarının yönetimi, etiket üretimi, bildirim hazırlama ve operasyon takibi.

---

## Temel Entity'ler

### CompanyDirectory (Firma Rehberi)
Gönderici veya alıcı firmalar.

| Alan | Açıklama |
|------|----------|
| Name | Firma adı (zorunlu) |
| ContactPerson | Dikkatine / iletişim kişisi |
| Address | Adres |
| Phone | Telefon |
| Email | E-posta |

### CargoCompany (Kargo Firmaları)
Kullanılan nakliyat firmaları.

| Alan | Açıklama |
|------|----------|
| Name | Kargo firması adı |
| TrackingUrlTemplate | Takip URL şablonu; `{0}` takip nosuyla değiştirilir |

### CargoShipment (Sevkiyat)
Her gelen veya giden kargo kaydı.

| Alan | Açıklama |
|------|----------|
| ShipmentNumber | Otomatik: G-2026-0001 (giden), C-2026-0001 (gelen) |
| Direction | Giden=1, Gelen=2 |
| ShipmentType | CargoShipmentType enum |
| Status | CargoShipmentStatus enum |
| NotificationStatus | CargoNotificationStatus enum |
| CargoCompanyId | Kargo firması |
| CompanyDirectoryId | Giden: alıcı firma / Gelen: gönderen firma |
| TrackingNumber | Takip numarası |
| VehiclePlate | Araç plakası |
| ShipmentDate | Gönderim tarihi |
| DeliveryDate | Teslimat tarihi |
| Notes | Notlar |
| ReceiverEmailSnapshot | Bildirim anındaki e-posta kopyası |

---

## Kargo Numarası Otomasyonu

- Giden: `G-YYYY-0001` formatı, yılın ilk kaydından itibaren artan
- Gelen: `C-YYYY-0001` formatı
- `GetNextShipmentNumberAsync`: silinen kayıtlar dahil (`IgnoreQueryFilters`) max+1
- Unique index: `IX_cargo_shipments_ShipmentNumber` (partial: IS NOT NULL)
- Oluşturma sırasında ShipmentNumber boşsa otomatik üretilir

---

## Durum Yönetimi

### CargoShipmentStatus
```
1 = Beklemede
2 = Yolda
3 = Teslim Edildi
4 = İptal Edildi
```

### Durum Geçiş Kuralları

`CargoStatusTransitions` (Application katmanı) geçerli geçişleri tanımlar:

```
Beklemede → Yolda, İptal Edildi
Yolda     → Teslim Edildi, Beklemede, İptal Edildi
Teslim Edildi → (final)
İptal Edildi  → (final)
```

`UpdateCargoShipmentHandler` geçersiz geçişte `OperationResult.Fail` döner. Edit VM'de yalnızca izinli geçişler `AllowedStatusOptions` olarak gösterilir.

---

## Bildirim Durumu Yönetimi

### CargoNotificationStatus
```
1 = Bildirilmedi
2 = WhatsApp Hazır
3 = Mail Hazır
4 = Bildirildi
```

Bildirim hazırlandığında status güncellenir; `MarkCargoNotificationPreparedHandler` ilgili audit'i yazar.

---

## Operasyon Merkezi

`CargoOperationCenterWindow` — seçili kargo için tüm operasyonlar tek ekranda:

| Kart | Eylem |
|------|-------|
| Etiket | PDF etiket oluştur ve önizle |
| WhatsApp | WhatsApp bildirim metni hazırla |
| Mail | Mail bildirim içeriği hazırla |
| Takip | Takip linkini aç |
| Durum | Durum güncelle |

Kargo listesindeki "Operasyon" butonu bu ekranı açar.

---

## Dikkatine (Attention Contact) Sistemi

Kargo etiketi ve bildirim mesajlarında "Dikkatine: [Kişi Adı]" bilgisi kullanılır.

Bu bilgi `CompanyDirectory.ContactPerson` alanından gelir. Firma seçildiğinde gönderim ekranında otomatik dolar (5 firma kartı prop: DirectoryFirma, Contact, Address, Phone, Email + HasXxx).

Etiket PDF'de "DİKKATİNE: [ContactPerson]" satırı öne çıkarılır (font boyutu artırılmış).

---

## Kargo Etiketi PDF

`QuestPdfLabelRenderer` ile üretilir.

**Etiket içeriği:**
- Kargo firması
- Takip numarası (büyük font)
- QR kod placeholder ("▦▦▦/QR KOD")
- Barkod placeholder ("KARGO BARKODU" + simüle çizgiler)
- Dikkatine bilgisi
- Alıcı firma adı ve adresi
- Gönderim tarihi

---

## WhatsApp Bildirim Hazırlama

`CargoNotificationPreviewWindow` (WhatsApp modu):
1. Mesaj şablonu hazırlanır (WhatsApp notificationComposer).
2. Önizleme gösterilir.
3. "Kopyala" ile mesaj panoya alınır.
4. WhatsApp uygulamasında yapıştırılır.

Status → `WhatsApp Hazır` olarak güncellenir.

---

## Mail Bildirim Hazırlama

`MailNotificationComposer` → `CargoNotificationModel`:
- `TargetEmail`: `CompanyDirectory.Email` veya manuel giriş
- `Subject`: Kargo numarası + açıklama
- `Body`: HTML formatında bildirim içeriği

`CargoNotificationPreviewWindow` (Mail modu):
1. Konu ve içerik önizlenir.
2. "Mail Gönder" butonu — şu an için hazır değil, V2'de aktif edilecek.
3. Status → `Mail Hazır` olarak güncellenir.

**Not:** Bildirim anında `ReceiverEmailSnapshot` kaydedilir — firma e-postası sonradan değişse bile gönderilen adres korunur.

---

## Takip URL

`CargoCompany.TrackingUrlTemplate` içindeki `{0}` takip numarasıyla değiştirilir.

```
Örnek: "https://kargom.com/track?no={0}"
→ "https://kargom.com/track?no=TRK123456"
```

Liste ekranında DataGridTemplateColumn / Hyperlink ile tıklanabilir; `Process.Start(UseShellExecute=true)` ile varsayılan tarayıcıda açılır.

---

## Arama ve Filtreleme

Liste ekranında arama türü ComboBox:

| Arama Türü | Kapsam |
|------------|--------|
| Genel | Tüm alanlar |
| Firma | CompanyDirectory.Name |
| Kargo No | ShipmentNumber |
| Takip No | TrackingNumber |
| Araç Plakası | VehiclePlate |

Status filtresi ComboBox → seçim anında liste yenilenir.

---

## Kargo Dashboard

Gelen / giden kargo sayıları, durum dağılımı, son sevkiyatlar.

---

## Kargo Raporu ve PDF Export

- Tarih filtreli kargo raporu
- PDF export ile kargo listesi çıktısı

---

## İzin Matrisi

| Ekran | Gerekli İzin |
|-------|-------------|
| Kargo menüsünü görme | CanViewCargoModule |
| Gelen kargo listesi | CanViewIncomingCargo |
| Gelen kargo CRUD | CanManageIncomingCargo |
| Giden kargo listesi | CanViewOutgoingCargo |
| Giden kargo CRUD | CanManageOutgoingCargo |
| Firma rehberi CRUD | CanManageCompanyDirectory |
| Kargo firmaları CRUD | CanManageCargoCompanies |

---

## 6 Aylık Retention

`CargoRetentionService` (Singleton, `IServiceScopeFactory` ile scoped erişim):
- 6 aydan eski ve "Teslim Edildi" veya "İptal Edildi" durumundaki kargo kayıtları arşivlenir/temizlenir.
- Background service olarak çalışır; kullanıcı işlemini engellemez.

---

## Audit Aksiyonları (Kargo)

```
CompanyDirectoryCreated, CompanyDirectoryUpdated, CompanyDirectoryDeleted
CargoCompanyCreated, CargoCompanyUpdated, CargoCompanyDeleted
CargoShipmentCreated, CargoShipmentUpdated, CargoShipmentDeleted
CargoWhatsAppPrepared, CargoMailPrepared
```
