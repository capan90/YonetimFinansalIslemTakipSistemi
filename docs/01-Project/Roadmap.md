# Yol Haritası

## V1 — Tamamlandı ✅

### Altyapı ve Kurulum ✅
- [x] Proje kurulumu, klasör yapısı, Git, CLAUDE.md
- [x] Teknik dokümantasyon
- [x] Domain / Application / Infrastructure / UI katmanları
- [x] EF Core migration'lar: cash_transactions, users, audit_logs, user_permissions

### Kimlik Doğrulama ve Oturum ✅
- [x] Login ekranı (LoginWindow, LoginViewModel)
- [x] DB-backed authentication (BCrypt)
- [x] IUserContext / IUserSession
- [x] Logout (per-session scope)
- [x] Son kullanıcı adı hatırlama

### Kullanıcı ve Yetki Yönetimi ✅
- [x] CRUD: CreateUser, UpdateUser, DeleteUser, GetUsers
- [x] Son aktif kullanıcı koruma
- [x] PermissionType (1–14), UserPermission entity
- [x] DevDataSeeder (upgrade-safe)

### Nakit İşlemler ✅
- [x] CreateCashTransaction, UpdateCashTransaction, DeleteCashTransaction (soft-delete)
- [x] GetCashTransactions: filtrelenmiş liste
- [x] Running balance: TL / USD / EUR kümülatif bakiye
- [x] Borç (Giriş) / Alacak (Çıkış) modeli
- [x] Nakit işlem kopyalama

### Rapor ve Analiz ✅
- [x] GetReportHandler: tarih validasyonu, UTC yarı-açık aralık
- [x] ReportWindow: özet kartlar + işlem tablosu + detay satırları
- [x] PDF export (QuestPDF) + Excel export (ClosedXML)
- [x] ReportPreviewWindow
- [x] Finans dashboard (trend, son işlemler, bakiye kartları)
- [x] Analiz ekranı

### Audit Log ✅
- [x] AuditLog entity + IAuditLogService
- [x] Tüm kritik işlemlerde audit kaydı
- [x] AuditLogWindow (filtrelenebilir)

### Sistem Log ve İzleme ✅
- [x] SystemLog entity + ISystemLogService
- [x] Log seviyeleri: Info / Warning / Error / Critical
- [x] Sistem Log izleme ekranı
- [x] SMTP hata bildirimleri (mail cooldown mekanizması)
- [x] Sistem Sağlığı ekranı
- [x] Serilog dosya tabanlı loglama

### Güncelleme Sistemi (ClickOnce) ✅
- [x] ClickOnce startup güncelleme (foreground)
- [x] Manuel güncelleme kontrolü (version.json + Yardım menüsü)
- [x] Publish-ClickOnce.ps1 (dotnet-mage tabanlı)

### Döviz Ekranı ✅
- [x] ExchangeRate entity, migration
- [x] USD / EUR manuel kur girişi

### Kargo Katip Modülü ✅
- [x] CompanyDirectory (firma rehberi)
- [x] CargoCompany (kargo firmaları)
- [x] CargoShipment (gelen/giden sevkiyat)
- [x] Kargo numarası otomasyonu
- [x] Durum yönetimi + CargoStatusTransitions
- [x] Bildirim durumu takibi
- [x] Kargo etiketi PDF (Dikkatine sistemi, QR placeholder)
- [x] WhatsApp bildirim hazırlama
- [x] Mail bildirim hazırlama
- [x] CargoOperationCenterWindow
- [x] Kargo dashboard + raporu + PDF export

### Tema Sistemi ✅
- [x] IThemeService + ThemeService
- [x] LightTheme.xaml
- [x] DarkTheme.xaml (hazır, devre dışı)
- [x] Görünüm Ayarları ekranı

### Production Readiness ✅
- [x] appsettings.json + env var konfigürasyon
- [x] Backup/restore scriptleri
- [x] Disaster Recovery Plan
- [x] Release Checklist
- [x] AES-256 ayar şifreleme

---

## Teknik Borç

### Transfer İşlemi — Tek Taraflı Model (V1)
V1'de Transfer kaydı tek taraflı çıkış olarak işlenir.  
Gerçek kasa-arası transfer için:
- Kaynak kasa / para birimi + hedef kasa / para birimi alanları
- Opsiyonel kur bilgisi
- Eş hareket kaydı (kaynak çıkış + hedef giriş)
- `GetFinancialDirection()` ve rapor handler güncellemesi

### Koyu Tema
`DarkTheme.xaml` hazır; WPF DynamicResource sorunları nedeniyle devre dışı bırakıldı.  
`AppearanceSettingsWindow`'da "Koyu Tema (Yakında)" olarak görünür.

---

## V2 Planları

- **TCMB Entegrasyonu:** Döviz kurlarını API üzerinden otomatik çekme
- **Transfer İşlemi İki Taraflı Model:** Kaynak/hedef kasa ayrımı
- **Koyu Tema Aktivasyonu:** DarkTheme.xaml tamamlanması
- **Mail Gönderme Aktivasyonu:** Kargo mail hazırlama → gerçek gönderim
- **WhatsApp Entegrasyonu:** WhatsApp Business API veya link açma
