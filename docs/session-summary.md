# Oturum Özeti

## Oturum — 2026-07-22 — Revizyon Sprinti (Harf Tercihi, Kargo No, WhatsApp Rehberi, Portal URL)

### Yapılanlar

**1. Kullanıcı bazlı harf tercihi**
- `TextCasePreference` (Olduğu Gibi / BÜYÜK HARF / küçük harf) — kullanıcı bazlı, `user_preferences` tablosunda kalıcı.
- Merkezi `IUserTextNormalizationService`: tr-TR dönüşüm; kural tek noktada, UI'da zorlanmaz. Tercih login'de oturuma yüklenir, kaydetmede anında etkinleşir.
- Handler'larda alan bazlı uygulandı (reflection yok): nakit açıklama, firma rehberi metin alanları, kargo kişi/plaka/not alanları, dikkatine, kargo firması ad/not, WhatsApp rehber alanları. E-posta/telefon/URL/kod alanları muaf.
- Ayarlar → Harf Duyarlılığı ekranı; `UserPreferenceUpdated` audit.
- Davranış değişikliği: eski zorunlu TitleCase/UPPER kaldırıldı — varsayılan "Olduğu Gibi".

**2. Otomatik kargo numarası**
- Yeni format: Gelen `GLN00001`, Giden `GDN00001`; yönler bağımsız sayaç kullanır.
- `cargo_number_counters` + `AddWithAutoNumberAsync`: atomik `ON CONFLICT ... RETURNING`, insert ile aynı transaction — eşzamanlı mükerrer imkansız, rollback'te numara boşa gitmez; silinen numara asla geri kullanılmaz.
- Numara kullanıcıdan alınmaz (request'lerden kaldırıldı), UI salt okunur. Eski `G/C-YYYY-NNNN` numaralar aynen korunur.
- ADR-006 eklendi.

**3. Ortak WhatsApp rehberi**
- `whatsapp_contacts` (soft delete, normalize telefon unique). `PhoneNumberNormalizer`: tüm yazımlar → `+905XXXXXXXXX`.
- Yönetim ekranı (arama/firma filtresi/pasifler), bildirim önizlemede aranabilir çoklu seçim + chip + `+` hızlı ekleme, kişi seçiliyken telefon salt okunur.
- Toplu gönderim: kişi başına ayrı wa.me açılışı + başarı/başarısızlık raporu + audit'te alıcı listesi.
- Mükerrer numara: anlaşılır uyarı + mevcut kişi adı; soft delete edilmiş numara geri yüklenir.

**4. Kargo portal bağlantısı**
- `CargoCompany.PortalUrl` (http/https doğrulamalı, opsiyonel). Kargo düzenlemede salt okunur gösterim + "Portalı Aç".
- Yurtiçi Kargo varsayılan URL'i migration ile (mevcutsa boş alan doldurulur, yoksa eklenir); kod içinde hard-code yok.

**Migration:** `AddUserPrefsWhatsAppDirectoryAndCargoCounters` — canlı veri silinmez; yalnızca kolon/tablo ekleme + boş alan doldurma. DB'ye uygulanmadı.

**Revizyon düzeltmeleri (aynı gün):**
- Harf Duyarlılığı artık Yardım → Kullanıcı Ayarlarım altından yetki gerektirmeden erişilir (Ayarlar'daki giriş korunur; aynı pencere/handler).
- Silinen SON kargo numarası kontrollü geri kullanılır: koşullu sayaç geri alma + soft delete tek transaction'da; aradaki silinmiş numaralar asla geri dönmez (ADR-006 güncellendi). Schema değişikliği yok — ek migration gerekmedi.

**Build/Test:** 0 hata, 0 uyarı; 78/78 test başarılı.

## Oturum 4b — 2026-06-24 — Kargo Katip Sprint 1.1 Stabilizasyon

### Yapılanlar
- **DevDataSeeder** — değiştirilmedi; upgrade-safe `Enum.GetValues<PermissionType>().Except(existing)` pattern'i 7 yeni izni otomatik seed eder.
- **Kargo listesi butonları**: Yeni/Düzenle/Sil → manage izni yoksa `Visibility.Collapsed` (CargoShipmentList + CompanyDirectoryList + CargoCompanyList).
- **Migration** `AddCargoClerkModule` DB'ye uygulandı.
- **Build**: 0 hata, 0 uyarı.

## Oturum 4 — 2026-06-24 — Kargo Katip Sprint 1

### Yapılanlar

**Domain:**
- `CompanyDirectory`, `CargoCompany`, `CargoShipment` entity'leri eklendi (BaseEntity kalıtımı, soft delete).
- 4 yeni enum: `CargoShipmentDirection`, `CargoShipmentType`, `CargoShipmentStatus`, `CargoNotificationStatus`.
- `PermissionType` enum'una 7 yeni izin eklendi (8–14).
- `AuditAction` enum'una 9 yeni aksiyon eklendi.

**Application:**
- 3 repository interface: `ICompanyDirectoryRepository`, `ICargoCompanyRepository`, `ICargoShipmentRepository`.
- 15 handler: CompanyDirectory (5), CargoCompany (5), CargoShipment (5) — Create/Update/Delete/GetList/GetById.
- Tüm handler'lar mevcut pattern'e uygun: permission check → validation → entity → persist → audit.
- `CargoShipmentListHandler`: gelen/giden ayrı permission ile korunur, navigation property'ler include edilir.

**Infrastructure:**
- 3 EF konfigürasyon: `company_directories`, `cargo_companies`, `cargo_shipments` (soft delete filter, FK ilişkileri).
- 3 repository: `CompanyDirectoryRepository`, `CargoCompanyRepository`, `CargoShipmentRepository`.
- `AppDbContext`'e 3 yeni `DbSet` eklendi.
- `ServiceRegistration`'a repository kayıtları eklendi.
- Migration oluşturuldu: `AddCargoClerkModule` (2026-06-24).

**UI:**
- 4 ViewModel: `CompanyDirectoryList/Edit`, `CargoCompanyList/Edit`.
- 1 ortak list VM: `CargoShipmentListViewModel` (direction parametresiyle çalışır).
- 1 edit VM: `CargoShipmentEditViewModel` (giden kargoda firma seçince alıcı otomatik dolar).
- 8 View: XAML + code-behind (list + edit ekranları her modül için).
- `MainWindow.xaml`'a "Kargo Katip" menüsü eklendi.
- `MainWindow.xaml.cs`'e 4 click handler ve permission-based visibility eklendi.
- `App.xaml.cs`'e 12 handler + 5 ViewModel DI kaydı eklendi.

**Build:** 0 hata, 0 uyarı.

## Oturum 3 — 2026-06-18

### Yapılanlar
- `GetCashTransactionsQuery`, `CashTransactionDto`, `GetCashTransactionsHandler` Application katmanına eklendi.
- `ICashTransactionRepository`'ye `GetFilteredAsync` eklendi; opsiyonel filtreler LINQ zinciriyle birleştirildi.
- `CashTransactionRepository.GetFilteredAsync` tarih/tür/para birimi filtrelerini ve `OrderByDescending(TransactionDate)` sıralamasını DB seviyesinde uygular.
- `RelayCommand` ve `CashTransactionListViewModel` UI katmanına eklendi (harici MVVM paketi olmadan).
- `MainWindow` filtre paneli (DatePicker × 2, ComboBox × 2) ve DataGrid ile güncellendi.
- DI scope, `MainWindow.Closed` olayına bağlandı — DbContext pencere ömrünü aşmaz.
- Commit: `051b892` — `feat(ui): add cash transaction list screen with filtering`
- Runtime doğrulama: uygulama açıldı, 5 filtre senaryosu (tür, para birimi, tarih aralığı, kombine) DB seviyesinde doğrulandı.

## Oturum 1-2 — 2026-06-18

### Yapılanlar
- Domain katmanı src/ altına taşındı, orphan klasör temizlendi.
- App.xaml.cs'teki Application namespace çakışması düzeltildi.
- Application katmanı feature-based yapıya oturtuldu (Interfaces, Features, Common).
- CreateCashTransaction use case yazıldı (Request, Response, Handler, OperationResult).
- Hafif dokümantasyon yapısı kuruldu (progress.md, session-summary.md, decisions/).
- Infrastructure katmanı tamamlandı: AppDbContext, CashTransactionConfiguration, CashTransactionRepository, AppDbContextFactory, ServiceRegistration.
- EF Core migration oluşturuldu ve PostgreSQL'e uygulandı (cash_transactions tablosu).
- WPF DI composition root bağlandı: App.xaml.cs'te AppDbContext + Repository + Handler.
- Uçtan uca akış doğrulandı: CreateCashTransaction → PostgreSQL kaydı gerçekleşti.
- Commit'ler: `6f893d0` (core), `d6e4ab6` (refactor), `2ff6f08` (infrastructure)

### Açık Noktalar
- Login ekranı ve ana pencere navigasyonu henüz yok.
- User entity için migration ve IUserRepository implementasyonu yapılmadı.
- Connection string şu an env variable / hardcode fallback; ileride config dosyasına taşınacak.
- DB'de 3 adet test kaydı var (Ödeme/USD, Avans/EUR, Transfer/TRY) — istenirse silinebilir.

### Dikkat
- `OperationResult<T>` UI dialog tipini (Success/Error) belirlemek için kullanılır.
- Handler `DateTime.UtcNow` kullanır; UI katmanı yerel saate çevirmeyi üstlenir.
- `AppDbContextFactory` yalnızca `dotnet ef` CLI araçları içindir; üretim kodunda çağrılmaz.
- EF Core Design paketi `PrivateAssets="all"` ile işaretli — çalışma zamanında dağıtılmaz.
