# Maintenance Framework (Sprint 14.6A)

**Yönetim Finansal İşlem Takip Sistemi** üretim sunucusunda gece bakımını merkezi
olarak yöneten orchestrator: [`tools/Maintenance/Maintenance.ps1`](../../tools/Maintenance/Maintenance.ps1).

## Maintenance.ps1 ne yapar?

Mevcut scriptleri (backup, health check, security audit) **yalnızca çağırır** —
onları değiştirmez. Adımları sırayla çalıştırır ve tek bir bakım logu üretir:

| Adım | İş | Sonuç kuralı |
|------|-----|--------------|
| **A) PostgreSQL Backup** | `Scripts\Backup-YonetimDatabase.ps1` çalıştırır | exit 0 **ve** yeni, 0-byte olmayan `.backup` → PASS; aksi halde FAIL |
| **B) Health Check** | `Security\HealthCheck.ps1` çalıştırır | exit 0 → PASS; exit ≠ 0 → FAIL |
| **C) Security Audit** | `Security\SecurityAudit.ps1` (server modu) | exit 0 → PASS; exit ≠ 0 → **WARNING** (kabul edilebilir uyarılar olabilir) |
| **D) Cleanup** | 30 günden eski `.log` ve security `reports` dosyalarını siler | PASS (silinen sayısı raporlanır); hata → WARNING |
| **E) Summary** | PASS/WARNING/FAIL özeti | terminale + log dosyasına yazılır |

> **Güvenlik:** Alt scriptlerin çıktısı loga eklenirken `Password=…` / `PGPASSWORD`
> değerleri **maskelenir**. Bu script hiçbir sırrı üretmez, yazmaz veya loglamaz.

## Hangi klasörlerde çalışır?

Varsayılan parametreler (hepsi override edilebilir):

| Parametre | Varsayılan |
|-----------|-----------|
| `RootPath` | `C:\Apps\Yonetim` |
| `ScriptsPath` | `C:\Apps\Yonetim\Scripts` |
| `SecurityPath` | `C:\Apps\Yonetim\Kurulum\Security` |
| `LogPath` | `C:\Apps\Yonetim\Logs` |
| `BackupPath` | `C:\Apps\Yonetim\Backups` |
| `RetentionDays` | `30` |

Bağımlı olduğu dosyalar (sunucuda bulunmalı):
- `C:\Apps\Yonetim\Scripts\Backup-YonetimDatabase.ps1`
- `C:\Apps\Yonetim\Kurulum\Security\HealthCheck.ps1`
- `C:\Apps\Yonetim\Kurulum\Security\SecurityAudit.ps1`

Bakım logu: `C:\Apps\Yonetim\Logs\maintenance-yyyy-MM-dd_HH-mm.log`

## Manuel test komutu

```powershell
# Varsayilan yollarla:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Apps\Yonetim\Kurulum\Maintenance\Maintenance.ps1"

# Ozel yol/esik ile:
.\tools\Maintenance\Maintenance.ps1 -RootPath "C:\Apps\Yonetim" -RetentionDays 30

# Cikis kodunu gormek icin:
.\tools\Maintenance\Maintenance.ps1; Write-Host "ExitCode=$LASTEXITCODE"
```

## Sunucuya nasıl kopyalanır?

`Maintenance.ps1` sunucuda güvenlik scriptlerinin yanına, `Kurulum\Maintenance`
altına kopyalanması önerilir:

```powershell
# Ornek: proje kopyasindan sunucuya
New-Item -ItemType Directory -Force "C:\Apps\Yonetim\Kurulum\Maintenance" | Out-Null
Copy-Item ".\tools\Maintenance\Maintenance.ps1" "C:\Apps\Yonetim\Kurulum\Maintenance\" -Force
```

Güvenlik scriptleri ise ayrıca `C:\Apps\Yonetim\Kurulum\Security` altında olmalıdır
(bkz. [../Security/README.md](../Security/README.md)).

## Exit code mantığı

| Durum | Exit code |
|-------|-----------|
| Backup **FAIL** | `1` |
| Health Check **FAIL** | `1` |
| Security Audit **WARNING** | `0` (özet WARNING gösterir) |
| Yalnızca WARNING / tümü PASS | `0` |

Genel kural: **herhangi bir FAIL varsa `exit 1`**, yoksa `exit 0`. Bu sayede Windows
Scheduled Task (14.6B) başarısızlığı algılayabilir. Tüm adımlar çalışır (bir FAIL
diğer adımları atlamaz); exit kodu en sonda belirlenir.

## Backup / Health / Security ilişkisi

- **Backup (A)** kritiktir: gece yedeği alınamazsa bakım **FAIL** (exit 1) döner.
- **Health (B)** kritiktir: PostgreSQL/port/share/backup yaşı/disk sağlıksızsa **FAIL** (exit 1).
- **Security (C)** danışmandır: bulgular (kısıtlı firewall gibi kabul edilebilir warning'ler)
  bakımı durdurmaz; **WARNING** olarak raporlanır, exit 0 kalır.
- **Cleanup (D)** yalnızca `.log` ve security `reports` temizler; **backup dosyalarına dokunmaz**
  (yedek temizliği backup scriptinin sorumluluğundadır).

## 14.6B Notu — Windows Scheduled Task

Bu framework 14.6A'da **manuel/çağrılabilir** olacak şekilde tasarlandı. Sprint **14.6B**'de
`Maintenance.ps1`, her gece çalışacak bir **Windows Scheduled Task**'a bağlanacaktır
(ör. `SYSTEM` hesabı, en yüksek yetki, günlük tetikleyici). Exit code 1 döndüğünde görev
"başarısız" işaretleneceği için izleme/uyarı buna göre kurulacaktır. Örnek (14.6B'de detaylandırılacak):

```powershell
$action  = New-ScheduledTaskAction -Execute "powershell.exe" `
  -Argument "-NoProfile -ExecutionPolicy Bypass -File `"C:\Apps\Yonetim\Kurulum\Maintenance\Maintenance.ps1`""
$trigger = New-ScheduledTaskTrigger -Daily -At 03:00
Register-ScheduledTask -TaskName "Yonetim-NightlyMaintenance" -Action $action -Trigger $trigger `
  -User "SYSTEM" -RunLevel Highest
```
