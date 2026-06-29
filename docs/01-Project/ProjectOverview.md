# Proje Genel Bakış

## Amaç

**Yönetim Finansal İşlem Takip Sistemi**, şirket içi özel finansal işlemlerin ve kargo sevkiyatlarının düzenli, izlenebilir ve denetlenebilir şekilde kayıt altına alınması amacıyla geliştirilmiş Windows masaüstü uygulamasıdır.

## Kurumsal Hedef

- Finansal hareketleri merkezi olarak takip etmek
- Kargo gönderim ve alımlarını yönetmek
- Kullanıcı işlemlerini denetim kaydıyla izlenebilir kılmak
- Hata bildirimlerini otomatik olarak yöneticilere iletmek
- Kurumsal sunucu ortamına taşınabilir, bakımı kolay bir sistem sunmak

---

## Teknoloji Yığını

| Katman | Teknoloji |
|--------|-----------|
| UI | C# / WPF (.NET 9) |
| ORM | Entity Framework Core |
| Veritabanı | PostgreSQL |
| Sürücü | Npgsql |
| Dağıtım | ClickOnce |
| Loglama | Serilog (dosya tabanlı, rolling) |
| PDF Export | QuestPDF |
| Excel Export | ClosedXML |
| Şifreleme | AES (ayar gizleme) |
| Parola Hash | BCrypt |
| Publish Aracı | microsoft.dotnet.mage |
| VCS | Git / GitHub |

---

## Mimari

Clean Architecture (katmanlı):

```
Domain          ← Entity'ler, enum'lar, iş kuralları sabitleri
Application     ← Use case handler'lar, repository interface'leri, servis interface'leri
Infrastructure  ← EF Core, repository impl., audit log, dış servisler
UI              ← WPF ekranları, ViewModel'ler, uygulama başlatma
```

Bkz. [`docs/02-Architecture/CleanArchitecture.md`](../02-Architecture/CleanArchitecture.md)

---

## Modüller

### Finans Modülü
- Nakit işlem giriş/düzenleme/silme (Giriş / Çıkış)
- Para birimi bazında kümülatif bakiye (TRY / USD / EUR)
- Filtreli işlem listesi (tarih, tür, para birimi, açıklama)
- Finansal dashboard (kart özeti, trend grafiği, son işlemler)
- Rapor ekranı (PDF + Excel export)

### Kargo Katip Modülü
- Gelen ve giden kargo sevkiyat yönetimi
- Firma rehberi ve kargo firmaları yönetimi
- Kargo etiketi PDF çıktısı
- WhatsApp ve Mail bildirim hazırlama
- Operasyon merkezi (etiket, bildirim, durum yönetimi)
- Kargo dashboard ve raporu
- Dikkatine (Attention Contact) sistemi

### Kullanıcı Yönetimi
- Kullanıcı CRUD (yönetici tarafından)
- Yetki yönetimi (granüler, permission-bazlı)
- BCrypt parola hash

### Raporlama ve Analiz
- Finansal rapor (tarih aralığı, tür, para birimi filtreli)
- Detay satırları + genel toplamlar
- PDF ve Excel export

### Audit Log
- Tüm kritik işlemler kayıt altında (kim, ne, ne zaman, hangi bilgisayar, eski/yeni değer)
- Filtreli görüntüleme ekranı

### Sistem Log ve İzleme
- Uygulama log (Serilog, dosya bazlı)
- Sistem log ekranı (DB tabanlı, kritik hataları arayüzde listeler)
- SMTP ile otomatik hata bildirimi (admin e-posta)
- Sistem sağlığı ekranı (DB bağlantısı, migration durumu, backup klasörü)

### Güncelleme Sistemi
- ClickOnce startup güncelleme (foreground, sıfır kod)
- Manuel güncelleme kontrolü (Yardım menüsü, version.json okuma)

---

## Kullanıcı Rolleri

Rol sistemi yoktur; yetki sistemi granüler permission bazlıdır.

| PermissionType | Açıklama |
|---------------|----------|
| CanManageUsers = 1 | Kullanıcı yönetimi |
| CanManagePermissions = 2 | Yetki yönetimi |
| CanCreateTransaction = 3 | İşlem oluşturma |
| CanEditTransaction = 4 | İşlem düzenleme |
| CanDeleteTransaction = 5 | İşlem silme |
| CanViewReports = 6 | Rapor görüntüleme |
| CanManageExchangeRates = 7 | Döviz kuru yönetimi |
| CanViewCargoModule = 8 | Kargo modülü görüntüleme |
| CanViewIncomingCargo = 9 | Gelen kargo görüntüleme |
| CanManageIncomingCargo = 10 | Gelen kargo yönetimi |
| CanViewOutgoingCargo = 11 | Giden kargo görüntüleme |
| CanManageOutgoingCargo = 12 | Giden kargo yönetimi |
| CanManageCompanyDirectory = 13 | Firma rehberi yönetimi |
| CanManageCargoCompanies = 14 | Kargo firmaları yönetimi |

---

## Dağıtım Modeli

- Her çalışan bilgisayarına ClickOnce ile kurulum yapılır.
- PostgreSQL veritabanı merkezi bir sunucuda çalışır.
- Tüm istemciler aynı veritabanına bağlanır.
- Güncelleme UNC ağ paylaşımı üzerinden dağıtılır (`\\SUNUCU\YonetimPublish\`).

---

## Mevcut Durum (V1 — Tamamlandı)

Tüm temel modüller tamamlanmış ve production ortamına hazır haldedir.

- Login + yetki kontrolü: ✅
- Finans modülü (CRUD + raporlama + dashboard): ✅
- Kargo Katip modülü (sevkiyat + bildirim + etiket + dashboard): ✅
- Audit log: ✅
- Sistem log + SMTP bildirim: ✅
- ClickOnce güncelleme sistemi: ✅
- Backup/restore scriptleri: ✅
- Tema sistemi (Light): ✅

---

## Yol Haritası Özeti

Bkz. [`docs/01-Project/Roadmap.md`](Roadmap.md)

**V1 sonrası planlananlar:**
- TCMB entegrasyonu (döviz kurları otomatik çekme)
- Transfer işlemi iki taraflı model
- Koyu tema (Karanlık mod)
