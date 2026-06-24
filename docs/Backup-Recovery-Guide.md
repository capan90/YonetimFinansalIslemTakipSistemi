# Backup ve Kurtarma Kılavuzu

## Gereksinimler

- PostgreSQL istemci araçları (`pg_dump`, `pg_restore`, `psql`) kurulu ve PATH'te olmalıdır.
- Windows: `C:\Program Files\PostgreSQL\<sürüm>\bin` PATH'e eklenmeli.
- Doğrulama: PowerShell'de `pg_dump --version` çalışmalıdır.

---

## Backup Alma

### Otomatik (Script)

Proje kökünden:

```powershell
.\scripts\Backup-Database.ps1
```

Varsayılan çıktı: `Backups\yonetim_db_20260624_143000.backup`

#### Özel konum veya sunucu:

```powershell
.\scripts\Backup-Database.ps1 -BackupDirectory "D:\DBBackups"
.\scripts\Backup-Database.ps1 -Host prod-server -Username yonetim_app -BackupDirectory "\\nas\yonetim-backups"
```

### Manuel (pg_dump)

```powershell
$env:PGPASSWORD = "SIFRE"
pg_dump --host=localhost --port=5432 --username=postgres --dbname=yonetim_db --format=custom --file="yonetim_db_backup.backup"
$env:PGPASSWORD = ""
```

---

## Backup Dosyası Konumu

| Durum | Konum |
|-------|-------|
| Script varsayılan | `<proje-kökü>\Backups\` |
| Özel parametre | `-BackupDirectory` ile belirtilen klasör |

Dosya adı formatı: `yonetim_db_yyyyMMdd_HHmmss.backup`

> **Not:** `Backups/` klasörü ve `*.backup` dosyaları `.gitignore` ile versiyon kontrolünden hariç tutulmuştur.
> Backup dosyalarını ayrı bir fiziksel konuma veya ağ sürücüsüne kopyalayın.

---

## Migration Öncesi Backup Kuralı

**Her migration öncesinde backup zorunludur.**

Güvenli akış için:

```powershell
.\scripts\Backup-And-Migrate.ps1
```

Bu script:
1. Önce backup alır.
2. Backup başarısız ise migration **çalıştırmaz**.
3. Backup başarılı ise `dotnet ef database update` çalıştırır.

> Manuel migration çalıştırmadan önce her zaman `Backup-Database.ps1` çalıştırın.

---

## Restore

### Gerekli Ön Hazırlık

1. **Tüm kullanıcıları uygulamadan çıkarın.** Açık bağlantılar restore'u engelleyebilir.
2. Restore yapılacak backup dosyasının tam yolunu not edin.
3. Hedef veritabanının var olduğundan emin olun.

### Script ile Restore

```powershell
.\scripts\Restore-Database.ps1 -BackupFile "Backups\yonetim_db_20260624_143000.backup"
```

Script çalışmadan önce tüm bağlantı bilgilerini gösterir ve **"RESTORE"** yazmanızı ister.

### Sıfırdan Restore (Veritabanı Yeniden Oluşturarak)

Mevcut verilerle çakışma durumunda önce veritabanını yeniden oluşturun:

```sql
-- psql ile bağlanın (postgres kullanıcısı veya superuser):
DROP DATABASE IF EXISTS yonetim_db;
CREATE DATABASE yonetim_db;
```

Ardından restore scriptini çalıştırın.

### Manuel pg_restore

```powershell
$env:PGPASSWORD = "SIFRE"
pg_restore --host=localhost --port=5432 --username=postgres --dbname=yonetim_db --verbose "Backups\yonetim_db_20260624_143000.backup"
$env:PGPASSWORD = ""
```

---

## Şifre Güvenliği

Scriptler şifreyi şu sırayla arar:
1. `PGPASSWORD` ortam değişkeni
2. `YONETIM_DB_CONNECTION` ortam değişkeni (Npgsql connection string)
3. `appsettings.json` (sadece geliştirme ortamı)

Şifre **hiçbir zaman komut satırı parametresi olarak geçirilmez** (process listesinde görünmez).

Üretimde tavsiye edilen:

```powershell
$env:PGPASSWORD = "production-sifre"
.\scripts\Backup-Database.ps1
$env:PGPASSWORD = ""
```

---

## Backup Doğrulama

Backup dosyasının geçerli olduğunu doğrulamak için:

```powershell
pg_restore --list "Backups\yonetim_db_20260624_143000.backup"
```

Bu komut veriyi restore etmez; sadece backup içeriğini listeler.
