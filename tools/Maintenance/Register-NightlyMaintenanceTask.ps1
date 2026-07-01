<#
.SYNOPSIS
    Yonetim Finansal Islem Takip Sistemi -- Nightly Maintenance Scheduled Task kaydi

.DESCRIPTION
    Maintenance.ps1'i her gece calistiracak "Yonetim Nightly Maintenance" adli
    Windows Scheduled Task'ini olusturur veya gunceller.

    - SYSTEM hesabi, Highest privileges.
    - Daily 02:10 (backup task 02:00'de; cakismayi onlemek icin 10 dk sonra).
    - Mevcut backup task'ina DOKUNULMAZ (silinmez/degistirilmez).
    - Ayni task varsa -Force ile guncellenir.

    Yonetici (Administrator) olarak calistirilmalidir.
    PowerShell 5.1 uyumludur.

.PARAMETER TaskName
    Olusturulacak gorev adi.

.PARAMETER MaintenanceScript
    Sunucudaki Maintenance.ps1 tam yolu.

.PARAMETER At
    Gunluk tetikleme saati (HH:mm).

.EXAMPLE
    # Yonetici PowerShell:
    .\Register-NightlyMaintenanceTask.ps1
    .\Register-NightlyMaintenanceTask.ps1 -At "02:10"
#>
[CmdletBinding()]
param(
    [string]$TaskName          = "Yonetim Nightly Maintenance",
    [string]$MaintenanceScript = "C:\Apps\Yonetim\Kurulum\Maintenance\Maintenance.ps1",
    [string]$At                = "02:10"
)

$ErrorActionPreference = "Stop"

function Write-Info([string]$m, [string]$c = "Gray") { Write-Host $m -ForegroundColor $c }

Write-Info ""
Write-Info "=== Nightly Maintenance Task Kaydi ===" "Cyan"
Write-Info "  Gorev : $TaskName"
Write-Info "  Script: $MaintenanceScript"
Write-Info "  Saat  : $At (gunluk)"
Write-Info ""

# ── 1) Yonetici kontrolu ──────────────────────────────────────────────────────
$identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Info "[HATA] Bu script Yonetici (Administrator) olarak calistirilmalidir." "Red"
    Write-Info "       PowerShell'i 'Yonetici olarak calistir' ile acip tekrar deneyin." "Yellow"
    exit 1
}

# ── 2) Maintenance.ps1 hedefte var mi ─────────────────────────────────────────
if (-not (Test-Path $MaintenanceScript)) {
    Write-Info "[HATA] Maintenance script bulunamadi: $MaintenanceScript" "Red"
    Write-Info "       Once tools\Maintenance\Maintenance.ps1 dosyasini sunucuya kopyalayin." "Yellow"
    exit 1
}

# ── 3) Task tanimi ────────────────────────────────────────────────────────────
try {
    # Tetikleme saatini ayristir (yalnizca saat/dakika onemli; -Daily gunluk tekrar eder).
    $triggerAt = [DateTime]::ParseExact($At, "HH:mm", [System.Globalization.CultureInfo]::InvariantCulture)

    $action = New-ScheduledTaskAction -Execute "powershell.exe" `
        -Argument ("-NoProfile -ExecutionPolicy Bypass -File `"{0}`"" -f $MaintenanceScript)

    $trigger = New-ScheduledTaskTrigger -Daily -At $triggerAt

    # SYSTEM + en yuksek yetki (bakim; DB/servis/dosya erisimi icin gerekli).
    $taskPrincipal = New-ScheduledTaskPrincipal -UserId "SYSTEM" `
        -LogonType ServiceAccount -RunLevel Highest

    # Kacirilan calismalari mumkun olunca calistir; ust uste calismayi engelle; 2 saat limit.
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable `
        -DontStopOnIdleEnd -MultipleInstances IgnoreNew `
        -ExecutionTimeLimit (New-TimeSpan -Hours 2)

    $task = New-ScheduledTask -Action $action -Trigger $trigger `
        -Principal $taskPrincipal -Settings $settings `
        -Description "Yonetim Finansal Islem Takip Sistemi gece bakimi (backup + health + security + cleanup). Kaynak: Maintenance.ps1"

    # ── 4) Olustur / Guncelle ─────────────────────────────────────────────────
    $existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Info "Mevcut task bulundu, guncelleniyor (-Force)..." "Yellow"
    } else {
        Write-Info "Yeni task olusturuluyor..." "Yellow"
    }

    Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null

    # ── 5) Ozet ───────────────────────────────────────────────────────────────
    $reg  = Get-ScheduledTask -TaskName $TaskName
    $info = Get-ScheduledTaskInfo -TaskName $TaskName

    Write-Info ""
    Write-Info "[OK] Task kaydedildi." "Green"
    Write-Info "  Ad            : $($reg.TaskName)"
    Write-Info "  Durum         : $($reg.State)"
    Write-Info "  Hesap         : $($reg.Principal.UserId) (RunLevel: $($reg.Principal.RunLevel))"
    Write-Info "  Tetikleme     : Daily $At"
    Write-Info "  Sonraki calisma: $($info.NextRunTime)"
    Write-Info "  Komut         : powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$MaintenanceScript`""
    Write-Info ""
    Write-Info "NOT: Mevcut 'Yonetim PostgreSQL Daily Backup' task'ina DOKUNULMADI." "Cyan"
    exit 0
}
catch {
    Write-Info ""
    Write-Info "[HATA] Task kaydedilemedi: $($_.Exception.Message)" "Red"
    exit 1
}
