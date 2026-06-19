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

---

## Sıradaki

- TCMB entegrasyonu (opsiyonel V2 özelliği)
