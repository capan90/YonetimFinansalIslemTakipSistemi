# İlerleme Kaydı

## Tamamlanan

- [x] Proje kurulumu ve klasör yapısı
- [x] Teknik dokümantasyon (Architecture, Database, Audit-log, Dialog-system, Update-flow)
- [x] Domain katmanı: BaseEntity, User, CashTransaction, TransactionType, CurrencyType
- [x] Application katmanı: ICashTransactionRepository, IUserRepository
- [x] Application katmanı: CreateCashTransaction use case (Request, Response, Handler, OperationResult)
- [x] Infrastructure katmanı: AppDbContext (EF Core + Npgsql), CashTransactionRepository, ServiceRegistration
- [x] EF Core migration: InitialCreate — cash_transactions tablosu PostgreSQL'de oluşturuldu
- [x] WPF DI composition root: AppDbContext, Repository ve Handler App.xaml.cs'te bağlandı
- [x] Uçtan uca doğrulama: CreateCashTransaction akışı PostgreSQL'e gerçek kayıt yazdı

- [x] CashTransaction listeleme ekranı: GetCashTransactionsQuery/Handler, CashTransactionDto, GetFilteredAsync
- [x] WPF liste UI: RelayCommand, CashTransactionListViewModel, MainWindow filtre paneli + DataGrid
- [x] Runtime doğrulama: uygulama açılıyor, liste yükleniyor, 5 filtre senaryosu DB seviyesinde geçti

- [x] Login ekranı: LoginWindow, LoginViewModel, IAuthenticationService, AuthResult
- [x] DB-backed authentication: UserConfiguration, UserRepository, DatabaseAuthenticationService (BCrypt)
- [x] EF Core migration: AddUsersTable — users tablosu PostgreSQL'de oluşturuldu
- [x] IDevDataSeeder + DevDataSeeder [DEV-ONLY]: ilk çalıştırmada admin seed kullanıcısı
- [x] Uçtan uca doğrulama: admin / Admin123! ile giriş yapıldı, MainWindow açıldı

- [x] Kullanıcı yönetimi: CreateUser, UpdateUser, DeleteUser, GetUsers (vertical slice)
- [x] IPasswordHasher + BcryptPasswordHasher (Clean Architecture: BCrypt Application dışında)
- [x] UserManagementWindow + UserFormWindow (liste, oluşturma, düzenleme, soft delete)
- [x] Son aktif kullanıcı silme / pasifleştirme koruması
- [x] Uçtan uca doğrulama: tüm CRUD ve hata senaryoları başarılı

- [x] IUserContext + UserContext singleton (session bağlamı)
- [x] CashTransaction oluşturma ekranı: CashTransactionFormViewModel, CashTransactionFormWindow
- [x] LoginViewModel → UserContext.Set() entegrasyonu
- [x] MainWindow'a "Yeni İşlem" butonu ve kayıt sonrası liste yenileme
- [x] Uçtan uca doğrulama: form açılıyor, kayıt DB'ye yazılıyor, liste yenileniyor

- [x] CashTransaction düzenleme: UpdateCashTransactionHandler, edit-mode form, toolbar Düzenle butonu
- [x] CashTransaction silme: DeleteCashTransactionHandler (soft-delete + audit), toolbar Sil butonu + onay
- [x] DateTime UTC fix: CreateCashTransaction ve UpdateCashTransaction handler'larında Npgsql timestamptz uyumu
- [x] Uçtan uca doğrulama: oluşturma, düzenleme, silme, liste yenileme — tüm senaryolar başarılı

- [x] Audit log sistemi: AuditLog entity, AuditAction enum, IAuditLogRepository/Service, AuditLogService
- [x] EF Core migration: AddAuditLogsTable — audit_logs tablosu PostgreSQL'de oluşturuldu
- [x] Audit enjeksiyonu: Create/Update/Delete CashTransaction + Create/Update/Delete User + Login
- [x] OldValues/NewValues okunabilir format — pipe-ayrılmış Türkçe string (JSON kaldırıldı)
- [x] Update audit diff — yalnızca değişen alanlar eski/yeni olarak yazılır
- [x] AuditLogWindow: kullanıcı / tarih aralığı / işlem tipi filtreli liste ekranı
- [x] MainWindow menü: Yönetim → Denetim Günlüğü

- [x] Dialog sistemi: IDialogService, DialogService, MessageDialog, ConfirmationDialog
- [x] MessageBox kullanımları dialog sistemine taşındı (nakit işlem silme + kullanıcı silme onayı)

## Sıradaki
- [ ] Yetki yönetimi
- [ ] Rapor ekranı
- [ ] Döviz ekranı
- [ ] ClickOnce güncelleme sistemi
- [ ] Logout butonu
