# ADR-004: Kargo Katip Modül Kararları

**Tarih:** 2026-06-24  
**Durum:** Kabul Edildi

---

## Karar 1: Kargo Modülü Clean Architecture'a Dahil Edildi

Kargo Katip, finans modülüyle aynı katmanlı yapıyı izler. Ayrı proje veya microservice değil.

**Gerekçe:**
- Hem finans hem kargo aynı kullanıcı, izin ve audit altyapısını paylaşır.
- Ayrı proje gerektirecek ölçekte bir karmaşıklık yok.
- 15 handler, 3 entity clean architecture içinde rahat sığar.

---

## Karar 2: Granüler Kargo İzinleri

Tek "CanViewCargo" yerine 7 ayrı izin:

```
CanViewCargoModule = 8
CanViewIncomingCargo = 9
CanManageIncomingCargo = 10
CanViewOutgoingCargo = 11
CanManageOutgoingCargo = 12
CanManageCompanyDirectory = 13
CanManageCargoCompanies = 14
```

**Gerekçe:**
- Bazı çalışanlar yalnızca gelen kargoyu görmeli; giden kargoya erişimi olmamalı.
- Firma rehberi yönetimi ayrı bir sorumluluğa atanabilir.
- Finans modeliyle tutarlı (CanCreate/Edit/Delete ayrı izinler).

---

## Karar 3: Durum Geçiş Makinası

`CargoStatusTransitions` statik sınıfı geçerli geçişleri tanımlar.

**Alternatif:** Her durum geçişini serbest bırakmak.

**Gerekçe:**
- "Teslim Edildi" durumundan "Beklemede"ye geri dönmek anlamsız ve veri bütünlüğünü bozar.
- Geçerli geçişler iş kuralı — handler seviyesinde korunmalı.
- UI yalnızca geçerli seçenekleri gösterir (AllowedStatusOptions).

---

## Karar 4: Kargo Numarası Otomasyonu

Manuel giriş yerine otomatik `G-YYYY-XXXX` / `C-YYYY-XXXX` formatı.

**Gerekçe:**
- Manuel giriş hata ve tekrar riski taşır.
- Unique index ile çakışma önlenir.
- Yıl bazlı numaralandırma kronolojik takibi kolaylaştırır.
- Soft-delete'i dahil etmek (`IgnoreQueryFilters`) unique garantisi sağlar.

---

## Karar 5: ReceiverEmailSnapshot

Mail bildirimi hazırlanırken alıcı e-posta adresi kargo kaydına kopyalanır.

**Gerekçe:**
- Firma e-postası sonradan değişebilir.
- Bildirim anındaki adresin korunması audit ve itiraz senaryolarında kritik.
- Gönderilen e-postanın hep doğru adrese gittiği garanti edilir.

---

## Karar 6: Dikkatine (Attention Contact) Sistemi

Firma rehberindeki `ContactPerson` alanı etiket ve bildirim mesajlarında "Dikkatine: [Kişi]" olarak kullanılır.

**Gerekçe:**
- Ticaret kültüründe "Dikkatine" kullanımı yaygın ve zorunlu.
- Alıcı firmada doğru kişiye ulaşmayı sağlar.
- Firma seçildiğinde otomatik dolar — kullanıcı tekrar girmez.

---

## Karar 7: Mail Gönderimi V1'de Devre Dışı

Mail bildirim hazırlama akışı implement edildi; gerçek gönderim V2'ye bırakıldı.

**Gerekçe:**
- V1'de SMTP sistemi hata bildirimleri için kullanılıyor (kritik altyapı).
- Kargo bildirim maillerini aynı SMTP ile karıştırmak cooldown ve konfigürasyon çakışması riski taşır.
- "Mail Hazırla" UI'ı tamamdır; "Mail Gönder" butonu `IsEnabled="False"`.

---

## Karar 8: 6 Aylık Retention Politikası

`CargoRetentionService` (Background Singleton, `IServiceScopeFactory`):
- Terminal durumdaki (Teslim Edildi / İptal Edildi) kayıtlar 6 ay sonra arşivlenir.

**Gerekçe:**
- Aktif kargo listelerinin yönetilebilir boyutta kalması.
- Yasal saklama yükümlülüğü için 6 ay yeterli (işletme tarafından belirlenmiş).
- Singleton + IServiceScopeFactory pattern anti-pattern'i önler.
