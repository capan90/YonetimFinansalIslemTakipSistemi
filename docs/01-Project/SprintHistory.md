# Sprint Geçmişi

Kronolojik sprint kaydı. Her sprint için yapılanlar, önemli kararlar ve karşılaşılan problemler.

---

## Proje Kurulumu (2026-06-18 öncesi)

**Yapılanlar:**
- Proje klasör yapısı, README, CLAUDE.md, AGENTS.md hazırlandı.
- Git deposu kuruldu.
- Teknik dokümantasyon oluşturuldu (Architecture, Database, Audit-log, Dialog-system, Update-flow).
- .NET 9 solution yapısı: Domain / Application / Infrastructure / UI katmanları.

---

## Temel Altyapı — Session 1-2 (2026-06-18)

**Yapılanlar:**
- Domain katmanı `src/` altına taşındı; orphan klasör temizlendi.
- `Application` namespace ile `System.Windows.Application` çakışması giderildi (tam niteleme).
- Application katmanı feature-based yapıya taşındı (`Features/`, `Interfaces/`, `Common/`).
- `CreateCashTransaction` use case: Request / Response / Handler / OperationResult.
- Infrastructure katmanı tamamlandı: `AppDbContext`, `CashTransactionConfiguration`, `CashTransactionRepository`, `AppDbContextFactory`, `ServiceRegistration`.
- EF Core migration oluşturuldu ve PostgreSQL'e uygulandı (`cash_transactions` tablosu).
- WPF DI composition root bağlandı (`App.xaml.cs`).
- Uçtan uca akış doğrulandı: `CreateCashTransaction` → PostgreSQL kaydı.

**Commit'ler:** `6f893d0`, `d6e4ab6`, `2ff6f08`

---

## İşlem Listesi ve Filtreleme — Session 3 (2026-06-18)

**Yapılanlar:**
- `GetCashTransactionsQuery`, `CashTransactionDto`, `GetCashTransactionsHandler` eklendi.
- `ICashTransactionRepository.GetFilteredAsync`: tarih / tür / para birimi filtreleri LINQ zinciri.
- `RelayCommand`, `CashTransactionListViewModel` (harici MVVM paketi olmadan).
- `MainWindow`: filtre paneli (DatePicker × 2, ComboBox × 2) + DataGrid.
- DI scope `MainWindow.Closed` olayına bağlandı — DbContext pencere ömrünü aşmaz.

**Commit:** `051b892`

---

## Kullanıcı Yönetimi Dikey Dilimi

**Yapılanlar:**
- `User` entity, `IUserRepository`, migration.
- `CreateUser`, `UpdateUser`, `DeleteUser`, `GetUsers` handler'ları.
- Son aktif kullanıcı silme koruması.
- `UserManagementWindow`, `UserFormWindow`.
- BCrypt parola hash.

---

## Login ve Kimlik Doğrulama

**Yapılanlar:**
- `LoginWindow`, `LoginViewModel`, `IAuthenticationService`, `AuthResult`.
- `DatabaseAuthenticationService`: BCrypt doğrulama.
- `IUserContext` (okuma) + `IUserSession` (yazma) → singleton `UserContext`.
- Giriş sırasında kullanıcı izinleri DB'den yüklenir; `IUserContext.HasPermission()` tüm handler'larda kontrol edilir.

---

## Temel Modüller Tamamlanması

**Yapılanlar:**
- `UpdateCashTransaction`, `DeleteCashTransaction` (soft-delete).
- `IDialogService`, `DialogService`: Info / Success / Warning / Error / Confirmation.
- Audit log: `AuditLog` entity, `IAuditLogService`, `AuditLogRepository`, `AuditLogWindow`.
- Tüm kritik işlemler audit kaydına bağlandı.
- Logout: `IServiceScope` başına oturum, `while` döngüsü, `ShutdownMode.OnExplicitShutdown`.
- Yetki sistemi: `PermissionType` (1–6), `UserPermission` entity, `UpdateUserPermissionsHandler`.
- `UserPermissionWindow`: kullanıcı seç → checkbox listesi.

---

## Rapor Ekranı ve Export

**Yapılanlar:**
- `GetReportHandler`: `CanViewReports`, tarih validasyonu, UTC yarı-açık aralık.
- `GetReportDataAsync`: GROUP BY PostgreSQL'de; kayıtlar belleğe alınmaz.
- `TransactionTypeExtensions.GetFinancialDirection()`: merkezi yön kuralı.
- `ReportWindow`: TL / USD / EUR özet kartları + işlem türü tablosu.
- PDF export (QuestPDF 2024.3.5) + Excel export (ClosedXML).
- `ReportPreviewWindow`.
- xUnit test projesi: 11 test (TransactionTypeExtensions yön kuralları).

---

## Döviz Ekranı

**Yapılanlar:**
- `ExchangeRate` entity, `IExchangeRateRepository`, migration.
- `CreateOrUpdateExchangeRateHandler`, `GetExchangeRatesHandler`.
- `ExchangeRateWindow`: USD / EUR manuel kur girişi.

---

## Running Balance (Kümülatif Bakiye)

**Yapılanlar:**
- `GetAllForBalanceAsync`: tüm aktif kayıtlar `TransactionDate/CreatedAt/Id` ASC.
- Handler: ASC geçişte per-currency kümülatif bakiye hesabı.
- Filtre in-memory; tarih filtresi altında bile bakiye gerçek tarihsel değeri yansıtır.
- `MainWindow` DataGrid: TL / USD / EUR Bakiye kolonları (sağa hizalı, N2).

---

## ClickOnce Güncelleme Sistemi

**Yapılanlar:**
- `IUpdateService`, `UpdateService`: version.json okuma, sürüm karşılaştırma.
- `DeploymentSettings`: UNC konumu tek noktadan; `YONETIM_UPDATE_PATH` env var override.
- `MainWindow → Yardım → Güncellemeleri Denetle` menüsü.
- İki onay dialog akışı (Option B): güncelleme + uygulama kapatma onayı.
- `ClickOnce.pubxml`: localhost UNC, self-signed sertifika thumbprint.
- `Publish-ClickOnce.ps1`: `dotnet-mage` tabanlı CLI publish scripti.

**Önemli Karar:** `dotnet publish /p:PublishProfile=ClickOnce` .NET 9 CLI'de `Engine\Launcher.exe` eksikliği nedeniyle çalışmaz (VS bileşeni). Bu nedenle `microsoft.dotnet.mage` aracı kullanılır.

Bkz. [`docs/02-Architecture/ClickOnce.md`](../02-Architecture/ClickOnce.md)

---

## Production Readiness Sprint 1 — Configuration Management

**Yapılanlar:**
- Hardcoded connection string kaldırıldı.
- `appsettings.json` yapısı ve environment variable desteği.
- `AppDbContextFactory` yeniden düzenlendi.
- Örnek production konfigürasyon dosyası (`docs/config-production-example.json`).
- Production şifreleri `.gitignore` ile korundu.

**Commit:** `36c38af`

---

## Production Readiness Sprint 2 — Loglama ve Global Exception Handling

**Yapılanlar:**
- Serilog entegrasyonu, rolling log dosyaları.
- `DispatcherUnhandledException`, `AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException` yönetimi.
- Log Klasörünü Aç menü öğesi.

**Commit:** `27d26b4`

---

## Production Readiness Sprint 3 — Backup ve Kurtarma

**Yapılanlar:**
- `Backup-Database.ps1`, `Backup-And-Migrate.ps1`, `Restore-Database.ps1` scriptleri.
- `Backup-Recovery-Guide.md`, `Disaster-Recovery-Plan.md`, `Emergency-SQL-Commands.md`.
- `Release-Checklist.md`.
- Backup dosyaları `.gitignore` ile korundu.

**Commit:** `127cd65`

---

## Production Readiness Sprint 4 — Sistem Sağlığı İzleme

**Yapılanlar:**
- Sistem Sağlığı ekranı: DB bağlantısı, migration durumu, log/backup klasörü, version.json.
- Yönetici yetkisiyle erişim.

**Commit:** `78fd8ef`

---

## Production Readiness Sprint 5 — SMTP Hata Bildirimleri

**Yapılanlar:**
- `SmtpErrorNotificationService`, `NotificationContext`.
- Mail cooldown mekanizması (aynı hata için tekrarlayan bildirim engeli).
- SMTP ayarları `appsettings` ve env var üzerinden.
- Kritik hata noktaları bildirim sistemine bağlandı.
- Health Check ekranına bildirim durumu eklendi.

**Commit:** `bbd32ba`

---

## Finans Dashboard

**Yapılanlar:**
- `GetDashboardHandler`: para birimi kartları, trend, son işlemler.
- `AnalysisWindow` (grafik + trend).

**Commit:** `5d27eca`

---

## Debit/Credit Mantığı Hizalaması

**Yapılanlar:**
- Muhasebe kurallarına uyum: Giriş → Borç (yeşil), Çıkış → Alacak (kırmızı).
- `GetCashTransactionsHandler`, `GetReportHandler`, `GetDashboardHandler`, `ReportExportService`, `ReportPreviewWindow` güncellendi.
- XAML renk değişiklikleri.

**Commit:** `3816f51`

---

## Kargo Katip — Sprint 1 (2026-06-24)

**Yapılanlar:**

**Domain:**
- `CompanyDirectory`, `CargoCompany`, `CargoShipment` entity'leri (soft delete, navigation props).
- 4 enum: `CargoShipmentDirection`, `CargoShipmentType`, `CargoShipmentStatus`, `CargoNotificationStatus`.
- `PermissionType`'a 7 yeni izin (8–14).
- `AuditAction`'a 9 yeni aksiyon.

**Application:** 3 repository interface, 15 handler.

**Infrastructure:** 3 EF config, 3 repository, migration `AddCargoClerkModule`.

**UI:** `CompanyDirectoryList/Edit`, `CargoCompanyList/Edit`, `CargoShipmentList/Edit` View + ViewModel. `MainWindow` "Kargo Katip" menüsü.

**Commit:** `4c939e5`

---

## Kargo Katip — Sprint 1.1 Stabilizasyon (2026-06-24)

**Yapılanlar:**
- `DevDataSeeder` upgrade-safe: `Enum.GetValues<PermissionType>().Except(existing)` ile 7 yeni izin otomatik seed edildi.
- `CargoShipmentListWindow`: yön bazında manage izni → Yeni/Düzenle/Sil `Visibility.Collapsed`.
- `CompanyDirectoryListWindow`, `CargoCompanyListWindow` aynı pattern.

**Commit:** `4356b58`

---

## Kargo Katip — Sprint 2 (2026-06-25)

**Yapılanlar:**
- ShipmentNumber otomasyonu: G-YYYY-0001 (giden) / C-YYYY-0001 (gelen).
- Unique index + Migration `AddCargoShipmentOperationalFlow`.
- Takip URL: `DataGridTemplateColumn/Hyperlink` → tıklanabilir.
- `NotificationStatus` UI: ComboBox, liste kolonu.
- `CargoStatusTransitions`: geçersiz durum geçişi engeli.
- Liste ekranı 8 kolon.

---

## Kargo Katip — Sprint 3 — Etiket ve Bildirim (2026-06-25/26)

**Yapılanlar:**
- **Sprint 3.2 / 3.2.1:** Kargo etiketi PDF — kurumsal tasarım (`QuestPdfLabelRenderer`), Dikkatine (Attention Contact) sistemi, Kargo Barkodu placeholder, QR placeholder.
- **Sprint 3.4:** `MailNotificationComposer`, `CargoNotificationModel` (TargetEmail + Subject), `MarkCargoNotificationPreparedHandler` mail dalı.
- `CargoNotificationPreviewWindow`: mail/whatsapp mode desteği, "Mail Gönder" (UI hazır, backend pending).
- WhatsApp bildirim hazırlama akışı.

---

## Kargo Katip — Sprint 4 — Operasyon Merkezi UX (2026-06-26)

**Yapılanlar:**
- `CargoOperationCenterWindow`: 5 operasyon kartı (Etiket, WhatsApp, Mail, Takip, Durum).
- `CargoShipmentListWindow`: 5 buton → tek "Operasyon" butonu; arama türü ComboBox.
- `CargoShipmentEditWindow`: GroupBox (Gönderi/Teslim Bilgileri), firma kartı, Ctrl+S, tooltip.
- `CargoShipmentEditViewModel`: 5 firma kartı prop (Firma/İletişim/Adres/Tel/E-posta + HasXxx).

---

## Kargo Katip — Sprint 5 — Dashboard, Rapor, PDF Export (2026-06-26)

**Yapılanlar:**
- Kargo dashboard (gelen/giden sayıları, durum dağılımı, son sevkiyatlar).
- Kargo rapor ekranı (tarih filtreli).
- Kargo PDF export.

**Commit:** `c972eab`

---

## Sprint 7 — Permission-Based Startup, UI Temizliği (2026-06-27)

**Yapılanlar:**
- Permission-based startup: kullanıcı yetkisine göre açılış penceresi belirleme.
- Kargo navigasyonu iyileştirmesi.
- Uygulama ikonu (`AppIcon.ico`) eklendi.
- Nakit işlem kopyalama özelliği.

**Commit:** `5fc3717`

---

## Sprint 8 — Finans UI Polishing (2026-06-27)

**Yapılanlar:**
- Finans UI parlatması: DataGrid stilleri, renk uyumu.
- Menü yeniden düzenlemesi.
- Form UX iyileştirmeleri.
- Global stiller standardizasyonu.

**Commit:** `105745d`

---

## Kargo Stabilizasyon ve Mail Deadlock Fix (2026-06-27)

**Problem:** Kargo Mail Preview ekranı açılırken UI donuyordu.

**Root Cause:** `Result` / `.Wait()` kullanımı `async void` bağlamında deadlock yaratıyordu (WPF UI thread sync context üzerinde).

**Çözüm:** Tüm zincir `async/await` ile yeniden yazıldı; `.Result` ve `.Wait()` kaldırıldı.

**Commit:** `8a94daf`

Bkz. [`docs/04-Development/LessonsLearned.md`](../04-Development/LessonsLearned.md)

---

## Sprint 10 — Sistem Log ve Hata İzleme Paneli (2026-06-27/28)

**Yapılanlar:**
- `SystemLog` entity ve DB tablosu.
- `ISystemLogService` (Application), implementasyon (Infrastructure).
- Log seviyeleri: Info / Warning / Error / Critical.
- Kritik hatalarda Mail + System Log; normal hatalarda yalnızca System Log.
- Sistem Log izleme ekranı (filtrelenebilir, yönetici erişimli).
- `SmtpErrorNotificationService` sistem loguna bağlandı.

**Commit:** `a95fdb9`

---

## Sprint 11 — AES Şifre Koruması ve SMTP Ayarları (2026-06-28)

**Yapılanlar:**
- AES-256 ile ayar değerlerinin şifreli saklanması (`AesEncryptionService`).
- SMTP ayarları DB'ye seed edildi (şifreli).
- Ayarlar ekranı güncellendi.

**Commit:** `d2c9d70`

---

## Sprint 13 — Login Branding ve Tema Sistemi (2026-06-28/29)

**Yapılanlar:**
- `LoginWindow`: Logo (`LoginIcon.png`, 180×260), "Hoş geldiniz", versiyon, telif hakkı.
- Versiyon: ClickOnce ortamında assembly versiyonu, dev ortamında "Development".
- `IThemeService` (Application) + `ThemeService` (UI layer, WPF-specific singleton).
- `LightTheme.xaml` / `DarkTheme.xaml` (DynamicResource ile anlık değişim).
- Tema `UI:Theme` anahtarıyla `ApplicationSettings` tablosunda saklanıyor.
- `AppearanceSettingsWindow`: Görünüm Ayarları; koyu tema şimdilik devre dışı.

**Commit:** `89d0f71`

---

## Hotfix — Login Kullanıcı Adı Hatırla (2026-06-29)

**Yapılanlar:**
- `ILocalUserPreferencesService` (Application) + `LocalUserPreferencesService` (UI/Services).
- `%AppData%\YonetimFinansalIslemTakipSistemi\user-preferences.json` → `{ "LastUsername": "xxx" }`.
- **Kural:** Şifre asla kaydedilmez. Token/session asla kaydedilmez. Yalnızca kullanıcı adı.
- `LoginCompleted` callback'i `Action?` → `Func<Task>?` (awaitable).

**Commit:** `7759186`

---

## Hotfix — ClickOnce Masaüstü Kısayol İkonu (2026-06-29)

**Problem:** Masaüstü kısayolunun özel ikon göstermemesi.

**Araştırma Sonuçları:**
- EXE ikonu, taskbar ikonu ve pencere ikonu çalışıyor.
- `dotnet-mage -New Deployment -IconFile` → sadece Application tipi manifest için geçerli, Deployment için hata.
- `dotnet-mage -New Application -IconFile` → manifest'e mutlak yol yazıyor → "hatalı biçimlendirilmiş".
- Deployment manifest XML post-processing → ClickOnce runtime kabul etmiyor: "Dağıtım bildirimi simge dosyasının belirtimini kabul etmez."

**Karar:** Won't Fix. ClickOnce runtime kısıtı.

Bkz. [`docs/04-Development/LessonsLearned.md`](../04-Development/LessonsLearned.md)

---

## Hotfix — ClickOnce Türkçe Karakter Encoding (2026-06-29)

**Problem:** `$AppName = "Yönetim Finansal İşlem Takip Sistemi"` → PowerShell console encoding uyumsuzluğu → manifest'e mojibake yazılıyor (`YÃ¶netim Finansal Ä°ÅŸlem Takip Sistemi`) → "Uygulama hatalı biçimlendirilmiş."

**Çözüm:** `Publish-ClickOnce.ps1`'deki tüm mage teknik değerleri ASCII-only yapıldı.

```powershell
$AppName = "Yonetim Finansal Islem Takip Sistemi"
```

Publish klasörü her publish öncesi temizleniyor. Doğrulama bloğu: ASCII kontrolü + iconFile yasak referans kontrolü.

**Commit:** Bekliyor.

Bkz. [`docs/04-Development/LessonsLearned.md`](../04-Development/LessonsLearned.md)
