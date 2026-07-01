<#
.SYNOPSIS
    Yonetim Finansal Islem Takip Sistemi -- Nightly Maintenance Orchestrator

.DESCRIPTION
    Uretim sunucusunda gece bakimini merkezi olarak yonetir. Mevcut scriptleri
    (backup, health check, security audit) yalnizca CAGIRIR; onlari degistirmez.

    Adimlar:
      A) PostgreSQL Backup   -> Scripts\Backup-YonetimDatabase.ps1
      B) Health Check        -> Security\HealthCheck.ps1
      C) Security Audit      -> Security\SecurityAudit.ps1 (server modu)
      D) Cleanup             -> 30 gunden eski .log ve security report dosyalari
      E) Summary             -> PASS/WARNING/FAIL ozeti + log dosyasi

    Exit kodu:
      - Backup veya Health Check FAIL ise  -> 1
      - Yalnizca WARNING (or PASS) ise      -> 0

    Guvenlik:
      - Alt scriptlerin ciktisi loglara eklenirken sifre/secret maskelenir.
      - Bu script hicbir sifreyi uretmez, yazmaz veya loglamaz.

    PowerShell 5.1 uyumludur.

.PARAMETER RootPath
    Sunucu kok klasoru.

.PARAMETER ScriptsPath
    Backup scriptinin bulundugu klasor.

.PARAMETER SecurityPath
    Sunucuya kopyalanmis guvenlik scriptlerinin (HealthCheck/SecurityAudit) klasoru.

.PARAMETER LogPath
    Uygulama ve bakim loglarinin klasoru.

.PARAMETER BackupPath
    Yedeklerin bulundugu klasor.

.PARAMETER RetentionDays
    Cleanup icin gun esigi (bu gunden eski .log ve report dosyalari silinir). Varsayilan 30.

.EXAMPLE
    .\Maintenance.ps1
    .\Maintenance.ps1 -RootPath "C:\Apps\Yonetim" -RetentionDays 30
#>
[CmdletBinding()]
param(
    [string]$RootPath      = "C:\Apps\Yonetim",
    [string]$ScriptsPath   = "C:\Apps\Yonetim\Scripts",
    [string]$SecurityPath  = "C:\Apps\Yonetim\Kurulum\Security",
    [string]$LogPath       = "C:\Apps\Yonetim\Logs",
    [string]$BackupPath    = "C:\Apps\Yonetim\Backups",
    [int]   $RetentionDays = 30
)

# Orchestrator sistemi degistirmemeli; her adim kendi icinde hatasini yakalar.
$ErrorActionPreference = "Continue"

# ── Durum ve gunluk toplama ───────────────────────────────────────────────────
$script:Results    = New-Object System.Collections.Generic.List[object]
$script:Transcript = New-Object System.Collections.Generic.List[string]

function Write-Log {
    param([string]$Message = "", [string]$Color = "Gray")
    $stamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line  = "[$stamp] $Message"
    $script:Transcript.Add($line)
    Write-Host $line -ForegroundColor $Color
}

function Add-Result {
    param(
        [Parameter(Mandatory)][string]$Step,
        [Parameter(Mandatory)][ValidateSet("PASS", "WARNING", "FAIL")][string]$Status,
        [string]$Detail = ""
    )
    $script:Results.Add([PSCustomObject]@{ Step = $Step; Status = $Status; Detail = $Detail })
    $color = switch ($Status) { "PASS" { "Green" } "WARNING" { "Yellow" } "FAIL" { "Red" } }
    Write-Log ("  [{0,-7}] {1,-16} {2}" -f $Status, $Step, $Detail) $color
}

# Alt script ciktisinda sifre/secret maskele (asla loglanmaz).
function Protect-Secret([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return $text }
    $t = [regex]::Replace($text, "(?i)(password\s*=\s*)([^;\r\n]+)", '${1}***')
    $t = [regex]::Replace($t,    "(?i)(PGPASSWORD\s*=?\s*)(\S+)",   '${1}***')
    return $t
}

# Alt scripti ayri bir powershell.exe surecinde calistirir:
#  - 'exit' cagrisi ana scripti etkilemez,
#  - $LASTEXITCODE guvenilir sekilde alt surecin cikis kodudur.
function Invoke-ChildScript {
    param([Parameter(Mandatory)][string]$Path, [string[]]$ScriptArgs = @())
    $psArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $Path) + $ScriptArgs
    $raw = & powershell.exe @psArgs 2>&1 | Out-String
    return [PSCustomObject]@{ ExitCode = $LASTEXITCODE; Output = $raw }
}

# Alt script ciktisini (maskeli) bakim loguna ekler; terminale basmaz (ozet yeterli).
function Add-ChildOutputToLog([string]$stepName, [string]$output) {
    if ([string]::IsNullOrWhiteSpace($output)) { return }
    $clean = Protect-Secret $output
    $script:Transcript.Add("----- $stepName cikti (baslangic) -----")
    foreach ($l in ($clean -split "`r?`n")) { $script:Transcript.Add("    $l") }
    $script:Transcript.Add("----- $stepName cikti (bitis) -----")
}

# ── Baslik ────────────────────────────────────────────────────────────────────
Write-Log "==================================================" "Cyan"
Write-Log " YONETIM -- NIGHTLY MAINTENANCE" "Cyan"
Write-Log " Makine : $env:COMPUTERNAME" "Cyan"
Write-Log " Kok    : $RootPath" "Cyan"
Write-Log "==================================================" "Cyan"

# Log klasoru yoksa olustur (log dosyasi buraya yazilacak).
try {
    if (-not (Test-Path $LogPath)) {
        New-Item -ItemType Directory -Path $LogPath -Force | Out-Null
        Write-Log "Log klasoru olusturuldu: $LogPath"
    }
}
catch { Write-Log "Log klasoru olusturulamadi: $($_.Exception.Message)" "Red" }

# ── A) PostgreSQL Backup ──────────────────────────────────────────────────────
Write-Log ""
Write-Log "[A] PostgreSQL Backup" "Cyan"
try {
    $backupScript = Join-Path $ScriptsPath "Backup-YonetimDatabase.ps1"
    if (-not (Test-Path $backupScript)) {
        Add-Result "Backup" "FAIL" "Backup script bulunamadi: $backupScript"
    }
    else {
        $before = Get-Date
        $res = Invoke-ChildScript -Path $backupScript
        Add-ChildOutputToLog "Backup" $res.Output

        # En yeni .backup dosyasini bul ve dogrula (var mi, 0 byte mi, yeni mi).
        $newest = $null
        if (Test-Path $BackupPath) {
            $newest = Get-ChildItem $BackupPath -Filter *.backup -ErrorAction SilentlyContinue |
                Sort-Object LastWriteTime -Descending | Select-Object -First 1
        }

        if ($res.ExitCode -ne 0) {
            Add-Result "Backup" "FAIL" "Backup script hata verdi (exit $($res.ExitCode))."
        }
        elseif (-not $newest) {
            Add-Result "Backup" "FAIL" "Backup basarili gorundu ama .backup dosyasi bulunamadi ($BackupPath)."
        }
        elseif ($newest.Length -le 0) {
            Add-Result "Backup" "FAIL" "Backup dosyasi 0 byte: $($newest.Name)"
        }
        elseif ($newest.LastWriteTime -lt $before.AddMinutes(-1)) {
            Add-Result "Backup" "FAIL" "Yeni backup olusmadi. En yeni: $($newest.Name) ($($newest.LastWriteTime.ToString('yyyy-MM-dd HH:mm')))."
        }
        else {
            $sizeKb = [math]::Round($newest.Length / 1KB, 1)
            Add-Result "Backup" "PASS" "$($newest.Name) ($sizeKb KB) - $($newest.LastWriteTime.ToString('yyyy-MM-dd HH:mm'))"
        }
    }
}
catch { Add-Result "Backup" "FAIL" "Backup adiminda beklenmeyen hata: $($_.Exception.Message)" }

# ── B) Health Check ───────────────────────────────────────────────────────────
Write-Log ""
Write-Log "[B] Health Check" "Cyan"
try {
    $healthScript = Join-Path $SecurityPath "HealthCheck.ps1"
    if (-not (Test-Path $healthScript)) {
        Add-Result "HealthCheck" "FAIL" "HealthCheck.ps1 bulunamadi: $healthScript"
    }
    else {
        $res = Invoke-ChildScript -Path $healthScript
        Add-ChildOutputToLog "HealthCheck" $res.Output
        if ($res.ExitCode -eq 0) {
            Add-Result "HealthCheck" "PASS" "Saglik kontrolu basarili (exit 0)."
        } else {
            Add-Result "HealthCheck" "FAIL" "Saglik kontrolu basarisiz (exit $($res.ExitCode)). Detay bakim logunda."
        }
    }
}
catch { Add-Result "HealthCheck" "FAIL" "Health Check adiminda beklenmeyen hata: $($_.Exception.Message)" }

# ── C) Security Audit (server modu -- -PublishMachine VERILMEZ) ────────────────
Write-Log ""
Write-Log "[C] Security Audit" "Cyan"
try {
    $auditScript = Join-Path $SecurityPath "SecurityAudit.ps1"
    if (-not (Test-Path $auditScript)) {
        Add-Result "SecurityAudit" "WARNING" "SecurityAudit.ps1 bulunamadi: $auditScript"
    }
    else {
        $res = Invoke-ChildScript -Path $auditScript
        Add-ChildOutputToLog "SecurityAudit" $res.Output
        if ($res.ExitCode -eq 0) {
            Add-Result "SecurityAudit" "PASS" "Denetim temiz (exit 0)."
        } else {
            # Audit kabul edilebilir warning'ler uretebilir -> bakim icin WARNING (exit 0 kalir).
            Add-Result "SecurityAudit" "WARNING" "Denetim bulgu bildirdi (exit $($res.ExitCode)). Detay bakim logunda."
        }
    }
}
catch { Add-Result "SecurityAudit" "WARNING" "Security Audit adiminda beklenmeyen hata: $($_.Exception.Message)" }

# ── D) Cleanup (30 gunden eski .log ve report; BACKUP SILME) ──────────────────
Write-Log ""
Write-Log "[D] Cleanup" "Cyan"
try {
    $cutoff        = (Get-Date).AddDays(-$RetentionDays)
    $deletedLogs   = 0
    $deletedReports = 0

    # Eski .log dosyalari (bugun olusan bakim logu henuz diske yazilmadi -> guvenli).
    if (Test-Path $LogPath) {
        Get-ChildItem $LogPath -Filter *.log -File -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -lt $cutoff } | ForEach-Object {
                try { Remove-Item $_.FullName -Force -ErrorAction Stop; $deletedLogs++ }
                catch { Write-Log "  Log silinemedi: $($_.Name) - $($_.Exception.Message)" "Yellow" }
            }
    }

    # Eski security report dosyalari.
    $reportsPath = Join-Path $SecurityPath "reports"
    if (Test-Path $reportsPath) {
        Get-ChildItem $reportsPath -File -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -lt $cutoff } | ForEach-Object {
                try { Remove-Item $_.FullName -Force -ErrorAction Stop; $deletedReports++ }
                catch { Write-Log "  Report silinemedi: $($_.Name) - $($_.Exception.Message)" "Yellow" }
            }
    }

    # NOT: Backup temizligi backup scriptinin kendi sorumlulugundadir; burada backup SILINMEZ.
    Add-Result "Cleanup" "PASS" "$deletedLogs eski log, $deletedReports eski report silindi ($RetentionDays gun+)."
}
catch { Add-Result "Cleanup" "WARNING" "Cleanup sirasinda hata: $($_.Exception.Message)" }

# ── E) Summary ────────────────────────────────────────────────────────────────
$pass = @($script:Results | Where-Object { $_.Status -eq "PASS" }).Count
$warn = @($script:Results | Where-Object { $_.Status -eq "WARNING" }).Count
$fail = @($script:Results | Where-Object { $_.Status -eq "FAIL" }).Count

Write-Log ""
Write-Log "==================================================" "Cyan"
Write-Log " BAKIM OZETI" "Cyan"
Write-Log "==================================================" "Cyan"
foreach ($r in $script:Results) {
    $c = switch ($r.Status) { "PASS" { "Green" } "WARNING" { "Yellow" } "FAIL" { "Red" } }
    Write-Log ("  {0,-16} : {1}" -f $r.Step, $r.Status) $c
}
Write-Log ("  PASS: {0}  |  WARNING: {1}  |  FAIL: {2}" -f $pass, $warn, $fail) "Cyan"

if ($fail -gt 0) {
    Write-Log "SONUC: FAIL -- Kritik adim(lar) basarisiz." "Red"
} elseif ($warn -gt 0) {
    Write-Log "SONUC: WARNING -- Bakim tamamlandi, incelenmesi gereken bulgular var." "Yellow"
} else {
    Write-Log "SONUC: PASS -- Bakim sorunsuz tamamlandi." "Green"
}

# ── Log dosyasini yaz ─────────────────────────────────────────────────────────
try {
    $logStamp = Get-Date -Format "yyyy-MM-dd_HH-mm"
    $logFile  = Join-Path $LogPath "maintenance-$logStamp.log"
    $script:Transcript | Set-Content -Path $logFile -Encoding UTF8
    Write-Host ""
    Write-Host "Bakim logu: $logFile" -ForegroundColor Cyan
}
catch { Write-Host "Bakim logu yazilamadi: $($_.Exception.Message)" -ForegroundColor Red }

# ── Exit ──────────────────────────────────────────────────────────────────────
# Backup/Health FAIL -> exit 1. Security WARNING exit 0'i etkilemez.
if ($fail -gt 0) { exit 1 } else { exit 0 }
