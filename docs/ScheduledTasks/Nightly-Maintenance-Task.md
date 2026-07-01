# Nightly Maintenance -- Windows Scheduled Task (Sprint 14.6B)

[`Maintenance.ps1`](../../tools/Maintenance/Maintenance.ps1) framework'ünü (14.6A)
üretim sunucusunda her gece çalıştıran **Windows Scheduled Task** kurulumu.

| | |
|---|---|
| **Görev adı** | `Yonetim Nightly Maintenance` |
| **Çalışma saati** | Her gün **02:10** |
| **Hesap / yetki** | `SYSTEM` / Highest privileges |
| **Komut** | `powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Apps\Yonetim\Kurulum\Maintenance\Maintenance.ps1"` |
| **Kayıt scripti** | [`tools/Maintenance/Register-NightlyMaintenanceTask.ps1`](../../tools/Maintenance/Register-NightlyMaintenanceTask.ps1) |
| **Test scripti** | [`tools/Maintenance/Test-NightlyMaintenanceTask.ps1`](../../tools/Maintenance/Test-NightlyMaintenanceTask.ps1) |

## Register script nasıl kullanılır?

`Maintenance.ps1` sunucuda `C:\Apps\Yonetim\Kurulum\Maintenance\` altında olmalıdır
(bkz. [Maintenance-Framework.md](./Maintenance-Framework.md)). Ardından **Yönetici**
PowerShell'de:

```powershell
# Register scriptini sunucuya kopyalayip yonetici olarak calistirin:
Copy-Item ".\tools\Maintenance\Register-NightlyMaintenanceTask.ps1" "C:\Apps\Yonetim\Kurulum\Maintenance\" -Force

C:\Apps\Yonetim\Kurulum\Maintenance\Register-NightlyMaintenanceTask.ps1
# veya saat override:
C:\Apps\Yonetim\Kurulum\Maintenance\Register-NightlyMaintenanceTask.ps1 -At "02:10"
```

Script sırasıyla: yönetici mi kontrol eder → `Maintenance.ps1` var mı kontrol eder →
görevi oluşturur/günceller (`-Force`) → görev bilgisini yazar. Exit code: başarılı `0`,
hata (yönetici değil / script yok / istisna) `1`.

## Test script nasıl kullanılır?

```powershell
C:\Apps\Yonetim\Kurulum\Maintenance\Test-NightlyMaintenanceTask.ps1
# uzun bakimlar icin bekleme suresini artirin:
C:\Apps\Yonetim\Kurulum\Maintenance\Test-NightlyMaintenanceTask.ps1 -TimeoutSeconds 300
```

Görevi manuel tetikler, bitmesini bekler, `LastRunTime` / `LastTaskResult` yazar ve son
`maintenance-*.log` dosyasından sonucu okur. Exit code: **FAIL → 1**, WARNING/PASS → `0`
(WARNING'de uyarı yazılır).

## Backup task ile ilişki

- Mevcut `Yonetim PostgreSQL Daily Backup` görevi **02:00**'de çalışır ve bu adımda
  **silinmez / değiştirilmez**.
- `Yonetim Nightly Maintenance` **02:10**'da çalışır; içindeki backup adımı bağımsızdır
  (kendi `Backup-YonetimDatabase.ps1`'ini çağırır).
- İleride backup, maintenance içine alınıp ayrı backup görevi kapatılabilir; **bu sprintte
  yapılmaz**. İki görev şimdilik yan yana çalışır (maintenance backup adımı, 02:00 yedeğinin
  hemen ardından güncel bir yedek daha üretir ve doğrular).

## Neden 02:10 seçildi?

- Backup görevi **02:00**'de çalışıyor. Maintenance backup adımı PostgreSQL'e eş zamanlı
  yük bindirmesin ve dosya kilidi/çakışma olmasın diye **10 dakika sonra**, 02:10'da başlar.
- Gece penceresi (kullanıcı yükü düşük) tercih edilir; sağlık/güvenlik denetimi ve temizlik
  mesai dışında tamamlanır.

## Task nasıl devre dışı bırakılır?

```powershell
Disable-ScheduledTask -TaskName "Yonetim Nightly Maintenance"
# tekrar etkinlestirme:
Enable-ScheduledTask -TaskName "Yonetim Nightly Maintenance"
```

## Task nasıl silinir?

```powershell
Unregister-ScheduledTask -TaskName "Yonetim Nightly Maintenance" -Confirm:$false
```

> Yalnızca maintenance görevini kaldırır; backup görevine ve `Maintenance.ps1` dosyasına
> dokunmaz.

## Loglar nerede?

- Bakım logları: `C:\Apps\Yonetim\Logs\maintenance-yyyy-MM-dd_HH-mm.log`
- Görev geçmişi: Task Scheduler → `Yonetim Nightly Maintenance` → History sekmesi
  (History kapalıysa: `wevtutil set-log Microsoft-Windows-TaskScheduler/Operational /enabled:true`).

## Troubleshooting

| Belirti | Olası neden / çözüm |
|---------|---------------------|
| Register "Yonetici olarak çalıştırın" hatası | PowerShell'i **Yönetici olarak** açın. |
| Register "Maintenance script bulunamadi" | `Maintenance.ps1`'i `C:\Apps\Yonetim\Kurulum\Maintenance\` altına kopyalayın. |
| `LastTaskResult = 1` | Bakım FAIL döndü (backup veya health). İlgili `maintenance-*.log`'u inceleyin. |
| `LastTaskResult = 267009 (0x41301)` | Görev hâlâ çalışıyor. `-TimeoutSeconds` artırın veya bekleyin. |
| `LastTaskResult = 2147942401` | `powershell.exe` veya `-File` yolu bulunamadı; komut/yolları doğrulayın. |
| Görev çalışmıyor (State: Disabled) | `Enable-ScheduledTask` ile etkinleştirin. |
| Log oluşmuyor | `C:\Apps\Yonetim\Logs` var mı ve SYSTEM yazma yetkisi var mı kontrol edin. |
| Backup adımı FAIL | PostgreSQL servisi/PGPASSWORD/`Backup-YonetimDatabase.ps1` yolunu doğrulayın (şifre loglanmaz). |

İlişkili: [Maintenance-Framework.md](./Maintenance-Framework.md) · [../Security/README.md](../Security/README.md)
