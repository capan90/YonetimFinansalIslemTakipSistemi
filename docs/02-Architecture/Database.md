# Veritabanı

## Genel Yapı

- PostgreSQL (merkezi, tüm istemciler aynı veritabanına bağlanır)
- Tablo adları: snake_case (EF Core konfigürasyonu ile)
- Soft-delete: `IsDeleted`, `DeletedAt`, `DeletedByUserId` alanları; global query filter aktif
- Tüm tarih alanları UTC olarak saklanır; UI katmanı yerel saate çevirir

---

## Tablolar

### `users`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | Guid | PK |
| FullName | string | Ad Soyad |
| UserName | string | Benzersiz kullanıcı adı |
| PasswordHash | string | BCrypt hash |
| IsActive | bool | Pasif kullanıcı girişi engellenir |
| IsDeleted | bool | Soft delete |
| CreatedAt | datetime (UTC) | |

### `user_permissions`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| UserId | Guid | FK → users |
| Permission | int | PermissionType enum değeri |

Composite PK: `(UserId, Permission)`

### `cash_transactions`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | Guid | PK |
| TransactionDate | datetime (UTC) | İşlem tarihi |
| TransactionType | int | 1=Giriş, 2=Çıkış |
| CurrencyType | int | 1=TRY, 2=USD, 3=EUR |
| Amount | decimal | Pozitif değer |
| Description | string | Zorunlu |
| CreatedByUserId | Guid | FK → users |
| IsDeleted | bool | Soft delete |

### `audit_logs`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | Guid | PK |
| Timestamp | datetime (UTC) | |
| UserId | Guid | |
| UserName | string | Anlık kopya |
| Action | int | AuditAction enum |
| EntityType | string | "CashTransaction" vs. |
| EntityId | string | |
| ComputerName | string | Hostname |
| OldValues | string | JSON/metin |
| NewValues | string | JSON/metin |

### `exchange_rates`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | Guid | PK |
| RateDate | datetime (UTC) | |
| CurrencyType | int | 2=USD, 3=EUR |
| ForexBuying | decimal | |
| ForexSelling | decimal | |

### `application_settings`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | Guid | PK |
| Key | string | Benzersiz ayar anahtarı |
| Value | string | Değer (hassaslar AES şifreli) |

Önemli anahtarlar: `SMTP:*`, `UI:Theme`

### `system_logs`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | Guid | PK |
| Timestamp | datetime (UTC) | |
| Level | string | Info/Warning/Error/Critical |
| Category | string | Modül adı |
| Message | string | |
| Exception | string? | Stack trace |
| UserId | Guid? | Oturumdaki kullanıcı |

### `user_grid_layouts`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | Guid | PK |
| UserId | Guid | FK → users |
| ScreenKey | string | Ekran tanımlayıcısı |
| LayoutJson | string | Kolon düzeni JSON |

### `company_directories`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | Guid | PK |
| Name | string | Firma adı |
| ContactPerson | string? | İletişim kişisi |
| Address | string? | |
| Phone | string? | |
| Email | string? | |
| IsDeleted | bool | |

### `cargo_companies`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | Guid | PK |
| Name | string | Kargo firması adı |
| TrackingUrlTemplate | string? | `{0}` ile takip no placeholder |
| IsDeleted | bool | |

### `cargo_shipments`
| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | Guid | PK |
| ShipmentNumber | string? | G-YYYY-XXXX / C-YYYY-XXXX; unique |
| Direction | int | 1=Gelen, 2=Giden |
| ShipmentType | int | CargoShipmentType enum |
| Status | int | CargoShipmentStatus enum |
| NotificationStatus | int | CargoNotificationStatus enum |
| CargoCompanyId | Guid? | FK → cargo_companies |
| CompanyDirectoryId | Guid? | FK → company_directories (giden: alıcı, gelen: gönderen) |
| TrackingNumber | string? | |
| VehiclePlate | string? | |
| ShipmentDate | datetime (UTC)? | |
| DeliveryDate | datetime (UTC)? | |
| Notes | string? | |
| ReceiverEmailSnapshot | string? | Bildirim anındaki e-posta kopyası |
| IsDeleted | bool | |

---

## Enum Değerleri

### TransactionType
```
1 = Giriş  (Borç — bakiyeyi artırır, yeşil)
2 = Çıkış  (Alacak — bakiyeyi azaltır, kırmızı)
```

### CurrencyType
```
1 = TRY
2 = USD
3 = EUR
```

### PermissionType
```
1  = CanManageUsers
2  = CanManagePermissions
3  = CanCreateTransaction
4  = CanEditTransaction
5  = CanDeleteTransaction
6  = CanViewReports
7  = CanManageExchangeRates
8  = CanViewCargoModule
9  = CanViewIncomingCargo
10 = CanManageIncomingCargo
11 = CanViewOutgoingCargo
12 = CanManageOutgoingCargo
13 = CanManageCompanyDirectory
14 = CanManageCargoCompanies
```

### CargoShipmentStatus
```
1 = Beklemede
2 = Yolda
3 = Teslim Edildi
4 = İptal Edildi
```

### CargoNotificationStatus
```
1 = Bildirilmedi
2 = WhatsApp Hazır
3 = Mail Hazır
4 = Bildirildi
```

---

## Bakiye Hesabı Kuralı

```
Net Bakiye = SUM(Giriş.Amount) - SUM(Çıkış.Amount)
```

Kümülatif bakiye hesabı handler'da tüm kayıtlar ASC sırayla çekilerek yapılır:

```csharp
// GetAllForBalanceAsync: TransactionDate / CreatedAt / Id ASC
// Handler: per-currency running total
balance += direction == FinancialDirection.Inflow ? amount : -amount;
```

---

## Migration Yönetimi

```powershell
# Yeni migration oluştur
dotnet ef migrations add <MigrationAdi> \
  --project src\YonetimFinansalIslemTakipSistemi.Infrastructure \
  --startup-project src\YonetimFinansalIslemTakipSistemi.Infrastructure

# Migration'ı uygula
dotnet ef database update \
  --project src\YonetimFinansalIslemTakipSistemi.Infrastructure \
  --startup-project src\YonetimFinansalIslemTakipSistemi.Infrastructure
```

**Kritik Kural:** Migration her zaman publish öncesinde uygulanır. Uygulama startup'ta migration çalıştırmaz.

---

## Acil Durum SQL Sorguları

Bkz. `docs/Emergency-SQL-Commands.md` (tam liste).

**Hızlı Referans:**

```sql
-- Anlık TRY bakiye
SELECT SUM(CASE WHEN "TransactionType"=1 THEN "Amount" ELSE -"Amount" END)
FROM cash_transactions WHERE "IsDeleted"=false AND "CurrencyType"=1;

-- Bugünkü işlem sayısı
SELECT COUNT(*) FROM cash_transactions
WHERE "IsDeleted"=false
AND DATE("TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul')
    = DATE(NOW() AT TIME ZONE 'Europe/Istanbul');

-- Son 10 audit kaydı
SELECT "Timestamp", "UserName", "Action", "EntityType"
FROM audit_logs ORDER BY "Timestamp" DESC LIMIT 10;

-- Tüm aktif kullanıcılar
SELECT "FullName", "UserName" FROM users
WHERE "IsActive"=true AND "IsDeleted"=false ORDER BY "FullName";
```
