# Güvenlik Araçları (docs/Security)

Bu klasör, **Yönetim Finansal İşlem Takip Sistemi** üretim sunucusu için güvenlik
denetim ve sağlık kontrol araçlarını içerir. Tüm scriptler **salt-denetimdir** —
sistemi, PostgreSQL bağlantısını, ClickOnce publish akışını veya migration yapısını
**değiştirmez**.

## İçerik

| Dosya | Amaç |
|-------|------|
| `SecurityAudit.ps1` | 11 başlıkta güvenlik denetimi (PASS/WARNING/FAIL) |
| `HealthCheck.ps1` | Hızlı üretim sağlık kontrolü (günlük) |
| `ServerInfo.ps1` | Sunucu envanteri → Markdown rapor (`reports/`) |
| `Security-Hardening.md` | Sıkılaştırma rehberi (share, NTFS, firewall, PostgreSQL, backup, publish, sertifika, env) |
| `README.md` | Bu dosya |

## Gereksinimler

- **Windows PowerShell 5.1+** (Windows Server üzerinde mevcut).
- Scriptler **üretim sunucusunda** çalıştırılmalıdır (share/servis/sertifika lokal denetlenir).
- Bazı kontroller (firewall, servis, sertifika) için **yönetici** PowerShell önerilir.
- İlk çalıştırmada script yürütme politikası gerekebilir:

```powershell
# Yalnizca bu oturum icin, imzasiz yerel scriptlere izin ver
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

## Kullanım

### 1) Güvenlik Denetimi

```powershell
.\docs\Security\SecurityAudit.ps1
```

Varsayılan yollar/portlarla çalışır. Özelleştirme:

```powershell
.\docs\Security\SecurityAudit.ps1 `
  -PublishPath "C:\Apps\Yonetim\Publish" `
  -ShareName   "YonetimPublish" `
  -InstallUnc  "\\10.0.0.169\YonetimPublish" `
  -Port        5432 `
  -BackupPath  "C:\Apps\Yonetim\Backups" `
  -LogsPath    "C:\Apps\Yonetim\Logs" `
  -CertThumbprint "0136460438B6DED7F20498C00F7D3AB4C1E1B203"
```

Çıkış kodu: **FAIL varsa 1**, aksi halde 0 (WARNING çıkış kodunu etkilemez) — CI/zamanlanmış görevde kullanılabilir.

#### Çalışma modları: Server vs PublishMachine

`SecurityAudit.ps1` iki modda çalışır; fark **sertifika** kontrolünün sertliğindedir:

| Mod | Nasıl | Sertifika yok/expired | Kullanım |
|-----|-------|-----------------------|----------|
| **Server** (varsayılan) | parametresiz | **WARNING** — "Bu makine publish/imzalama makinesi değilse normaldir." | DB/uygulama sunucusu |
| **PublishMachine** | `-PublishMachine` | **FAIL** — imzalı yayın alınamaz | ClickOnce yayın/imzalama makinesi |

```powershell
# DB sunucusu (sertifika beklenmez):
.\docs\Security\SecurityAudit.ps1

# Publish/imzalama makinesi (sertifika zorunlu):
.\docs\Security\SecurityAudit.ps1 -PublishMachine
```

#### Port ve Firewall değerlendirmesi

PostgreSQL portu `0.0.0.0` (tüm arayüzler) üzerinde dinliyor olsa bile, **firewall
`RemoteAddress`'i güvenli bir subnet'e kısıtlıysa** (ör. `10.0.0.0/24`) bu durum **FAIL
değildir** — "Firewall kısıtlı olduğu için kabul edilebilir" açıklamasıyla WARNING olarak
raporlanır. Firewall `Any`'e açıksa veya kural yoksa yine WARNING (sıkılaştırma önerilir).

#### YONETIM_DB_CONNECTION

Bu ortam değişkeni ayarlı değilse **WARNING** verilir (FAIL değil): bağlantı dizesi
`appsettings.Production.json` üzerinden de sağlanabilir (bkz. [../Environment-Configuration.md](../Environment-Configuration.md)).

### 2) Sağlık Kontrolü

```powershell
.\docs\Security\HealthCheck.ps1
# esikleri ozellestir:
.\docs\Security\HealthCheck.ps1 -MaxBackupAgeHours 24 -DiskWarnPercent 85
```

Günlük zamanlanmış görev örneği:

```powershell
$action  = New-ScheduledTaskAction -Execute "powershell.exe" `
  -Argument "-NoProfile -ExecutionPolicy Bypass -File `"C:\Apps\Yonetim\docs\Security\HealthCheck.ps1`""
$trigger = New-ScheduledTaskTrigger -Daily -At 08:00
Register-ScheduledTask -TaskName "Yonetim-HealthCheck" -Action $action -Trigger $trigger -RunLevel Highest
```

### 3) Sunucu Envanteri Raporu

```powershell
.\docs\Security\ServerInfo.ps1
# veya belirli bir cikti:
.\docs\Security\ServerInfo.ps1 -OutputPath "C:\Temp\server.md"
```

Rapor varsayılan olarak `docs\Security\reports\ServerInfo-<makine>-<tarih>.md` altına yazılır.
Secret ortam değişkenleri (şifre/bağlantı) **maskelenir**; rapora asla açık değer yazılmaz.

## Çıktı Anlamları

| Durum | Anlam |
|-------|-------|
| **PASS** | Kontrol başarılı, aksiyon gerekmez. |
| **WARNING** | Çalışmayı engellemez ama sıkılaştırma/iyileştirme önerilir. |
| **FAIL** | Kritik bulgu; üretim güvenliği/erişilebilirliği risk altında, giderilmeli. |

## Önerilen Rutin

- **Günlük:** `HealthCheck.ps1`
- **Haftalık / her production publish öncesi:** `SecurityAudit.ps1`
- **Aylık / sunucu değişikliği sonrası:** `ServerInfo.ps1`

Bulgular için düzeltme adımları: [`Security-Hardening.md`](./Security-Hardening.md).

## Notlar

- `reports/` klasörü sunucuya özel envanter içerir; repoya commit edilmesi gerekmez (gerekirse `.gitignore`'a eklenebilir).
- Scriptler PostgreSQL'e **bağlanmaz**; yalnızca servis/port/paylaşım/dosya/sertifika durumunu okur.
- Bağlantı/yedek şifreleri için bkz. [../Environment-Configuration.md](../Environment-Configuration.md).
