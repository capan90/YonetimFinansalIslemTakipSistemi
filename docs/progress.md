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

## Sıradaki

- [ ] Ana pencere iskelet ve navigasyon (UI)
- [ ] Kullanıcı yönetimi ekranı (admin tarafından kullanıcı oluşturma)
