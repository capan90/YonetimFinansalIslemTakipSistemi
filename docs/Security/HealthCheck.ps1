<#
.SYNOPSIS
    Yonetim Finansal Islem Takip Sistemi -- Production Health Check

.DESCRIPTION
    Uretim sunucusunun gunluk saglik durumunu hizlica dogrular. Sistemi DEGISTIRMEZ.

    Kontroller:
      - PostgreSQL calisiyor mu
      - 5432 acik mi
      - Publish Share var mi
      - Kurulum Share var mi
      - Backup klasoru var mi
      - Son backup tarihi (yas)
      - Disk doluluk orani

.PARAMETER PublishPath
    Yerel publish klasoru.

.PARAMETER ShareName
    Publish/kurulum SMB paylasim adi.

.PARAMETER InstallUnc
    Istemcilerin kurulum icin kullandigi UNC yolu.

.PARAMETER Port
    PostgreSQL portu.

.PARAMETER BackupPath
    Yedeklerin bulundugu klasor.

.PARAMETER MaxBackupAgeHours
    Son yedegin bu saatten eski olmasi WARNING uretir.

.PARAMETER DiskWarnPercent
    Disk doluluk orani bu esigi asarsa WARNING uretir.

.EXAMPLE
    .\docs\Security\HealthCheck.ps1
    .\docs\Security\HealthCheck.ps1 -MaxBackupAgeHours 24 -DiskWarnPercent 85
#>
[CmdletBinding()]
param(
    [string]$PublishPath       = "C:\Apps\Yonetim\Publish",
    [string]$ShareName         = "YonetimPublish",
    [string]$InstallUnc        = "\\10.0.0.169\YonetimPublish",
    [int]   $Port              = 5432,
    [string]$BackupPath        = "C:\Apps\Yonetim\Backups",
    [int]   $MaxBackupAgeHours = 24,
    [int]   $DiskWarnPercent   = 85
)

$ErrorActionPreference = "Continue"

$script:Results = New-Object System.Collections.Generic.List[object]
function Add-Result {
    param(
        [Parameter(Mandatory)][string]$Check,
        [Parameter(Mandatory)][ValidateSet("PASS", "WARNING", "FAIL")][string]$Status,
        [string]$Detail = ""
    )
    $script:Results.Add([PSCustomObject]@{ Check = $Check; Status = $Status; Detail = $Detail })
    $color = switch ($Status) { "PASS" { "Green" } "WARNING" { "Yellow" } "FAIL" { "Red" } }
    Write-Host ("  [{0,-7}] {1,-20} {2}" -f $Status, $Check, $Detail) -ForegroundColor $color
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " YONETIM -- HEALTH CHECK"                            -ForegroundColor Cyan
Write-Host " Makine : $env:COMPUTERNAME  |  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# ── PostgreSQL calisiyor mu ───────────────────────────────────────────────────
try {
    $pgSvc = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue
    if ($pgSvc | Where-Object { $_.Status -eq "Running" }) {
        Add-Result "PostgreSQL" "PASS" "Servis calisiyor."
    } elseif ($pgSvc) {
        Add-Result "PostgreSQL" "FAIL" "Servis mevcut ama calismiyor."
    } else {
        Add-Result "PostgreSQL" "FAIL" "Servis bulunamadi."
    }
}
catch { Add-Result "PostgreSQL" "FAIL" "Kontrol hatasi: $($_.Exception.Message)" }

# ── 5432 acik mi ──────────────────────────────────────────────────────────────
try {
    $listen = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($listen) {
        Add-Result "Port $Port" "PASS" "Dinleniyor."
    } else {
        Add-Result "Port $Port" "FAIL" "Dinlenmiyor."
    }
}
catch { Add-Result "Port $Port" "WARNING" "Okunamadi: $($_.Exception.Message)" }

# ── Publish Share var mi ──────────────────────────────────────────────────────
try {
    $share = Get-SmbShare -Name $ShareName -ErrorAction SilentlyContinue
    if ($share) {
        Add-Result "Publish Share" "PASS" "$ShareName -> $($share.Path)"
    } else {
        Add-Result "Publish Share" "FAIL" "'$ShareName' paylasimi yok."
    }
}
catch { Add-Result "Publish Share" "WARNING" "Okunamadi: $($_.Exception.Message)" }

# ── Kurulum Share var mi ──────────────────────────────────────────────────────
try {
    if (Test-Path $InstallUnc) {
        Add-Result "Kurulum Share" "PASS" "$InstallUnc erisilebilir."
    } else {
        Add-Result "Kurulum Share" "FAIL" "$InstallUnc erisilemiyor."
    }
}
catch { Add-Result "Kurulum Share" "WARNING" "Okunamadi: $($_.Exception.Message)" }

# ── Backup klasoru var mi + son backup tarihi ─────────────────────────────────
try {
    if (-not (Test-Path $BackupPath)) {
        Add-Result "Backup Folder" "FAIL" "$BackupPath yok."
    } else {
        Add-Result "Backup Folder" "PASS" "$BackupPath mevcut."
        $newest = Get-ChildItem $BackupPath -Filter *.backup -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (-not $newest) {
            Add-Result "Son Backup" "WARNING" "Hic .backup dosyasi yok."
        } else {
            $ageHours = [math]::Round(((Get-Date) - $newest.LastWriteTime).TotalHours, 1)
            $stamp = $newest.LastWriteTime.ToString('yyyy-MM-dd HH:mm')
            if ($ageHours -gt $MaxBackupAgeHours) {
                Add-Result "Son Backup" "WARNING" "$stamp ($ageHours saat once; esik $MaxBackupAgeHours sa)."
            } else {
                Add-Result "Son Backup" "PASS" "$stamp ($ageHours saat once)."
            }
        }
    }
}
catch { Add-Result "Backup Folder" "WARNING" "Kontrol hatasi: $($_.Exception.Message)" }

# ── Disk doluluk orani ────────────────────────────────────────────────────────
try {
    $driveLetter = (Split-Path -Qualifier $PublishPath).TrimEnd(":")
    $disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='$driveLetter`:'" -ErrorAction SilentlyContinue
    if ($disk -and $disk.Size -gt 0) {
        $usedPct = [math]::Round((($disk.Size - $disk.FreeSpace) / $disk.Size) * 100, 1)
        $freeGb  = [math]::Round($disk.FreeSpace / 1GB, 1)
        if ($usedPct -ge $DiskWarnPercent) {
            Add-Result "Disk ($driveLetter`:)" "WARNING" "%$usedPct dolu, $freeGb GB bos (esik %$DiskWarnPercent)."
        } else {
            Add-Result "Disk ($driveLetter`:)" "PASS" "%$usedPct dolu, $freeGb GB bos."
        }
    } else {
        Add-Result "Disk" "WARNING" "$driveLetter`: diski okunamadi."
    }
}
catch { Add-Result "Disk" "WARNING" "Kontrol hatasi: $($_.Exception.Message)" }

# ── Ozet ──────────────────────────────────────────────────────────────────────
# @() ile sar: PS 5.1'de tek eslesmede .Count scalar donup sayimi bozar.
$pass = @($script:Results | Where-Object { $_.Status -eq "PASS" }).Count
$warn = @($script:Results | Where-Object { $_.Status -eq "WARNING" }).Count
$fail = @($script:Results | Where-Object { $_.Status -eq "FAIL" }).Count

Write-Host ""
Write-Host ("  PASS: {0}  |  WARNING: {1}  |  FAIL: {2}" -f $pass, $warn, $fail) -ForegroundColor Cyan
Write-Host ""

if ($fail -gt 0) {
    Write-Host "DURUM: SAGLIKSIZ (FAIL)" -ForegroundColor Red
    exit 1
} elseif ($warn -gt 0) {
    Write-Host "DURUM: DIKKAT (WARNING)" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "DURUM: SAGLIKLI (PASS)" -ForegroundColor Green
    exit 0
}
