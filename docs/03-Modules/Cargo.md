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
| ShipmentNumber | Otomatik: GDN00001 (giden), GLN00001 (gelen) — salt okunur |
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

## Kargo Numarası Otomasyonu (2026-07-22'de revize edildi)

- Giden: `GDN00001`, Gelen: `GLN00001` — yönler bağımsız sayaç kullanır
- Sayaç: `cargo_number_counters` tablosu (yön başına satır); artış
  `INSERT ... ON CONFLICT DO UPDATE ... RETURNING` ile atomik, kargo insert'iyle
  aynı transaction'da (`AddWithAutoNumberAsync`) — eşzamanlı mükerrer imkânsız,
  rollback'te numara boşa gitmez
- Numara kullanıcı tarafından girilemez/değiştirilemez; UI'da salt okunur
- Silme davranışı: **aradaki** silinmiş numaralar asla yeniden kullanılmaz; yalnızca
  yönün **en son** numarası silinirse (daha yüksek kayıt yoksa) sayaç aynı transaction'da
  bir geri alınır ve numara sonraki kayıtta yeniden kullanılır
  (`SoftDeleteWithNumberReclaimAsync`); geri alma System Log Info'ya yazılır,
  numara silme audit kaydında korunur
- Unique index: `IX_cargo_shipments_ShipmentNumber` (partial: IS NOT NULL); ihlalde
  sayaç mevcut max'a resync edilip yeniden denenir
- Eski `G/C-YYYY-NNNN` numaralar korunur; detaylı gerekçe: `docs/05-ADR/ADR-006-CargoNumberCounter.md`

---

## Kargo Portal / Takip Bağlantısı (2026-07-22, tek kaynak)

- Çalışma zamanı **tek bağlantı kaynağı `CargoCompany.PortalUrl`'dir** ("Kargo Portal /
  Takip Bağlantısı"); firma adına göre hard-code edilmez, yalnızca Kargo Firmaları
  ekranından düzenlenir (mevcut yönetim yetkisi)
- Doğrulama: yalnızca `http`/`https` (`UrlValidator`), boş bırakılabilir
- Kargo ekleme/düzenleme, liste ("Portal / Takip" kolonu) ve Operasyon Merkezi
  ("Portalı / Takibi Aç") aynı değeri kullanır; URL boşsa buton pasif/link gizli
- Bağlantıyı açmak için kargo ekranına erişim yeterlidir; yönetim yetkisi istenmez
- Eski yapılar: `CargoCompany.TrackingUrlTemplate` UI'dan ve çalışma zamanından
  kaldırıldı (kolon/veri korunur, VM passthrough); kayıt bazlı `CargoShipment.TrackingUrl`
  artık üretilmez — eski kayıtlarda saklı değer korunur ve gösterimde öncelik alır,
  boşsa firma PortalUrl'ine düşülür (DTO/builder seviyesinde)
- Yurtiçi Kargo varsayılan bağlantısı migration/seed ile gelir; değişiklikler
  `CargoCompanyUpdated` audit'inde izlenir

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

## Gönderi / Teslim İsim Önerileri (2026-08-04)

`Gönderen`, `Teslim Eden`, `Alıcı`, `Teslim Alan` alanları düzenlenebilir ComboBox'tır
(Dikkatine alanıyla aynı desen). Öneriler **geçmiş kargo kayıtlarından** türetilir —
ayrı rehber tablosu ve kullanıcı bakımı yoktur, liste kullanıldıkça kendini besler.

- Handler: `GetCargoPartySuggestionsHandler`
- Repository: `ICargoShipmentRepository.GetPartyNameHistoryAsync` — entity yüklemez,
  yalnızca 4 isim kolonu + `CreatedAt` projekte edilir
- Öneriler **yön bazlıdır** (gelen/giden isim kümeleri karışmaz)
- Taranan kayıt: son 500; alan başına gösterilen: en fazla 30
- Tekilleştirme Türkçe farkındadır (`TextNormalizer.TurkishIgnoreCase`):
  `OrdinalIgnoreCase` "YILMAZ" ile "Yılmaz"ı farklı sayıp listeyi ikizlerle doldururdu
- Listede olmayan yeni isim serbestçe yazılabilir

---

## ComboBox Boş Seçimi (2026-08-04)

Kargo ekleme/düzenleme formunda **DB'de nullable olan** alanlarda listenin başına
`— Seçim yok —` satırı eklenir; seçim böylece geri temizlenebilir (önceden bir firma
seçildikten sonra temizlemenin hiçbir yolu yoktu).

| Alan | Boş seçim | Gerekçe |
|------|-----------|---------|
| Kargo Türü | ✅ | `CargoShipment.ShipmentType` nullable |
| Kargo Firması | ✅ | `CargoCompanyId` nullable |
| Firma Rehberi | ✅ | `CompanyDirectoryId` nullable |
| Durum / Öncelik / Bildirim Durumu | ❌ | Kolonlar NOT NULL — boş seçim şema değişikliği ister |

Uygulama: koleksiyonun başına sentinel DTO (`Id = Guid.Empty`) eklenir; ViewModel setter'ı
sentinel'i `null`'a çevirir. Liste tipi ve `DisplayMemberPath` bozulmaz.

Liste/filtre ekranlarında zaten `(Tümü)` seçeneği vardır; oralarda değişiklik yapılmadı.

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
1. Alıcı firma e-postasıyla dolar; **"Varsayılan CC"** işaretli mail rehberi kişileri
   CC alanına otomatik eklenir.
2. Alıcı ve CC **birden fazla adres** kabul eder (`;` / `,` / boşluk ile ayrılır).
   "📇 Rehber" butonu ortak mail rehberinden çoklu seçim yapar; seçilenler mevcut
   metnin üzerine eklenir.
3. Geçersiz adres varsa gönder butonu pasifleşir ve hatalı adres adıyla gösterilir.
4. Gönderim başarılı olursa kullanılan adreslerin `LastUsedAt` bilgisi tazelenir ve
   Status → `Mail Hazır` olarak güncellenir.

Detay: [`docs/03-Modules/Mail.md`](Mail.md) → "Mail Rehberi (Ortak)"

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

### Özet Kart Tanımları (2026-08-04'te netleştirildi)

Her kartın üzerine gelindiğinde ne saydığını anlatan tooltip görünür.

| Kart | Kapsam |
|------|--------|
| Bugün Gelen / Giden | Gönderim tarihi bugün olan kayıtlar |
| **Toplam Bekleyen** | `CargoShipmentStatusRules.IsPending` — Teslim Edildi, **Personele Teslim Edildi** ve İptal dışındaki tüm kayıtlar (gelen + giden, tarih filtresi yok) |
| Bildirim Bekleyen | Bildirim hazırlanmamış + aktif durumdaki kargolar |
| Acil Bekleyen | Önceliği Acil/Çok Acil olan, teslim edilmemiş ve iptal edilmemiş kargolar |
| Bugün Teslim | Bugün "Teslim Edildi" durumuna geçenler |

**Not:** "Bekleyen" tanımı Dashboard kartı ile kargo raporunda **ortaktır**
(`CargoShipmentStatusRules.IsPending`). İki ekranın farklı sayı göstermemesi için kural
tek kaynakta toplanmıştır. `Personele Teslim Edildi` önceden bekleyen sayılıyordu; gelen
kargoda bu durum kaydın operasyonel olarak kapandığı andır, bu yüzden sayımdan çıkarıldı.

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
| WhatsApp / Mail rehberi okuma | (izin yok — oturum yeterli) |
| WhatsApp / Mail rehberi CRUD | CanManageIncomingCargo **veya** CanManageOutgoingCargo **veya** CanManageCompanyDirectory |

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
MailContactCreated, MailContactUpdated, MailContactDeleted
```
