# Git Flow ve Release Süreci

## Branch Stratejisi

Şu an tek `main` branch. Proje tek geliştirici tarafından yönetildiğinden feature branch akışı uygulanmıyor.

---

## Commit Kuralı

Konvansiyonel commit formatı:

```
<type>(scope): <description>

Tipler:
  feat      — yeni özellik
  fix       — hata düzeltme
  style     — kod stili (iş mantığı değişmez)
  refactor  — yeniden yapılandırma
  docs      — dokümantasyon
  chore     — build, tooling, config
```

Örnekler:
```
feat(cargo): add mail notification preparation flow
fix(cargo): prevent mail preview from freezing UI
style(ui): polish cargo grids and improve selected row visibility
feat(ui): sprint 8 — finance UI polish, menu reorg, form UX
fix(clickonce): restore valid deployment manifest and publish flow
```

---

## Release Süreci

### Sürüm Öncesi Kontrol Listesi

```
[ ] dotnet build — 0 hata, 0 uyarı
[ ] dotnet test — tüm testler geçiyor
[ ] Veritabanı backup alındı
[ ] Migration uygulandı (varsa)
```

### Migration Varsa

```powershell
# Migration'dan önce backup
.\scripts\Backup-And-Migrate.ps1

# Veya elle
.\scripts\Backup-Database.ps1
dotnet ef database update \
  --project src\YonetimFinansalIslemTakipSistemi.Infrastructure \
  --startup-project src\YonetimFinansalIslemTakipSistemi.Infrastructure
```

### Sürüm Numarası Artırma

`src/YonetimFinansalIslemTakipSistemi.UI/YonetimFinansalIslemTakipSistemi.UI.csproj`:

```xml
<Version>1.0.0.22</Version>
<AssemblyVersion>1.0.0.22</AssemblyVersion>
<FileVersion>1.0.0.22</FileVersion>
```

### Publish

```powershell
# Lokal test
.\Publish-ClickOnce.ps1 -Version "1.0.0.22" -Sign $true

# Üretim
$env:YONETIM_UPDATE_PATH = "\\SUNUCU\YonetimPublish\"
.\Publish-ClickOnce.ps1 -Version "1.0.0.22" -Sign $true
```

**Kritik:** `$AppName` ve tüm mage parametreleri ASCII-only olmalıdır. Türkçe karakter encoding sorunu için bkz. `docs/04-Development/LessonsLearned.md`.

### Smoke Test

```
[ ] Uygulama başlıyor
[ ] Giriş yapılabiliyor
[ ] İşlem listesi yükleniyor
[ ] Yeni işlem eklenip kaydediliyor
[ ] Rapor oluşturuluyor
[ ] Güncelleme kontrolü çalışıyor (debug: "Yalnızca kurulu sürümde" mesajı)
```

### Git Tag

```powershell
git tag v1.0.0.22
git push origin v1.0.0.22
```

---

## Rollback Planı

| Sorun | Eylem |
|-------|-------|
| Migration sorunlu | `.\scripts\Restore-Database.ps1 -BackupFile "..."` |
| Uygulama başlamıyor | Log incele, eski ClickOnce paketi sunucuya geri yükle |
| Veri kaybı | Pre-release backup'tan restore |

---

## Backup Scriptleri

```powershell
# Backup al
.\scripts\Backup-Database.ps1

# Backup + migrate (atomik)
.\scripts\Backup-And-Migrate.ps1

# Restore (interaktif onay ister)
.\scripts\Restore-Database.ps1 -BackupFile "Backups\yonetim_db_yyyyMMdd_HHmmss.backup"
```

Backup dosyaları `Backups/` klasöründe saklanır; `.gitignore` ile korunur.

---

## SMB Share (Bir Kez Kurulum)

```powershell
# Admin PowerShell
New-SmbShare -Name "YonetimPublish" -Path "C:\Apps\Yonetim\Publish" -FullAccess "Everyone"
```

---

## Sertifika Kurulumu (Her İstemcide Bir Kez)

```powershell
# Admin PowerShell
Import-Certificate -FilePath "YonetimApp.cer" -CertStoreLocation "Cert:\LocalMachine\Root"
```
