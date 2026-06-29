# Yetkilendirme

## Genel Model

Rol tabanlı değil, granüler permission tabanlı sistem. Her kullanıcıya bireysel izinler atanır. İzinler giriş sırasında DB'den yüklenir ve bellekte tutulur.

---

## PermissionType Enum

```csharp
public enum PermissionType
{
    CanManageUsers          = 1,
    CanManagePermissions    = 2,
    CanCreateTransaction    = 3,
    CanEditTransaction      = 4,
    CanDeleteTransaction    = 5,
    CanViewReports          = 6,
    CanManageExchangeRates  = 7,
    CanViewCargoModule      = 8,
    CanViewIncomingCargo    = 9,
    CanManageIncomingCargo  = 10,
    CanViewOutgoingCargo    = 11,
    CanManageOutgoingCargo  = 12,
    CanManageCompanyDirectory = 13,
    CanManageCargoCompanies = 14,
}
```

---

## Handler Seviyesinde Yetki Kontrolü

Her handler giriş noktasında `IUserContext.HasPermission()` kontrolü yapar:

```csharp
public async Task<OperationResult<...>> Handle(...)
{
    if (!_userContext.HasPermission(PermissionType.CanCreateTransaction))
        return OperationResult<...>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");
    // ...
}
```

UI katmanı handler'ın dönüş değerini okur; yetki hatalarında `ShowError` dialog gösterilir.

---

## UI Seviyesinde Görünürlük Kontrolü

Buton ve menü öğeleri yetki yoksa `Visibility.Collapsed` yapılır (gizlenir, `Disabled` değil):

```csharp
// MainWindow.Loaded
BtnNewTransaction.Visibility = _userContext.HasPermission(PermissionType.CanCreateTransaction)
    ? Visibility.Visible : Visibility.Collapsed;
```

Bu pattern; kargo liste ekranlarında Yeni/Düzenle/Sil butonları, menü öğeleri ve form butonlarında tutarlı uygulanır.

---

## Permission-Based Startup

Uygulama başlangıcında kullanıcının yetkilerine göre açılış penceresi belirlenir. Kullanıcı hiçbir modüle erişim yetkisi yoksa uygun bir bilgilendirme yapılır.

---

## Yetki Yönetimi Ekranı

`UserPermissionWindow`:
1. Kullanıcı listesinden seçim yapılır.
2. PermissionType listesi checkbox formatında gösterilir.
3. Kaydet → `UpdateUserPermissionsHandler`.

**Koruma:** Kullanıcı kendi `CanManagePermissions` iznini kaldıramaz (kilitlenme koruması).  
**Audit:** `PermissionUpdated` audit kaydı yazılır.

---

## DevDataSeeder

Geliştirme ortamında ilk kullanıcıyı ve izinleri seed eder.

**Upgrade-safe pattern:**

```csharp
var existing = await _repo.GetPermissionsAsync(userId);
var missing = Enum.GetValues<PermissionType>().Except(existing);
foreach (var p in missing)
    await _repo.AddPermissionAsync(userId, p);
```

Yeni bir `PermissionType` enum değeri eklendiğinde seeder otomatik olarak ekler; mevcut izinlere dokunmaz.

---

## Kullanıcı Koruma Kuralları

- **Son aktif kullanıcı silinemez:** `DeleteUserHandler` aktif kullanıcı sayısını kontrol eder.
- **Son aktif kullanıcı pasifleştirilemez:** Aynı koruma `UpdateUserHandler`'da da uygulanır.
- **Kendini silme:** Oturum açık kullanıcı kendi hesabını silemez.
