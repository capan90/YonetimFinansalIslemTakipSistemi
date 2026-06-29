# Kullanıcı Yönetimi

## Genel Bakış

Kullanıcı hesapları yönetici tarafından manuel oluşturulur. Kullanıcılar kendi hesabını açamaz.

---

## Kullanıcı CRUD

| İşlem | Handler | İzin |
|-------|---------|------|
| Oluştur | CreateUserHandler | CanManageUsers |
| Güncelle | UpdateUserHandler | CanManageUsers |
| Sil (soft) | DeleteUserHandler | CanManageUsers |
| Listele | GetUsersHandler | CanManageUsers |

---

## Koruma Kuralları

**Son aktif kullanıcı koruması:**
- `DeleteUserHandler`: aktif kullanıcı sayısı 1 ise silme engellenir.
- `UpdateUserHandler`: aktif kullanıcı sayısı 1 ve `IsActive=false` yapılmak istenirse engellenir.

**Kendini silme:** Oturum açık kullanıcı kendi hesabını silemez.

**Silme:** Soft delete (`IsDeleted = true`, `DeletedAt`, `DeletedByUserId`). Veri veritabanında kalır.

---

## Parola Yönetimi

- `BCrypt.HashPassword(plainPassword)` ile hash üretilir.
- Plain-text şifre asla saklanmaz, iletilmez, loglanmaz.
- Parola sıfırlama: yönetici kullanıcı kaydını güncelleyerek yeni parola belirler.

---

## Audit Log

| Eylem | Audit Aksiyonu |
|-------|---------------|
| Kullanıcı oluşturma | UserCreated |
| Kullanıcı güncelleme | UserUpdated |
| Kullanıcı silme | UserDeleted |

---

## Yetki Yönetimi Ekranı

`UserPermissionWindow`:
1. Kullanıcı listesinden seçim yapılır.
2. `PermissionType` listesi checkbox formatında gösterilir.
3. "Kaydet" → `UpdateUserPermissionsHandler`.

**Kilitlenme koruması:** Kullanıcı kendi `CanManagePermissions` iznini kaldıramaz.

**Audit:** `PermissionUpdated` audit kaydı yazılır.

---

## DevDataSeeder

Development ortamında ilk kullanıcıyı ve tüm izinleri seed eder.

Upgrade-safe: her sürümde yeni `PermissionType` değerleri otomatik olarak eksik kullanıcılara eklenir.

```csharp
var missing = Enum.GetValues<PermissionType>().Except(existingPermissions);
// → her yeni permission eklendiğinde seeder otomatik seed eder
```
