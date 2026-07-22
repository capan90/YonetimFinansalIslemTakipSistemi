# İlerleme Kaydı

## Tamamlanan

### Altyapı ve Kurulum
- [x] Proje kurulumu, klasör yapısı, Git, CLAUDE.md
- [x] Teknik dokümantasyon (Architecture, Database, Audit-log, Dialog-system, Update-flow)
- [x] Domain katmanı: BaseEntity, User, CashTransaction, AuditLog, UserPermission
- [x] Enum'lar: TransactionType, CurrencyType, AuditAction, PermissionType, FinancialDirection
- [x] EF Core migration'lar: cash_transactions, users, audit_logs, user_permissions

### Kimlik Doğrulama ve Oturum
- [x] Login ekranı: LoginWindow, LoginViewModel, IAuthenticationService, AuthResult
- [x] DB-backed authentication: BCrypt, DatabaseAuthenticationService
- [x] IUserContext (okuma) + IUserSession (yazma) — singleton UserContext
- [x] Logout: IServiceScope başına oturum, while döngüsü, ShutdownMode.OnExplicitShutdown
- [x] Giriş sırasında kullanıcı izinleri DB'den yüklenir; IUserContext.HasPermission() tüm handler'larda

### Kullanıcı ve Yetki Yönetimi
- [x] CRUD: CreateUser, UpdateUser, DeleteUser, GetUsers
- [x] Son aktif kullanıcı silme / pasifleştirme koruması
- [x] UserManagementWindow + UserFormWindow
- [x] PermissionType (1–6), UserPermission entity, composite PK
- [x] UpdateUserPermissionsHandler: kilitlenme koruması, PermissionUpdated audit
- [x] UserPermissionWindow: kullanıcı seç, checkbox listesi, self-refresh
- [x] DevDataSeeder: upgrade-safe, eksik izinleri granüler ekler

### Nakit İşlemler
- [x] CreateCashTransaction, UpdateCashTransaction, DeleteCashTransaction (soft-delete)
- [x] GetCashTransactions: tarih / tür / para birimi filtreli liste
- [x] CashTransactionFormWindow, CashTransactionListViewModel
- [x] Handler-level yetki kontrolü: CanCreate / CanEdit / CanDelete

### Rapor
- [x] GetReportHandler: CanViewReports, tarih validasyonu, UTC yarı-açık aralık
- [x] GetReportDataAsync: GROUP BY PostgreSQL'de, kayıtlar belleğe alınmaz
- [x] TransactionTypeExtensions.GetFinancialDirection(): merkezi yön kuralı
- [x] ReportWindow: TL / USD / EUR özet kartları + işlem türü tablosu
- [x] CanViewReports = 6; DevDataSeeder upgrade-safe seed
- [x] xUnit test projesi: 11 test (TransactionTypeExtensions yön kuralları)

### Audit Log
- [x] AuditLog entity, IAuditLogService, AuditLogRepository
- [x] Tüm kritik işlemler audit kaydı: Create/Update/Delete İşlem, Create/Update/Delete User, Login, PermissionUpdated
- [x] AuditLogWindow: kullanıcı / tarih / işlem tipi filtreli liste

### Diyalog Sistemi
- [x] IDialogService, DialogService: Info, Success, Warning, Error, Confirmation

### Güncelleme Sistemi (ClickOnce)
- [x] IUpdateService / UpdateService: version.json okuma, sürüm karşılaştırma
- [x] DeploymentSettings: UNC konumu tek noktadan — env var YONETIM_UPDATE_PATH override
- [x] MainWindow → Yardım → Güncellemeleri Denetle menüsü
- [x] İki onay dialog akışı (Option B): güncelleme + uygulama kapatma onayı
- [x] ClickOnce.pubxml: localhost UNC, self-signed sertifika thumbprint
- [x] Publish-ClickOnce.ps1: dotnet-mage tabanlı CLI publish scripti
- [x] version.json AfterTargets: her publish sonrası UNC'ye yazılır
- Not: `dotnet publish` ClickOnce profili, Engine\Launcher.exe (VS bileşeni) gerektirdiğinden
  `Publish-ClickOnce.ps1` ve `microsoft.dotnet.mage` aracı kullanılır.

### Döviz Ekranı
- [x] ExchangeRate entity, IExchangeRateRepository, migration
- [x] CreateOrUpdateExchangeRateHandler, GetExchangeRatesHandler
- [x] ExchangeRateWindow: USD/EUR manuel kur girişi

### Nakit İşlem Listesi — Running Balance
- [x] GetAllForBalanceAsync: tüm aktif kayıtlar TransactionDate/CreatedAt/Id ASC
- [x] Handler: ASC geçişte per-currency kümülatif bakiye hesabı (GetFinancialDirection() üzerinden)
- [x] Filtre in-memory; tarih filtresi altında bile bakiye gerçek tarihsel değeri yansıtır
- [x] DTO: TlBalanceAfter, UsdBalanceAfter, EurBalanceAfter
- [x] MainWindow DataGrid: TL Bakiye / USD Bakiye / EUR Bakiye kolonları (sağa hizalı, N2)

### Kargo Katip — Sprint 1.1 Stabilizasyon (2026-06-24)
- [x] Migration AddCargoClerkModule DB'ye uygulandı
- [x] DevDataSeeder — upgrade-safe; yeni 7 izin otomatik seeded
- [x] CargoShipmentListWindow: yön bazında manage izni → Yeni/Düzenle/Sil visibility
- [x] CompanyDirectoryListWindow: CanManageCompanyDirectory → buton visibility
- [x] CargoCompanyListWindow: CanManageCargoCompanies → buton visibility
- [x] Build: 0 hata, 0 uyarı

### Kargo Katip — Sprint 1 (2026-06-24)

- [x] Domain: CompanyDirectory, CargoCompany, CargoShipment entity'leri (BaseEntity, soft delete)
- [x] Domain: 4 enum — CargoShipmentDirection, CargoShipmentType, CargoShipmentStatus, CargoNotificationStatus
- [x] PermissionType: 7 yeni izin (CanViewCargoModule=8 … CanManageOutgoingCargo=14)
- [x] AuditAction: 9 yeni aksiyon (CompanyDirectory/CargoCompany/CargoShipment Create/Update/Delete)
- [x] Application: ICompanyDirectoryRepository, ICargoCompanyRepository, ICargoShipmentRepository
- [x] Application: 15 handler (Create/Update/Delete/GetList her entity için), permission + audit
- [x] Infrastructure: 3 EF konfigürasyon (soft delete filter, FK ilişkileri, snake_case tablolar)
- [x] Infrastructure: 3 repository, AppDbContext güncellendi, ServiceRegistration güncellendi
- [x] Migration: AddCargoClerkModule (cargo_companies, company_directories, cargo_shipments)
- [x] UI: CompanyDirectoryList/Edit, CargoCompanyList/Edit, CargoShipmentList/Edit ViewModels + Views
- [x] UI: MainWindow "Kargo Katip" menüsü, permission-based visibility
- [x] UI: App.xaml.cs DI kayıtları (12 handler + 5 ViewModel)
- [x] Build: 0 hata, 0 uyarı

### Revizyon Sprinti — Harf Tercihi, Otomatik Kargo No, WhatsApp Rehberi, Kargo Portalı (2026-07-22)

#### Kullanıcı Bazlı Harf Tercihi
- [x] `TextCasePreference` enum (Preserve/Uppercase/Lowercase) + `UserPreference` entity (user_preferences, UserId unique)
- [x] `IUserTextNormalizationService` + `UserTextNormalizationService`: tr-TR kültürüyle merkezi dönüşüm; tercih `IUserContext`'ten okunur
- [x] Login'de tercih DB'den oturuma yüklenir; kaydetmede `IUserSession.SetTextCasePreference` ile anında etkinleşir
- [x] Dönüştürülen alanlar handler seviyesinde açıkça belirlendi: işlem açıklaması, firma adı/adres/il/ilçe, kişi adları, kargo notları, plaka, WhatsApp rehber alanları
- [x] Muaf alanlar: e-posta, telefon, URL, takip no, otomatik kargo no, posta kodu
- [x] Ayarlar → Harf Duyarlılığı penceresi (`TextCaseSettingsWindow`); `UserPreferenceUpdated` audit
- [x] NOT: Eski zorunlu TitleCase/UPPER davranışı kaldırıldı — dönüşüm artık tamamen kullanıcı tercihine bağlı (varsayılan: Olduğu Gibi)

#### Otomatik Kargo Numarası (GLN/GDN)
- [x] `cargo_number_counters` tablosu (yön başına satır); `CargoNumberCounter` entity — BaseEntity değil
- [x] `AddWithAutoNumberAsync`: sayaç artışı `INSERT ... ON CONFLICT DO UPDATE ... RETURNING` ile atomik, insert ile aynı transaction'da (rollback'te numara boşa gitmez); unique ihlalinde sayaç resync + retry
- [x] Format: Gelen `GLN00001`, Giden `GDN00001` (`CargoNumberFormatter`); eski `G/C-YYYY-NNNN` numaralar korunur
- [x] Create/Update request'lerinden `ShipmentNumber` kaldırıldı; UI'da salt okunur
- [x] Migration backfill: yalnızca NULL numaralar deterministik doldurulur; sayaçlar mevcut max'a eşitlenir
- [x] Bkz. `docs/05-ADR/ADR-006-CargoNumberCounter.md`

#### Ortak WhatsApp Rehberi
- [x] `WhatsAppContact` entity (whatsapp_contacts) — soft delete, normalize telefon üzerinde filtresiz unique index
- [x] `PhoneNumberNormalizer`: 0532/5xx/+90/0090 yazımları → `+905321234567`; TR mobil doğrulaması
- [x] CRUD handler'lar: mükerrer numarada anlaşılır uyarı; soft delete edilmiş numara yeniden eklenince kayıt geri yüklenir
- [x] `WhatsAppContactListWindow`: arama (ad/telefon/firma), firma filtresi, pasifleri göster, çift tıkla düzenle
- [x] Bildirim önizleme (WhatsApp modu): aranabilir çoklu seçim listesi + chip'ler + `+` hızlı ekleme (otomatik seçim); kişi seçiliyken telefon salt okunur
- [x] Toplu gönderim: her alıcı için ayrı wa.me açılışı; başarılı/başarısız raporu; alıcılar `CargoWhatsAppPrepared` audit'ine yazılır
- [x] Permission eklenmedi (bilinçli): oturum açan kullanıcılar rehberi görüntüleyip yönetebilir

#### Kargo Firması Portal Bağlantısı
- [x] `CargoCompany.PortalUrl` (500, opsiyonel) + `UrlValidator` (yalnızca http/https)
- [x] Kargo Firmaları düzenleme ekranına alan eklendi; değişiklik `CargoCompanyUpdated` audit'inde izlenir
- [x] Kargo düzenleme ekranı: seçili firmanın portal bağlantısı salt okunur + "Portalı Aç" butonu (firma adına if/else yok)
- [x] Yurtiçi Kargo varsayılanı migration'da: mevcut kayıtta boş URL doldurulur, kayıt yoksa sabit Id ile eklenir

#### Revizyon Düzeltmeleri (2026-07-22, aynı sprint)
- [x] Harf Duyarlılığı erişimi: Yardım → Kullanıcı Ayarlarım → Harf Duyarlılığı (yetki gerektirmez; Ayarlar'daki giriş korunur, aynı pencere/handler)
- [x] Silinen SON kargo numarasının kontrollü geri alınması: soft delete + koşullu sayaç geri alma tek transaction'da (`SoftDeleteWithNumberReclaimAsync`); aradaki silinmiş numaralar asla geri dönmez; geri alma System Log Info'ya, numara silme audit'ine yazılır — schema değişikliği yok, migration gerekmedi
- [x] Testler: yetkisiz kullanıcı tercih kaydı (4 test) + numara geri alma senaryoları (8 test) — toplam 78/78

#### Ortak
- [x] Migration: `AddUserPrefsWhatsAppDirectoryAndCargoCounters`
- [x] Test projesi Application referansı aldı; 66 test (harf tercihi, telefon normalize, kargo no, arama, portal URL)
- [x] Build: 0 hata, 0 uyarı; testler: 66/66 başarılı

---

## Sıradaki (V2)

- TCMB entegrasyonu: döviz kurlarını otomatik çekme
- Transfer işlemi iki taraflı model (bkz. Teknik Borç)
