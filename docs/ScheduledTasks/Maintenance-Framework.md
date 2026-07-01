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
| **D) Cleanup** | Eski `maintenance-*.log`/`backup-*.log`, eski security `reports` ve fazla ClickOnce publish versiyonlarını siler | PASS (silinen sayıları raporlanır); silinemeyen öğe → WARNING |
| **E) Summary** | Zengin özet + **Windows Event Log** + (FAIL'de) **mail bildirimi** | terminale + log dosyasına yazılır |

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
| `PublishPath` | `C:\Apps\Yonetim\Publish` |
| `LogRetentionDays` | `30` |
| `ReportRetentionDays` | `30` |
| `PublishRetentionVersions` | `5` |
| `EventSource` / `EventLogName` | `YonetimMaintenance` / `Application` |

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
.\tools\Maintenance\Maintenance.ps1 -RootPath "C:\Apps\Yonetim" -LogRetentionDays 30 -PublishRetentionVersions 5

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

## Sprint 14.6C — Event Log, Mail Bildirimi ve Genişletilmiş Cleanup

### Windows Event Log

Her bakım sonunda `Application` loguna, sonuca göre bir event yazılır:

| Sonuç | EntryType | Event ID |
|-------|-----------|----------|
| PASS | Information | **140600** |
| WARNING | Warning | **140601** |
| FAIL | Error | **140602** |

- Kaynak (`YonetimMaintenance`) yoksa oluşturulmaya çalışılır. **Kaynak oluşturmak yönetici
  yetkisi ister**; yetki yoksa script **çökmez**, özet alanında `EventLogWriteStatus: WARNING…`
  yazılır. Kaynağı bir kez yönetici olarak oluşturmak yeterlidir (Register/Test yönetici çalışır).

**Event Log nasıl kontrol edilir?**
- **Event Viewer** → `Windows Logs` → `Application` → Source = `YonetimMaintenance`.
- PowerShell ile:
  ```powershell
  Get-EventLog -LogName Application -Source YonetimMaintenance -Newest 10 |
    Format-Table TimeGenerated, EntryType, EventID, Message -Auto
  ```

### Mail Bildirimi (opsiyonel)

`-EnableMailNotification` verilirse ve bakım **FAIL** olursa mail gönderilir.
**WARNING veya PASS'te mail gönderilmez.**

| Parametre | Açıklama |
|-----------|----------|
| `-EnableMailNotification` | Mail yolunu etkinleştirir (switch) |
| `-MailTo` / `-MailFrom` | Alıcı / gönderen |
| `-SmtpHost` / `-SmtpPort` | SMTP sunucu / port (varsayılan 587) |
| `-SmtpUseSsl` | SSL/TLS (varsayılan `$true`) |
| `-SmtpUsername` | SMTP kullanıcı adı |
| **`YONETIM_SMTP_PASSWORD`** (env var) | **Şifre yalnızca buradan okunur** |

- **Şifre kesinlikle parametre/dosya değil, yalnızca `YONETIM_SMTP_PASSWORD` ortam
  değişkeninden** okunur ve **asla loglanmaz** (özet yalnızca "ayarlı/ayarlanmamış" der).
- Gerekli ayarlar (MailTo/MailFrom/SmtpHost/SmtpUsername/şifre) eksikse özet
  `MailNotificationStatus: WARNING: eksik ayar(lar)…` yazar; **script çökmez, mail gönderilmez**.
- Mail konusu: `[YONETIM] Nightly Maintenance FAILED - APPS`. Gövde: özet + PASS/WARNING/FAIL
  sayısı + log path + son backup bilgisi.

> **Send-MailMessage deprecated notu:** `Send-MailMessage` cmdlet'i Microsoft tarafından
> **deprecated** işaretlenmiştir (PowerShell 5.1'de sorunsuz çalışır). İleride **.NET
> `System.Net.Mail.SmtpClient`** veya **uygulama içi bildirim servisine** (SmtpErrorNotificationService)
> taşınabilir. Kurumsal SMTP için MailKit gibi bir kütüphane de değerlendirilebilir.

Ortam değişkenini (Machine kapsamı) ayarlama:
```powershell
[System.Environment]::SetEnvironmentVariable('YONETIM_SMTP_PASSWORD','<sifre>','Machine')
```

### Cleanup Retention Ayarları

| Parametre | Varsayılan | Kapsam |
|-----------|-----------|--------|
| `LogRetentionDays` | 30 | `Logs\maintenance-*.log` ve `Logs\backup-*.log` |
| `ReportRetentionDays` | 30 | `Security\reports\*` |
| `PublishRetentionVersions` | 5 | `Publish\Application Files\` versiyon klasörleri |

- **Publish cleanup:** `C:\Apps\Yonetim\Publish\Application Files` altındaki ClickOnce versiyon
  klasörleri **LastWriteTime**'a göre sıralanır; **en yeni 5 tanesi tutulur**, eskiler silinir.
  Böylece geri alma (rollback) için son birkaç sürüm korunurken disk şişmesi önlenir.
- **Uygulama `app-*.log` dosyalarına dokunulmaz** (yalnızca `maintenance-*` ve `backup-*`).
- **Backup dosyaları burada SİLİNMEZ** — yedek temizliği backup scriptinin sorumluluğundadır.
- Silinen sayılar özete yazılır: `DeletedLogFiles`, `DeletedReportFiles`, `DeletedPublishVersions`.

### Zengin Özet (Summary) Alanları

Log sonunda ve Event/mail gövdesinde:
`OverallStatus`, `StartedAt`, `FinishedAt`, `DurationSeconds`, `LatestBackup`,
`DeletedLogFiles`, `DeletedReportFiles`, `DeletedPublishVersions`, `EventLogWriteStatus`,
`MailNotificationStatus`.

> **Exit code mantığı 14.6C'de DEĞİŞMEDİ:** herhangi bir FAIL → `exit 1`, aksi halde `exit 0`.
> Event Log/mail durumları exit kodunu etkilemez.

### 14.6C Manuel Test Komutları

```powershell
# Bildirim altyapisi testi (GERCEK mail GONDERMEZ):
.\tools\Maintenance\Test-MaintenanceNotification.ps1

# Mail parametreleriyle kontrol (yine gondermez, eksikleri raporlar):
.\tools\Maintenance\Test-MaintenanceNotification.ps1 -MailTo bilgi@x.com -MailFrom app@x.com -SmtpHost smtp.x.com -SmtpUsername app@x.com

# GERCEK test maili (YONETICI + YONETIM_SMTP_PASSWORD gerekli):
.\tools\Maintenance\Test-MaintenanceNotification.ps1 -SendTestMail -MailTo bilgi@x.com -MailFrom app@x.com -SmtpHost smtp.x.com -SmtpUsername app@x.com

# Tam bakim + mail (FAIL olursa mail dener):
.\tools\Maintenance\Maintenance.ps1 -EnableMailNotification -MailTo bilgi@x.com -MailFrom app@x.com -SmtpHost smtp.x.com -SmtpUsername app@x.com
```

## 14.6B Notu — Windows Scheduled Task

> **Windows Scheduled Task kurulumu için [Nightly-Maintenance-Task.md](./Nightly-Maintenance-Task.md) dosyasına bakın.**

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
