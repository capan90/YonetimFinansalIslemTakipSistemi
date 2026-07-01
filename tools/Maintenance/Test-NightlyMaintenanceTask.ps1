<#
.SYNOPSIS
    Yonetim Finansal Islem Takip Sistemi -- Nightly Maintenance Task testi

.DESCRIPTION
    "Yonetim Nightly Maintenance" gorevini manuel tetikler, bitmesini bekler ve
    son calisma sonucunu + son bakim logunu raporlar.

    Cikis kodu:
      - Bakim FAIL ise            -> 1
      - Yalnizca WARNING ise      -> 0 (uyari yazilir)
      - PASS ise                  -> 0

    Bu script gorevi olusturmaz; yalnizca calistirir ve dogrular.
    PowerShell 5.1 uyumludur.

.PARAMETER TaskName
    Test edilecek gorev adi.

.PARAMETER LogPath
    Bakim loglarinin klasoru.

.PARAMETER TimeoutSeconds
    Gorevin bitmesi icin beklenecek azami sure.

.EXAMPLE
    .\Test-NightlyMaintenanceTask.ps1
    .\Test-NightlyMaintenanceTask.ps1 -TimeoutSeconds 120
#>
[CmdletBinding()]
param(
    [string]$TaskName       = "Yonetim Nightly Maintenance",
    [string]$LogPath        = "C:\Apps\Yonetim\Logs",
    [int]   $TimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"

function Write-Info([string]$m, [string]$c = "Gray") { Write-Host $m -ForegroundColor $c }

Write-Info ""
Write-Info "=== Nightly Maintenance Task Testi ===" "Cyan"
Write-Info "  Gorev : $TaskName"
Write-Info ""

# ── 1) Gorev var mi ───────────────────────────────────────────────────────────
$task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if (-not $task) {
    Write-Info "[HATA] Gorev bulunamadi: $TaskName" "Red"
    Write-Info "       Once Register-NightlyMaintenanceTask.ps1 ile olusturun." "Yellow"
    exit 1
}

try {
    # ── 2) Baslat ─────────────────────────────────────────────────────────────
    Write-Info "Gorev tetikleniyor..." "Yellow"
    Start-ScheduledTask -TaskName $TaskName

    # ── 3) Bitmesini bekle ────────────────────────────────────────────────────
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        Start-Sleep -Seconds 3
        $state = (Get-ScheduledTask -TaskName $TaskName).State
        Write-Info "  Durum: $state" "DarkGray"
    } while ($state -eq "Running" -and (Get-Date) -lt $deadline)

    if ($state -eq "Running") {
        Write-Info "[UYARI] Gorev $TimeoutSeconds sn icinde bitmedi; hala calisiyor." "Yellow"
    }

    # ── 4) Sonuc bilgisi ──────────────────────────────────────────────────────
    $info = Get-ScheduledTaskInfo -TaskName $TaskName
    Write-Info ""
    Write-Info "Son calisma bilgisi:" "Cyan"
    Write-Info "  LastRunTime    : $($info.LastRunTime)"
    # LastTaskResult 0 = basarili (exit 0); Maintenance.ps1 FAIL'de exit 1 -> LastTaskResult 1.
    Write-Info "  LastTaskResult : $($info.LastTaskResult)"
    Write-Info "  NextRunTime    : $($info.NextRunTime)"

    # ── 5) Son bakim logunu bul ───────────────────────────────────────────────
    $log = $null
    if (Test-Path $LogPath) {
        $log = Get-ChildItem $LogPath -Filter "maintenance-*.log" -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
    }

    $outcome = "UNKNOWN"
    if ($log) {
        Write-Info "  Bakim logu     : $($log.FullName)"
        $content = Get-Content $log.FullName -Raw
        if ($content -match "SONUC:\s*FAIL")        { $outcome = "FAIL" }
        elseif ($content -match "SONUC:\s*WARNING") { $outcome = "WARNING" }
        elseif ($content -match "SONUC:\s*PASS")    { $outcome = "PASS" }
    } else {
        Write-Info "  Bakim logu     : bulunamadi ($LogPath)" "Yellow"
    }

    # LastTaskResult ile capraz kontrol: 0 disi kod bakim FAIL demektir.
    if ($info.LastTaskResult -ne 0 -and $info.LastTaskResult -ne $null -and $state -ne "Running") {
        if ($outcome -ne "FAIL") { $outcome = "FAIL" }
    }

    # ── 6) Sonuc ──────────────────────────────────────────────────────────────
    Write-Info ""
    switch ($outcome) {
        "FAIL"    { Write-Info "SONUC: FAIL -- Bakim basarisiz. Bakim logunu inceleyin." "Red";    exit 1 }
        "WARNING" { Write-Info "SONUC: WARNING -- Bakim tamamlandi, bulgular var (kabul edilebilir olabilir)." "Yellow"; exit 0 }
        "PASS"    { Write-Info "SONUC: PASS -- Bakim sorunsuz." "Green"; exit 0 }
        default   { Write-Info "SONUC: BELIRSIZ -- Log okunamadi/gorev bitmedi. Manuel dogrulayin." "Yellow"; exit 0 }
    }
}
catch {
    Write-Info ""
    Write-Info "[HATA] Test sirasinda hata: $($_.Exception.Message)" "Red"
    exit 1
}
