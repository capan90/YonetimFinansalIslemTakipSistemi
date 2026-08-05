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

## WPF Tema ve Renk

Üç kural. Üçü de otomatik testle korunur
(`tests/YonetimFinansalIslemTakipSistemi.UiTests`); ihlal build'i değil testi kırar.

### 1. XAML'de ham renk yazılmaz

Yalnızca `{DynamicResource Theme.*}`. Ham hex, `White`/`Black` gibi adlandırılmış
sistem renkleri ve `StaticResource` ile renk bağlama yasaktır — hiçbiri tema
değişiminde güncellenmez.

```xml
<!-- YANLIŞ -->
<TextBlock Foreground="#1A202C"/>
<Border Background="White"/>

<!-- DOĞRU -->
<TextBlock Foreground="{DynamicResource Theme.Text}"/>
<Border Background="{DynamicResource Theme.Surface}"/>
```

`Transparent` serbesttir — renk değil, "yok" demektir.

Bilinçli istisnalar `docs/02-Architecture/ColorTokenMap.md` içinde listelidir ve
`RawColorScanTests.IsKnownException` ile kodda sabitlenmiştir.

### 2. Temalı bir tipi hedefleyen her yerel stil `BasedOn` alır

`BasedOn` almayan bir `Style`, kontrolü uygulamanın örtük tema stilinden **tamamen**
koparır ve WPF'in yerleşik (sabit renkli) stiline düşürür. Belirti "bir pencerede
kontroller temaya uymuyor" şeklinde çıkar; ham renk taramasıyla **yakalanmaz**,
çünkü ortada yanlış token yoktur — hiç token yoktur.

```xml
<!-- YANLIŞ: kontrol temadan kopar -->
<Style x:Key="InputStyle" TargetType="TextBox">
    <Setter Property="Height" Value="32"/>
</Style>

<!-- DOĞRU -->
<Style x:Key="InputStyle" TargetType="TextBox"
       BasedOn="{StaticResource {x:Type TextBox}}">
    <Setter Property="Height" Value="32"/>
</Style>
```

Kod tarafında aynı tuzak: `new Style(typeof(X))` tek argümanlı biçimi `BasedOn`
taşımaz. İkinci argüman verilmelidir.

```csharp
// YANLIŞ
var style = new Style(typeof(DataGridColumnHeader));

// DOĞRU
var baseStyle = (Style?)TryFindResource(typeof(DataGridColumnHeader));
var style     = new Style(typeof(DataGridColumnHeader), baseStyle);
```

### 3. Converter renk döndürmez

Converter bağlama başına **bir kez** çalışır; döndürdüğü `Brush` örneği o bağlamada
donar. Tema değiştiğinde sözlükteki fırça değişir ama ekranda duran eski örnek
yerinde kalır.

Renk `DataTrigger` + `DynamicResource` ile verilir — setter içindeki
`DynamicResource` tema değişiminde anında güncellenir.

```xml
<!-- YANLIŞ -->
<TextBlock Foreground="{Binding Level, Converter={StaticResource LevelToBrush}}"/>

<!-- DOĞRU -->
<Style x:Key="LevelText" TargetType="TextBlock">
    <Setter Property="Foreground" Value="{DynamicResource Theme.Text}"/>
    <Style.Triggers>
        <DataTrigger Binding="{Binding Level}" Value="Critical">
            <Setter Property="Foreground" Value="{DynamicResource Theme.Critical.Text}"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

Code-behind'da renk atarken karşılığı `SetResourceReference`'tır:

```csharp
// YANLIŞ — fırçayı kopyalar, tema değişimini görmez
label.Foreground = ThemeBrush.Get("Theme.Success");

// DOĞRU — dinamik referans kurar
ThemeBrush.Apply(label, TextBlock.ForegroundProperty, "Theme.Success");
```

### Ek: FontSize serbest yazılmaz

`AppStyles.xaml`'deki altı tipografi stilinden biri kullanılır:
`Text.Display` / `Text.H1` / `Text.H2` / `Text.Body` / `Text.BodyStrong` /
`Text.Caption`.

### Ek: Yeni kontrol tipi kullanırken

Bir ekranda ilk kez `TabControl`, `Expander`, `TreeView` gibi bir tip kullanılıyorsa
`Resources/Controls.xaml`'de şablonu yoksa **tema körüdür**. Önce şablonu yazılır,
sonra `ControlStateMatrixTests`'e satırı eklenir.

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
