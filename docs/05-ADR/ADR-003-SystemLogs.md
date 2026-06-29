# ADR-003: Sistem Log Mimarisi

**Tarih:** 2026-06-28  
**Durum:** Kabul Edildi

---

## Karar

İki katmanlı log sistemi: Serilog (dosya) + SystemLog (DB tablosu).

- **Serilog:** Global exception handler'lar, startup hataları — dosyaya yazar.
- **SystemLog:** Uygulama seviyesi önemli olaylar — DB'ye yazar, arayüzden görüntülenir.
- **AuditLog:** İş akışı denetimi — ayrı tablo, ayrı amaç.

---

## Bağlam

- Üretim ortamında hataları görmek için log dosyasına erişim zorunlu ve zahmetli.
- Yönetici kritik hataları e-posta ile almalı.
- Audit log iş akışları için, sistem log teknik hatalar için kullanılır — karıştırılmamalı.
- Serilog zaten altyapıda mevcuttu; bunun üzerine DB katmanı eklendi.

---

## Alternatifler

### A: Yalnızca Serilog

Tüm loglar dosyaya. Hata yönetimi manuel (birisi dosyayı açıp kontrol etmeli).

**Sorun:** Uzak sunucuda log dosyasına erişmek zahmetli. Kritik hataları kaçırma riski.

### B: Yalnızca DB Log

Tüm loglar DB'ye. Serilog kaldırılır.

**Sorun:** Uygulama başlamadan önce oluşan hatalar (DB bağlantısı açılamadı gibi) kaybolur.

### C: Harici Log Servisi (Seq, ELK, Application Insights)

**Sorun:** Bu ölçekteki proje için overkill. Ek altyapı gerektirir.

### D: İki Katmanlı Sistem (Seçilen)

Serilog startup ve global hatalar için; SystemLog uygulama içi önemli olaylar için.

---

## Neden Seçildi

- Serilog startup hatalarını bile yakalar (DB bağlantısı yokken bile çalışır).
- SystemLog DB'de olduğu için uygulama içinden arayüzle görüntülenir.
- SMTP bildirimi yalnızca `Critical` seviyede tetiklenir — spam riski yok (cooldown ile desteklenir).
- Audit log iş amacı için saf kalır.

---

## Seviye Kararı

| Seviye | Kullanım Alanı | Mail |
|--------|---------------|------|
| Info | Başarılı önemli işlemler | Hayır |
| Warning | Beklenen ama dikkat gerektiren (kullanıcı adı kaydedilemedi gibi) | Hayır |
| Error | İşlem başarısız, kurtarılabilir | Hayır |
| Critical | Sistem işlevsiz, veri kaybı riski | **Evet** |

---

## Artılar

- Yönetici kritik hataları anında alır.
- UI'dan sistem sağlığı izlenebilir.
- Audit log ve sistem log ayrı — sorumluluklar net.

---

## Eksiler

- DB bağlantısı yoksa SystemLog yazılamaz (Serilog fallback devreye girer).
- İki ayrı log mekanizması idare edilmeli.

---

## Sonuç

Bu mimari V1'de stabil çalışıyor. SMTP cooldown mekanizması spam sorununu önledi. Startup hatalarında Serilog'un dosyaya yazması kritik bilgi kaybını engelledi.
