# Felaket Kurtarma Planı

Her senaryo için uygulanabilir adımlar. Sakinliği koruyun, adımları sırayla uygulayın.

---

## Senaryo 1: Uygulama Açılmıyor

**Belirtiler:** Uygulama başlar başlamaz kapanıyor, beyaz ekran, veya hata penceresi.

**Adımlar:**
1. Log dosyasını kontrol edin: `<kurulum-dizini>\logs\app-YYYYMMDD.log`
2. `[FTL]` (Fatal) satırlarını bulun — başlangıç hatası nedeni orada yazar.
3. Olası nedenler ve çözümler:

| Log mesajı | Çözüm |
|-----------|-------|
| "Veritabanına bağlanılamadı" | Senaryo 2'ye bakın |
| "Bağlantı dizesi yapılandırılmamış" | `appsettings.json` dosyasını kontrol edin |
| "appsettings.json bulunamadı" | Dosyanın uygulama dizininde olduğunu doğrulayın |

4. ClickOnce güncelleme hatasıysa: Denetim Masası → Program Ekle/Kaldır → uygulamayı kaldırın ve yeniden yükleyin.

---

## Senaryo 2: Veritabanı Sunucusuna Erişilemiyor

**Belirtiler:** Uygulama "Veritabanı bağlantısı kurulamadı" hatası veriyor.

**Adımlar:**
1. PostgreSQL sunucusuna ping atın: `ping <sunucu-adresi>`
2. Port açık mı kontrol edin: `Test-NetConnection <sunucu-adresi> -Port 5432`
3. Sunucuya RDP veya fiziksel erişimle bağlanın.
4. PostgreSQL servisini kontrol edin: Senaryo 3'e bakın.
5. Güvenlik duvarı kurallarını kontrol edin (5432 portu açık olmalı).
6. `appsettings.json` veya `YONETIM_DB_CONNECTION` env var'ındaki bağlantı bilgilerini doğrulayın.

---

## Senaryo 3: PostgreSQL Servisi Durmuş

**Belirtiler:** Sunucuya erişilebiliyor ama PostgreSQL yanıt vermiyor.

**Adımlar:**

Windows sunucusunda:
```powershell
# Servis durumunu kontrol et
Get-Service postgresql*

# Servisi başlat
Start-Service postgresql-x64-16   # sürüm numarasını ortama göre uyarlayın

# Servisin başlamasını bekle
Start-Sleep -Seconds 5
Get-Service postgresql*
```

PostgreSQL logunu kontrol edin:
```
C:\Program Files\PostgreSQL\<sürüm>\data\log\
```

Disk doluluğunu kontrol edin — PostgreSQL disk dolduğunda başlamayı reddeder:
```powershell
Get-PSDrive C | Select-Object Used, Free
```

---

## Senaryo 4: Yanlış Migration Uygulandı

**Belirtiler:** Migration sonrası uygulama çöküyor, veriler eksik veya bozuk.

**Ön koşul:** Migration öncesi backup alınmış olmalıdır (bkz. Backup-Recovery-Guide.md).

**Adımlar:**
1. Uygulamayı hemen kapatın — yeni veri girişini engelleyin.
2. Son başarılı migration öncesi alınan backup'ı bulun: `Backups\` klasörüne bakın.
3. Restore işlemi için tüm kullanıcıları uygulamadan çıkarın.
4. Restore scriptiyle geri dönün:

```powershell
.\scripts\Restore-Database.ps1 -BackupFile "Backups\yonetim_db_20260624_120000.backup"
```

5. Uygulamanın sağlıklı çalıştığını doğrulayın.
6. Migration sorununu geliştirici ile çözün, ardından `Backup-And-Migrate.ps1` ile tekrar deneyin.

---

## Senaryo 5: Yanlış Veri Girildi

**Belirtiler:** Yanlış tutar, yanlış işlem tipi veya yanlış kullanıcıya ait kayıt.

**Adımlar:**
1. Uygulama içinden kaydı bulun ve doğrudan düzeltin (güncelleme veya silme).
2. Audit log ekranından kim ne zaman değiştirdi kontrol edin.
3. Kayıt bulunamıyorsa (soft delete sonrası):

```sql
-- Silinmiş kaydı bul
SELECT "Id", "TransactionDate", "Amount", "Description", "DeletedAt", "DeletedByUserId"
FROM cash_transactions
WHERE "IsDeleted" = true
ORDER BY "DeletedAt" DESC
LIMIT 20;

-- Gerekirse soft delete'i geri al (dikkatle, audit log yaz)
UPDATE cash_transactions
SET "IsDeleted" = false, "DeletedAt" = NULL, "DeletedByUserId" = NULL
WHERE "Id" = '<kayit-id>';
```

4. Manuel SQL değişikliklerinden sonra audit_logs tablosuna elle not ekleyin.

---

## Senaryo 6: Sunucu Diski Bozuldu

**Belirtiler:** Sunucu başlamıyor, RAID alarmı, disk hatası.

**Adımlar:**
1. En son backup dosyasını bulun (ağ sürücüsü veya harici depolama).
2. Yeni sunucuya PostgreSQL kurun (aynı sürüm önerilir).
3. `yonetim_db` veritabanını oluşturun:
```sql
CREATE DATABASE yonetim_db;
```
4. Restore edin:
```powershell
$env:PGPASSWORD = "SIFRE"
pg_restore --host=yeni-sunucu --port=5432 --username=postgres --dbname=yonetim_db --verbose "son-backup.backup"
```
5. `appsettings.json` veya `YONETIM_DB_CONNECTION` env var'ını yeni sunucu adresine güncelleyin.
6. Uygulamayı test edin.

> Veri kaybı = son başarılı backup'tan bu yana girilen kayıtlar. Bu nedenle günlük backup kritiktir.

---

## Senaryo 7: Yeni Sürüm Sorunlu Çıktı

**Belirtiler:** Yeni ClickOnce güncellemesi sonrası uygulama hata veriyor veya özellik çalışmıyor.

**Adımlar:**
1. Log dosyasını kontrol edin: Hata nereden kaynaklanıyor?
2. Migration varsa ve sorun migration'daysa: Senaryo 4'ü uygulayın.
3. Önceki sürüme dönmek için:
   - ClickOnce geçici dosyalarını temizleyin: `%LocalAppData%\Apps\2.0\`
   - Önceki sürüm ClickOnce paketini yayınlayın (önceki publish klasörünü sunucuya geri kopyalayın).
   - Kullanıcılar yeniden kurulum yaptıklarında önceki sürümü alır.
4. Alternatif: Sorunlu özelliği devre dışı bırakan bir hotfix yayınlayın.

---

## Acil İletişim

Felaket senaryolarında:
- Uygulamayı hemen kapatın (yeni veri girişini durdurun).
- Backup'ın mevcut olduğundan emin olun.
- Sorunu ve adımları belgelendirin.
- Çözüm sonrası audit_logs'a veya bir değişiklik günlüğüne not ekleyin.
