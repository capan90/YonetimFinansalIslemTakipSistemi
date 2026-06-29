# Kodlama Standartları

## Genel Kurallar

- Clean Architecture katman sınırlarına uyulur.
- Her use case kendi klasöründe yaşar (feature-based).
- Handler'lar tek sorumluluk ilkesine uyar.
- SQL UI katmanına karışmaz.
- WPF kodu Application/Infrastructure'a karışmaz.

---

## Adlandırma

| Yapı | Kural | Örnek |
|------|-------|-------|
| Handler | `VerbEntityHandler` | `CreateCashTransactionHandler` |
| Command/Query | `VerbEntityCommand` | `CreateCashTransactionCommand` |
| Repository Interface | `IEntityRepository` | `ICashTransactionRepository` |
| ViewModel | `EntityActionViewModel` | `CashTransactionListViewModel` |
| Window | `EntityActionWindow` | `CashTransactionFormWindow` |
| DB Tablo | snake_case | `cash_transactions` |
| C# Property | PascalCase | `TransactionDate` |

---

## Yorum Kuralları

Yorum varsayılan olarak yazılmaz. Şu durumlarda gereklidir:

- **İş kuralı:** Neden bu hesaplama yapılıyor?
- **Audit tetikleyici:** Hangi koşulda audit yazılıyor?
- **Bakiye hesabı:** Kümülatif hesap mantığı
- **Güncelleme akışı:** ClickOnce / version.json mantığı
- **WPF özel durum:** Namespace çakışması, DynamicResource kısıtı
- **Güvenlik kararı:** Şifre asla kaydedilmez gibi

```csharp
// Şifre kaydedilmez — sadece kullanıcı adı
await _prefService.SaveLastUsernameAsync(_vm.UserName);

// Filtre in-memory; tarih filtresi altında bile bakiye gerçek tarihsel değeri yansıtır
var allRecords = await _repo.GetAllForBalanceAsync();
```

---

## Async Pattern

```csharp
// DOĞRU — async/await all the way down
public async Task<OperationResult<T>> HandleAsync(...)
{
    var result = await _repository.GetAsync(...);
    await _auditService.LogAsync(...);
    return OperationResult<T>.Success(result);
}

// YANLIŞ — deadlock riski (WPF UI thread)
var result = _repository.GetAsync(...).Result;
_auditService.LogAsync(...).Wait();
```

WPF UI thread'inde `.Result` veya `.Wait()` kullanmak `SynchronizationContext` üzerinde deadlock yaratır.

---

## OperationResult Pattern

```csharp
// Application katmanı dönüş tipi
public record OperationResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? ErrorMessage { get; init; }

    public static OperationResult<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static OperationResult<T> Fail(string message) => new() { IsSuccess = false, ErrorMessage = message };
}
```

---

## Soft Delete

Silme işlemleri her zaman soft delete:

```csharp
entity.IsDeleted = true;
entity.DeletedAt = DateTime.UtcNow;
entity.DeletedByUserId = _userContext.UserId;
```

EF Core global query filter: `builder.HasQueryFilter(e => !e.IsDeleted);`

Silinmiş kayıtları dahil etmek için: `dbSet.IgnoreQueryFilters()`

---

## Tarih/Saat

- Veritabanına her zaman **UTC** kaydedilir: `DateTime.UtcNow`
- UI'da kullanıcıya gösterilirken yerel saate çevrilir: `dateTime.ToLocalTime()`
- Rapor tarih aralığı: UTC yarı-açık aralık → `>= start AND < end.AddDays(1)`

---

## EF Core Konfigürasyon

```csharp
// Infrastructure/Configurations/CashTransactionConfiguration.cs
public class CashTransactionConfiguration : IEntityTypeConfiguration<CashTransaction>
{
    public void Configure(EntityTypeBuilder<CashTransaction> builder)
    {
        builder.ToTable("cash_transactions");
        builder.HasQueryFilter(e => !e.IsDeleted);
        // ...
    }
}
```

---

## WPF RelayCommand

```csharp
// Async command pattern
public ICommand SaveCommand { get; }
SaveCommand = new RelayCommand(async () => await SaveAsync());

// Harici MVVM paketi yok — kendi RelayCommand implementasyonu
```

---

## DI Singleton + Scoped Anti-Pattern Çözümü

Singleton bir servis scoped servise ihtiyaç duyduğunda:

```csharp
// Singleton serviste
private readonly IServiceScopeFactory _scopeFactory;

public async Task DoWorkAsync()
{
    using var scope = _scopeFactory.CreateScope();
    var repo = scope.ServiceProvider.GetRequiredService<IRepository>();
    await repo.DoSomethingAsync();
}
```

Doğrudan inject etme — Singleton, Scoped'u DI container'dan doğrudan alamaz (scope validation hatası veya stale data riski).
