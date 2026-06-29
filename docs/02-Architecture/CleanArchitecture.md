# Clean Architecture

## Genel Bakış

Proje, klasik Clean Architecture (Onion Architecture) katmanlı yapısını izler. Bağımlılık yönü dıştan içe doğrudur: UI → Infrastructure → Application → Domain.

```
┌─────────────────────────────────────────┐
│                  UI                     │ WPF, ViewModels, App.xaml.cs
│  ┌───────────────────────────────────┐  │
│  │          Infrastructure           │  │ EF Core, Repositories, Mail, Audit
│  │  ┌─────────────────────────────┐  │  │
│  │  │        Application          │  │  │ Use Case Handler'lar, Interfaces
│  │  │  ┌───────────────────────┐  │  │  │
│  │  │  │        Domain         │  │  │  │ Entity'ler, Enum'lar
│  │  │  └───────────────────────┘  │  │  │
│  │  └─────────────────────────────┘  │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

---

## Katmanlar

### Domain (`YonetimFinansalIslemTakipSistemi.Domain`)

Bağımlılığı yoktur. Saf iş modeli.

**İçerik:**
- `BaseEntity<TId>`: `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt` soft-delete alanları
- Entity'ler: `User`, `CashTransaction`, `AuditLog`, `UserPermission`, `ExchangeRate`, `ApplicationSettings`, `SystemLog`, `CompanyDirectory`, `CargoCompany`, `CargoShipment`
- Enum'lar: `TransactionType`, `CurrencyType`, `AuditAction`, `PermissionType`, `FinancialDirection`, `CargoShipmentDirection`, `CargoShipmentType`, `CargoShipmentStatus`, `CargoNotificationStatus`

**Kural:** Domain katmanında infrastructure veya UI referansı olmamalıdır.

---

### Application (`YonetimFinansalIslemTakipSistemi.Application`)

Yalnızca Domain'e bağımlıdır.

**İçerik:**

```
Application/
├── Features/
│   ├── CashTransactions/
│   │   ├── Commands/CreateCashTransaction/
│   │   ├── Commands/UpdateCashTransaction/
│   │   ├── Commands/DeleteCashTransaction/
│   │   └── Queries/GetCashTransactions/
│   ├── Users/
│   ├── Reports/
│   ├── Cargo/
│   │   ├── Commands/CreateCargoShipment/
│   │   └── ...
│   └── ...
├── Interfaces/
│   ├── Repositories/
│   │   ├── ICashTransactionRepository
│   │   ├── IUserRepository
│   │   └── ...
│   └── Services/
│       ├── IAuthenticationService
│       ├── IAuditLogService
│       ├── ISystemLogService
│       ├── IUpdateService
│       ├── IThemeService
│       └── ILocalUserPreferencesService
└── Common/
    ├── OperationResult<T>
    └── CargoStatusTransitions
```

**Kural:** Application katmanında EF Core, WPF veya dış servis referansı olmamalıdır.

**Handler Pattern:**

```csharp
// Her use case: Request → Handler → OperationResult<Response>
public class CreateCashTransactionHandler
{
    // 1. Yetki kontrolü
    if (!_userContext.HasPermission(PermissionType.CanCreateTransaction))
        return OperationResult<...>.Fail("Yetki yok.");
    
    // 2. Validasyon
    // 3. Entity oluşturma
    // 4. Repository persist
    // 5. Audit log
    return OperationResult<...>.Success(...);
}
```

---

### Infrastructure (`YonetimFinansalIslemTakipSistemi.Infrastructure`)

Domain ve Application'a bağımlıdır.

**İçerik:**
- `AppDbContext`: EF Core context, tüm DbSet'ler
- Entity konfigürasyonları (`EntityTypeConfiguration<T>`): snake_case tablo adları, soft-delete global filter
- Repository implementasyonları
- `ServiceRegistration`: Infrastructure DI kayıtları
- `AppDbContextFactory`: yalnızca `dotnet ef` CLI için (üretim kodunda kullanılmaz)
- `SmtpErrorNotificationService`, `AesEncryptionService`, `QuestPdfLabelRenderer`

**Soft Delete Pattern:**

```csharp
builder.HasQueryFilter(e => !e.IsDeleted);
// Silinmiş kayıtları dahil et: repository.IgnoreQueryFilters()
```

---

### UI (`YonetimFinansalIslemTakipSistemi.UI`)

Tüm diğer katmanlara bağımlıdır.

**İçerik:**
- `App.xaml.cs`: DI composition root, startup/shutdown yönetimi
- WPF pencereleri (`.xaml` + `.xaml.cs`)
- ViewModel'ler
- `UI/Services/`: WPF'e özgü servis implementasyonları (ThemeService, UpdateService, LocalUserPreferencesService)
- `UI/Common/`: RelayCommand, WPF utilities
- `UI/Abstractions/`: UI katmanına özgü interface'ler

**Önemli:** `System.Windows.Application` ile `YonetimFinansalIslemTakipSistemi.Application` namespace'i çakışır. WPF ile çalışırken `System.Windows.Application.Current` tam niteleme zorunludur.

---

## Bağımlılık Enjeksiyonu

`App.xaml.cs`'te `IServiceCollection` ile tüm servisler kayıt edilir.

**Yaşam döngüsü kuralları:**

| Servis | Lifetime | Neden |
|--------|----------|-------|
| `AppDbContext` | Scoped | EF Unit of Work pattern |
| Handler'lar | Scoped | DbContext bağımlılığı |
| `UserContext` (singleton) | Singleton | Oturum bilgisi |
| `ThemeService` | Singleton | WPF Application.Current erişimi |
| `SmtpService` | Singleton | Mail cooldown state |

**Singleton → Scoped anti-pattern çözümü:**  
Singleton servisler `IServiceScopeFactory` kullanarak `IServiceScope` oluşturur ve scoped servisler oradan çözümlenir.

---

## Dialog Sistemi

`IDialogService` aracılığıyla merkezi dialog yönetimi. Tüm iş akışlarında `MessageBox` yerine bu sistem kullanılır.

```csharp
// Tipler
_dialogService.ShowInfo(title, message)
_dialogService.ShowSuccess(title, message)
_dialogService.ShowWarning(title, message)
_dialogService.ShowError(title, message)
_dialogService.ShowConfirmation(title, message) // → bool
```

Her dialog: başlık, mesaj, ikon, renk ve uygun butonlar içerir.

---

## OperationResult Pattern

Handler'lar `OperationResult<T>` döner. UI bu sonucu dialog tipini belirlemek için kullanır.

```csharp
OperationResult<T>.Success(value)
OperationResult<T>.Fail("Hata mesajı")

// UI'da
if (!result.IsSuccess)
{
    _dialogService.ShowError("İşlem Hatası", result.ErrorMessage);
    return;
}
_dialogService.ShowSuccess("Başarılı", "İşlem kaydedildi.");
```
