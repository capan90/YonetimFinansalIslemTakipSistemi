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
      D) Cleanup             -> eski log/report + fazla ClickOnce publish versiyonlari
      E) Summary             -> zengin ozet + Windows Event Log + (FAIL'de) mail bildirimi

    Exit kodu:
      - Backup veya Health Check FAIL ise  -> 1
      - Yalnizca WARNING (or PASS) ise      -> 0
      (Exit kodu mantigi 14.6C'de DEGISMEDI.)

    Event Log:
      - Sonuca gore Application logune event yazilir (PASS=14600, WARNING=14601, FAIL=14602).
      - Kaynak yoksa olusturulmaya calisilir; yetki yoksa script cokmez, WARNING loglanir.

    Mail bildirimi (opsiyonel, -EnableMailNotification):
      - Yalnizca FAIL durumunda gonderilir (WARNING'de gonderilmez).
      - SMTP sifresi YALNIZCA YONETIM_SMTP_PASSWORD env var'dan okunur ve ASLA loglanmaz.
      - Gerekli parametreler eksikse WARNING yazilir; script cokmez.

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

.PARAMETER PublishPath
    ClickOnce publish klasoru (Application Files versiyon temizligi icin).

.PARAMETER LogRetentionDays
    maintenance-*.log ve backup-*.log dosyalari icin gun esigi. Varsayilan 30.

.PARAMETER ReportRetentionDays
    Security report dosyalari icin gun esigi. Varsayilan 30.

.PARAMETER PublishRetentionVersions
    Application Files altinda tutulacak son ClickOnce versiyon klasoru sayisi. Varsayilan 5.

.PARAMETER EventSource
    Windows Event Log kaynak adi. Varsayilan "YonetimMaintenance".

.PARAMETER EventLogName
    Event Log adi. Varsayilan "Application".

.PARAMETER EnableMailNotification
    Verilirse FAIL durumunda mail bildirimi denenir (WARNING'de gonderilmez).

.PARAMETER MailTo
    Bildirim alicisi.

.PARAMETER MailFrom
    Gonderen adresi.

.PARAMETER SmtpHost
    SMTP sunucusu.

.PARAMETER SmtpPort
    SMTP portu. Varsayilan 587.

.PARAMETER SmtpUseSsl
    SSL/TLS kullan. Varsayilan $true.

.PARAMETER SmtpUsername
    SMTP kullanici adi. Sifre YALNIZCA YONETIM_SMTP_PASSWORD env var'dan okunur.

.EXAMPLE
    .\Maintenance.ps1
    .\Maintenance.ps1 -EnableMailNotification -MailTo bilgi@x.com -MailFrom app@x.com -SmtpHost smtp.x.com -SmtpUsername app@x.com
#>
[CmdletBinding()]
param(
    [string]$RootPath                 = "C:\Apps\Yonetim",
    [string]$ScriptsPath              = "C:\Apps\Yonetim\Scripts",
    [string]$SecurityPath             = "C:\Apps\Yonetim\Kurulum\Security",
    [string]$LogPath                  = "C:\Apps\Yonetim\Logs",
    [string]$BackupPath               = "C:\Apps\Yonetim\Backups",
    [string]$PublishPath              = "C:\Apps\Yonetim\Publish",

    # Cleanup retention
    [int]   $LogRetentionDays         = 30,
    [int]   $ReportRetentionDays      = 30,
    [int]   $PublishRetentionVersions = 5,

    # Windows Event Log
    [string]$EventSource              = "YonetimMaintenance",
    [string]$EventLogName             = "Application",

    # Mail notification (yalnizca FAIL'de; sifre env var'dan, asla loglanmaz)
    [switch]$EnableMailNotification,
    [string]$MailTo                   = "",
    [string]$MailFrom                 = "",
    [string]$SmtpHost                 = "",
    [int]   $SmtpPort                 = 587,
    [bool]  $SmtpUseSsl               = $true,
    [string]$SmtpUsername             = ""
)

# Orchestrator sistemi degistirmemeli; her adim kendi icinde hatasini yakalar.
$ErrorActionPreference = "Continue"

# ── Durum ve gunluk toplama ───────────────────────────────────────────────────
$script:Results    = New-Object System.Collections.Generic.List[object]
$script:Transcript = New-Object System.Collections.Generic.List[string]

# Summary alanlari (E adiminda raporlanir)
$script:StartedAt              = Get-Date
$script:LatestBackup           = "yok"
$script:DeletedLogFiles        = 0
$script:DeletedReportFiles     = 0
$script:DeletedPublishVersions = 0
$script:EventLogWriteStatus    = "denenmedi"
$script:MailNotificationStatus = "devre disi"

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

# Sonuca gore Windows Event Log'a event yazar. Kaynak yoksa olusturulmaya calisilir;
# yetki yoksa (New-EventLog admin ister) script COKMEZ, durum WARNING olarak kaydedilir.
function Write-MaintenanceEvent([string]$overallStatus, [string]$message) {
    # Windows Event ID araligi 0-65535'tir; degerler bu sinirin altinda olmalidir.
    $map = @{
        "PASS"    = @{ Type = "Information"; Id = 14600 }
        "WARNING" = @{ Type = "Warning";     Id = 14601 }
        "FAIL"    = @{ Type = "Error";       Id = 14602 }
    }
    $entry = $map[$overallStatus]
    try {
        $sourceExists = $false
        try { $sourceExists = [System.Diagnostics.EventLog]::SourceExists($EventSource) } catch { $sourceExists = $false }

        if (-not $sourceExists) {
            # Kaynak olusturmak yonetici yetkisi ister; basarisiz olursa yakalanir.
            New-EventLog -LogName $EventLogName -Source $EventSource -ErrorAction Stop
            $sourceExists = $true
        }

        Write-EventLog -LogName $EventLogName -Source $EventSource `
            -EntryType $entry.Type -EventId $entry.Id -Message $message -ErrorAction Stop
        $script:EventLogWriteStatus = "yazildi ($($entry.Type), EventId $($entry.Id))"
    }
    catch {
        $script:EventLogWriteStatus = "WARNING: yazilamadi ($($_.Exception.Message)). Kaynak olusturmak icin bir kez yonetici olarak calistirin."
        Write-Log "  [WARNING] Event Log yazilamadi: $($_.Exception.Message)" "Yellow"
    }
}

# FAIL durumunda mail bildirimi gonderir. Sifre YALNIZCA env var'dan; deger asla loglanmaz.
function Send-FailureMail([string]$bodyText) {
    if (-not $EnableMailNotification) { $script:MailNotificationStatus = "devre disi (-EnableMailNotification verilmedi)"; return }

    # Sifre yalnizca ortam degiskeninden; degeri hicbir yere yazilmaz.
    $smtpPass = [Environment]::GetEnvironmentVariable("YONETIM_SMTP_PASSWORD", "Process")
    if ([string]::IsNullOrWhiteSpace($smtpPass)) { $smtpPass = $env:YONETIM_SMTP_PASSWORD }

    # Eksik parametreleri topla (SIFRE DEGERI degil, yalnizca 'eksik' bilgisi loglanir).
    $missing = @()
    if ([string]::IsNullOrWhiteSpace($MailTo))        { $missing += "MailTo" }
    if ([string]::IsNullOrWhiteSpace($MailFrom))      { $missing += "MailFrom" }
    if ([string]::IsNullOrWhiteSpace($SmtpHost))      { $missing += "SmtpHost" }
    if ([string]::IsNullOrWhiteSpace($SmtpUsername))  { $missing += "SmtpUsername" }
    if ([string]::IsNullOrWhiteSpace($smtpPass))      { $missing += "YONETIM_SMTP_PASSWORD" }

    if ($missing.Count -gt 0) {
        $script:MailNotificationStatus = "WARNING: eksik ayar(lar): $($missing -join ', ') -> mail gonderilmedi"
        Write-Log "  [WARNING] Mail bildirimi eksik ayar nedeniyle gonderilmedi: $($missing -join ', ')" "Yellow"
        return
    }

    try {
        $secure = ConvertTo-SecureString $smtpPass -AsPlainText -Force
        $cred   = New-Object System.Management.Automation.PSCredential($SmtpUsername, $secure)
        $subject = "[YONETIM] Nightly Maintenance FAILED - APPS"

        # NOT: Send-MailMessage PowerShell'de deprecated'dir (PS 5.1'de calisir).
        # Ileride .NET SmtpClient veya uygulama ici bildirim servisine tasinabilir.
        $params = @{
            To         = $MailTo
            From       = $MailFrom
            Subject    = $subject
            Body       = $bodyText
            SmtpServer = $SmtpHost
            Port       = $SmtpPort
            Credential = $cred
            ErrorAction = "Stop"
        }
        if ($SmtpUseSsl) { $params["UseSsl"] = $true }

        Send-MailMessage @params
        $script:MailNotificationStatus = "gonderildi -> $MailTo"
        Write-Log "  Mail bildirimi gonderildi: $MailTo" "Green"
    }
    catch {
        # Istisna mesaji sifre icermez; yine de degeri asla yazmiyoruz.
        $script:MailNotificationStatus = "HATA: gonderilemedi ($($_.Exception.Message))"
        Write-Log "  [WARNING] Mail gonderilemedi: $($_.Exception.Message)" "Yellow"
    }
    finally {
        # Yerel sifre degiskenlerini temizle.
        $smtpPass = $null; $secure = $null; $cred = $null
    }
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
        if ($newest) {
            $script:LatestBackup = "$($newest.Name) ($([math]::Round($newest.Length / 1KB, 1)) KB, $($newest.LastWriteTime.ToString('yyyy-MM-dd HH:mm')))"
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

# ── D) Cleanup (eski log/report + fazla publish versiyonu; BACKUP SILME) ──────
Write-Log ""
Write-Log "[D] Cleanup" "Cyan"
try {
    $cleanupWarn = $false

    # 1) Eski loglar: yalnizca maintenance-*.log ve backup-*.log (uygulama app-*.log'a dokunma).
    #    Bugun olusan bakim logu henuz diske yazilmadi -> guvenli.
    $logCutoff = (Get-Date).AddDays(-$LogRetentionDays)
    if (Test-Path $LogPath) {
        foreach ($pattern in @("maintenance-*.log", "backup-*.log")) {
            Get-ChildItem $LogPath -Filter $pattern -File -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -lt $logCutoff } | ForEach-Object {
                    try { Remove-Item $_.FullName -Force -ErrorAction Stop; $script:DeletedLogFiles++ }
                    catch { $cleanupWarn = $true; Write-Log "  Log silinemedi: $($_.Name) - $($_.Exception.Message)" "Yellow" }
                }
        }
    }

    # 2) Eski security report dosyalari.
    $reportCutoff = (Get-Date).AddDays(-$ReportRetentionDays)
    $reportsPath  = Join-Path $SecurityPath "reports"
    if (Test-Path $reportsPath) {
        Get-ChildItem $reportsPath -File -ErrorAction SilentlyContinue |
            Where-Object { $_.LastWriteTime -lt $reportCutoff } | ForEach-Object {
                try { Remove-Item $_.FullName -Force -ErrorAction Stop; $script:DeletedReportFiles++ }
                catch { $cleanupWarn = $true; Write-Log "  Report silinemedi: $($_.Name) - $($_.Exception.Message)" "Yellow" }
            }
    }

    # 3) ClickOnce publish versiyonlari: son N versiyon klasoru kalsin, eskiler silinsin.
    $appFilesPath = Join-Path $PublishPath "Application Files"
    if (Test-Path $appFilesPath) {
        $versionDirs = Get-ChildItem $appFilesPath -Directory -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending
        if ($versionDirs.Count -gt $PublishRetentionVersions) {
            $versionDirs | Select-Object -Skip $PublishRetentionVersions | ForEach-Object {
                try { Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop; $script:DeletedPublishVersions++ }
                catch { $cleanupWarn = $true; Write-Log "  Publish versiyonu silinemedi: $($_.Name) - $($_.Exception.Message)" "Yellow" }
            }
        }
    }

    # NOT: Backup temizligi backup scriptinin sorumlulugundadir; burada BACKUP SILINMEZ.
    $cleanupDetail = "$($script:DeletedLogFiles) log, $($script:DeletedReportFiles) report, $($script:DeletedPublishVersions) publish versiyonu silindi."
    if ($cleanupWarn) { Add-Result "Cleanup" "WARNING" "$cleanupDetail (bazi ogeler silinemedi)" }
    else              { Add-Result "Cleanup" "PASS" $cleanupDetail }
}
catch { Add-Result "Cleanup" "WARNING" "Cleanup sirasinda hata: $($_.Exception.Message)" }

# ── E) Summary ────────────────────────────────────────────────────────────────
$pass = @($script:Results | Where-Object { $_.Status -eq "PASS" }).Count
$warn = @($script:Results | Where-Object { $_.Status -eq "WARNING" }).Count
$fail = @($script:Results | Where-Object { $_.Status -eq "FAIL" }).Count

if     ($fail -gt 0) { $overall = "FAIL" }
elseif ($warn -gt 0) { $overall = "WARNING" }
else                 { $overall = "PASS" }

$script:FinishedAt = Get-Date
$durationSec       = [math]::Round(($script:FinishedAt - $script:StartedAt).TotalSeconds, 1)

# Bakim log dosyasi yolu (ozet metnine dahil; dosya en sonda yazilir).
$logStamp = Get-Date -Format "yyyy-MM-dd_HH-mm"
$logFile  = Join-Path $LogPath "maintenance-$logStamp.log"

# Adim sonuclari
Write-Log ""
Write-Log "==================================================" "Cyan"
Write-Log " BAKIM OZETI" "Cyan"
Write-Log "==================================================" "Cyan"
foreach ($r in $script:Results) {
    $c = switch ($r.Status) { "PASS" { "Green" } "WARNING" { "Yellow" } "FAIL" { "Red" } }
    Write-Log ("  {0,-16} : {1}  {2}" -f $r.Step, $r.Status, $r.Detail) $c
}
Write-Log ("  PASS: {0}  |  WARNING: {1}  |  FAIL: {2}" -f $pass, $warn, $fail) "Cyan"

# Event Log ve mail govdesi icin ortak ozet metni (secret icermez).
$summaryText = @"
Yonetim Nightly Maintenance
Makine                 : $env:COMPUTERNAME
OverallStatus          : $overall
StartedAt              : $($script:StartedAt.ToString('yyyy-MM-dd HH:mm:ss'))
FinishedAt             : $($script:FinishedAt.ToString('yyyy-MM-dd HH:mm:ss'))
DurationSeconds        : $durationSec
PASS / WARNING / FAIL  : $pass / $warn / $fail
LatestBackup           : $($script:LatestBackup)
DeletedLogFiles        : $($script:DeletedLogFiles)
DeletedReportFiles     : $($script:DeletedReportFiles)
DeletedPublishVersions : $($script:DeletedPublishVersions)
LogFile                : $logFile
"@

# Windows Event Log (sonuca gore) -> EventLogWriteStatus doldurulur
Write-MaintenanceEvent $overall $summaryText

# Mail bildirimi: yalnizca FAIL'de gonderilir; WARNING'de gonderilmez.
if ($overall -eq "FAIL") {
    Send-FailureMail $summaryText
}
elseif ($EnableMailNotification) {
    $script:MailNotificationStatus = "gonderilmedi (yalnizca FAIL'de gonderilir)"
}

# Zengin ozet alanlari
Write-Log ""
Write-Log "  OverallStatus          : $overall" "Cyan"
Write-Log "  StartedAt              : $($script:StartedAt.ToString('yyyy-MM-dd HH:mm:ss'))"
Write-Log "  FinishedAt             : $($script:FinishedAt.ToString('yyyy-MM-dd HH:mm:ss'))"
Write-Log "  DurationSeconds        : $durationSec"
Write-Log "  LatestBackup           : $($script:LatestBackup)"
Write-Log "  DeletedLogFiles        : $($script:DeletedLogFiles)"
Write-Log "  DeletedReportFiles     : $($script:DeletedReportFiles)"
Write-Log "  DeletedPublishVersions : $($script:DeletedPublishVersions)"
Write-Log "  EventLogWriteStatus    : $($script:EventLogWriteStatus)"
Write-Log "  MailNotificationStatus : $($script:MailNotificationStatus)"

Write-Log ""
if ($overall -eq "FAIL") {
    Write-Log "SONUC: FAIL -- Kritik adim(lar) basarisiz." "Red"
} elseif ($overall -eq "WARNING") {
    Write-Log "SONUC: WARNING -- Bakim tamamlandi, incelenmesi gereken bulgular var." "Yellow"
} else {
    Write-Log "SONUC: PASS -- Bakim sorunsuz tamamlandi." "Green"
}

# ── Log dosyasini yaz ─────────────────────────────────────────────────────────
try {
    $script:Transcript | Set-Content -Path $logFile -Encoding UTF8
    Write-Host ""
    Write-Host "Bakim logu: $logFile" -ForegroundColor Cyan
}
catch { Write-Host "Bakim logu yazilamadi: $($_.Exception.Message)" -ForegroundColor Red }

# ── Exit ──────────────────────────────────────────────────────────────────────
# Backup/Health FAIL -> exit 1. Security WARNING exit 0'i etkilemez. (14.6C'de DEGISMEDI.)
if ($fail -gt 0) { exit 1 } else { exit 0 }
