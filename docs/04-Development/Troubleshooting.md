# Sorun Giderme

## Program Açılmıyor

**Adımlar:**
1. Log dosyasını kontrol et: `<kurulum-dizini>\logs\app-YYYYMMDD.log`
2. `[FTL]` (Fatal) satırlarını bul — başlangıç hatası nedeni orada yazar.
3. Olası nedenler:

| Log Mesajı | Çözüm |
|-----------|-------|
| "Veritabanına bağlanılamadı" | Aşağıdaki "DB Bağlantısı Yok" senaryosuna bak |
| "Bağlantı dizesi yapılandırılmamış" | `appsettings.json` dosyasını kontrol et |
| "appsettings.json bulunamadı" | Dosyanın uygulama dizininde olduğunu doğrula |
| Migration hatası | `dotnet ef database update` çalıştır |

4. ClickOnce güncelleme hatasıysa: Denetim Masası → Program Ekle/Kaldır → uygulamayı kaldır ve yeniden yükle.

---

## Veritabanı Bağlantısı Yok

**Adımlar:**
1. PostgreSQL sunucusuna ping: `ping <sunucu-adresi>`
2. Port kontrolü: `Test-NetConnection <sunucu-adresi> -Port 5432`
3. PostgreSQL servisini kontrol et:
   ```powershell
   Get-Service postgresql*
   Start-Service postgresql-x64-16   # sürüm numarasını uyarla
   ```
4. `appsettings.json` veya `YONETIM_DB_CONNECTION` env var bağlantı bilgilerini doğrula.
5. Güvenlik duvarı: 5432 portu açık olmalı.

---

## Mail Gitmiyor

**Kontrol Listesi:**
1. Ayarlar → SMTP Diagnostics ekranında bağlantı testi yap.
2. Sağlık izleme ekranında SMTP durumunu kontrol et.
3. `appsettings.json`'da SMTP ayarlarını kontrol et (Host, Port, SSL, kimlik bilgileri).
4. Cooldown süresi geçti mi? Aynı hata için belirli süre içinde tek mail gönderilir.
5. System log ekranında `Error` veya `Critical` kayıtlarına bak.

---

## ClickOnce Çalışmıyor

### "Uygulama hatalı biçimlendirilmiş" Hatası

1. `Publish-ClickOnce.ps1`'de `$AppName` ASCII-only mu? Türkçe karakter var mı?
2. Publish klasöründe eski/bozuk manifest dosyası var mı? `[0/6]` adımı publish klasörünü temizler.
3. Deployment manifest'i Not Defteri ile aç; `YÃ¶` gibi karakterler varsa encoding sorunu var.
4. Sertifika istemci bilgisayarda kurulu mu? (`Import-Certificate` yapıldı mı?)

### Güncelleme Gelmiyor

1. `\\SUNUCU\YonetimPublish\` UNC klasörüne ağ erişimi var mı?
2. SMB share kurulu mu? (`Get-SmbShare -Name "YonetimPublish"`)
3. Yeni sürüm sürüm numarası artırıldı mı? (Aynı versiyon → güncelleme algılanmaz)
4. `version.json` UNC klasöründe yeni sürümü gösteriyor mu?
5. Deployment manifest `ProviderURL` doğru UNC'yi gösteriyor mu?

### Masaüstü Kısayol İkonu Yanlış

Bu bilinen bir kısıtır; Won't Fix. Bkz. `docs/02-Architecture/ClickOnce.md`.

---

## Logo Görünmüyor (Login Ekranı)

1. `Assets/LoginIcon.png` dosyası projede var mı ve `Build Action = Resource` mi?
2. Pack URI doğru mu: `pack://application:,,,/Assets/LoginIcon.png`
3. `LoginWindow.xaml`'da `AppLogo` image source bağlandı mı?

---

## Tema Bozuldu

1. `ApplicationSettings` tablosunda `UI:Theme` anahtarı ne gösteriyor?
   ```sql
   SELECT "Key", "Value" FROM application_settings WHERE "Key" = 'UI:Theme';
   ```
2. Geçersiz değer varsa `'Light'` olarak güncelle.
3. `ThemeService.ApplyTheme("Light")` çağrısı `App.xaml.cs` startup'ta yapılıyor mu?

---

## Log Oluşmuyor

1. Uygulama dizininde `logs/` klasörü var mı?
2. Uygulama yazma iznine sahip mi?
3. Serilog konfigürasyonu `appsettings.json`'da doğru mu?
4. `RollingInterval.Day` — yeni güne geçişte yeni dosya oluşur.

---

## Backup Başarısız

1. `pg_dump` PATH'te mi? `pg_dump --version` komutunu çalıştır.
2. PostgreSQL bağlantı bilgileri doğru mu?
3. `Backups/` klasörü oluşturulmuş mu? Script yoksa oluşturur.
4. Disk alanı yeterli mi?

---

## Yanlış Migration Uygulandı

**Kritik: önce backup al!**

```powershell
.\scripts\Restore-Database.ps1 -BackupFile "Backups\yonetim_db_yyyyMMdd_HHmmss.backup"
```

Restore scripti çalışmadan önce tüm bağlantı bilgilerini gösterir ve "RESTORE" yazmanızı ister.

---

## Yanlış Veri Girildi

1. Uygulama içinden kaydı bul ve düzelt (güncelle veya sil).
2. Audit log ekranından kim ne zaman değiştirdi kontrol et.
3. Soft-delete sonrası kayıt bulunamazsa (Acil SQL):
   ```sql
   SELECT "Id", "TransactionDate", "Amount", "Description", "DeletedAt"
   FROM cash_transactions WHERE "IsDeleted" = true ORDER BY "DeletedAt" DESC LIMIT 20;
   
   -- Geri almak için
   UPDATE cash_transactions
   SET "IsDeleted" = false, "DeletedAt" = NULL, "DeletedByUserId" = NULL
   WHERE "Id" = '<kayit-uuid>';
   ```
   Manuel SQL'den sonra audit_logs'a not ekle.

---

## Yeni Sürüm Sorunlu Çıktı (Rollback)

1. Log kontrol et.
2. Migration sorunuysa: `.\scripts\Restore-Database.ps1`.
3. Önceki ClickOnce paketini sunucuya geri yükle → kullanıcılar yeniden kurulumda önceki sürümü alır.
4. Alternatif: Sorunlu özelliği devre dışı bırakan hotfix yayınla.

---

## Acil Durum İletişim

1. Uygulamayı kapat (yeni veri girişini durdur).
2. Backup'ın mevcut olduğunu doğrula.
3. Sorunu ve adımları belgele.
4. Çözüm sonrası audit_logs'a veya değişiklik günlüğüne not ekle.
