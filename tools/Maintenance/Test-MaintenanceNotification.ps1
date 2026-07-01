<#
.SYNOPSIS
    Yonetim Finansal Islem Takip Sistemi -- Maintenance bildirim altyapisi testi

.DESCRIPTION
    Maintenance.ps1'in 14.6C bildirim altyapisini (Windows Event Log + opsiyonel mail)
    IZOLE olarak dogrular. Backup/health/security calistirmaz.

    - Event Log: kaynagi olusturmaya/dogrulamaya calisir ve bir test event'i yazar.
    - Mail: gerekli parametreler + YONETIM_SMTP_PASSWORD var mi kontrol eder ve
      eksikse duzgun WARNING verir (script cokmez).
    - VARSAYILAN olarak GERCEK MAIL GONDERMEZ. Yalnizca -SendTestMail verilirse,
      parametreler tamsa gercek bir test maili gonderir.
    - Sifre YALNIZCA env var'dan okunur ve ASLA loglanmaz/yazilmaz.

    PowerShell 5.1 uyumludur.

.PARAMETER EventSource
    Event Log kaynak adi. Varsayilan "YonetimMaintenance".

.PARAMETER EventLogName
    Event Log adi. Varsayilan "Application".

.PARAMETER MailTo / MailFrom / SmtpHost / SmtpPort / SmtpUseSsl / SmtpUsername
    Mail testi parametreleri.

.PARAMETER SendTestMail
    Verilirse (ve parametreler tamsa) GERCEK bir test maili gonderilir.

.EXAMPLE
    # Sadece kontrol (mail gondermez):
    .\Test-MaintenanceNotification.ps1

    # Gercek test maili (parametreler + YONETIM_SMTP_PASSWORD gerekli):
    .\Test-MaintenanceNotification.ps1 -SendTestMail -MailTo bilgi@x.com -MailFrom app@x.com -SmtpHost smtp.x.com -SmtpUsername app@x.com
#>
[CmdletBinding()]
param(
    [string]$EventSource  = "YonetimMaintenance",
    [string]$EventLogName = "Application",
    [string]$MailTo       = "",
    [string]$MailFrom     = "",
    [string]$SmtpHost     = "",
    [int]   $SmtpPort     = 587,
    [bool]  $SmtpUseSsl   = $true,
    [string]$SmtpUsername = "",
    [switch]$SendTestMail
)

$ErrorActionPreference = "Continue"
function Write-Info([string]$m, [string]$c = "Gray") { Write-Host $m -ForegroundColor $c }

Write-Info ""
Write-Info "=== Maintenance Notification Testi ===" "Cyan"
Write-Info "  EventSource : $EventSource ($EventLogName)"
Write-Info "  SendTestMail: $([bool]$SendTestMail)"
Write-Info ""

$fail = $false

# ── 1) Event Log testi ────────────────────────────────────────────────────────
Write-Info "[1] Windows Event Log" "Cyan"
try {
    $exists = $false
    try { $exists = [System.Diagnostics.EventLog]::SourceExists($EventSource) } catch { $exists = $false }

    if (-not $exists) {
        try {
            New-EventLog -LogName $EventLogName -Source $EventSource -ErrorAction Stop
            $exists = $true
            Write-Info "  Kaynak olusturuldu: $EventSource" "Green"
        }
        catch {
            Write-Info "  [WARNING] Kaynak olusturulamadi (yonetici gerekir): $($_.Exception.Message)" "Yellow"
        }
    } else {
        Write-Info "  Kaynak zaten mevcut." "Green"
    }

    if ($exists) {
        # TEST event ID (Windows siniri 0-65535 icinde).
        Write-EventLog -LogName $EventLogName -Source $EventSource `
            -EntryType Information -EventId 14609 `
            -Message "Maintenance notification test event (Test-MaintenanceNotification.ps1)" -ErrorAction Stop
        Write-Info "  [OK] Test event yazildi (EventId 14609)." "Green"
    } else {
        Write-Info "  [WARNING] Kaynak olmadigi icin event yazilamadi. Bir kez YONETICI olarak calistirin." "Yellow"
    }
}
catch {
    Write-Info "  [WARNING] Event Log testi basarisiz: $($_.Exception.Message)" "Yellow"
}

# ── 2) Mail yapilandirma testi ────────────────────────────────────────────────
Write-Info ""
Write-Info "[2] Mail Bildirim Yapilandirmasi" "Cyan"

# Sifre yalnizca env var'dan; DEGERI asla yazilmaz -- yalnizca var/yok bilgisi.
$smtpPass    = $env:YONETIM_SMTP_PASSWORD
$hasPassword = -not [string]::IsNullOrWhiteSpace($smtpPass)

$missing = @()
if ([string]::IsNullOrWhiteSpace($MailTo))       { $missing += "MailTo" }
if ([string]::IsNullOrWhiteSpace($MailFrom))     { $missing += "MailFrom" }
if ([string]::IsNullOrWhiteSpace($SmtpHost))     { $missing += "SmtpHost" }
if ([string]::IsNullOrWhiteSpace($SmtpUsername)) { $missing += "SmtpUsername" }
if (-not $hasPassword)                           { $missing += "YONETIM_SMTP_PASSWORD" }

Write-Info "  YONETIM_SMTP_PASSWORD : $(if ($hasPassword) { 'ayarli (deger gizli)' } else { 'AYARLANMAMIS' })" $(if ($hasPassword) { 'Green' } else { 'Yellow' })

if ($missing.Count -gt 0) {
    Write-Info "  [WARNING] Eksik ayar(lar): $($missing -join ', ')" "Yellow"
    Write-Info "  -> Bu ayarlar olmadan Maintenance FAIL'de mail GONDEREMEZ (ama cokmez)." "Yellow"
} else {
    Write-Info "  [OK] Tum mail parametreleri ve sifre mevcut." "Green"
}

# ── 3) Gercek test maili (yalnizca -SendTestMail ve parametreler tamsa) ────────
if ($SendTestMail) {
    Write-Info ""
    Write-Info "[3] Gercek Test Maili" "Cyan"
    if ($missing.Count -gt 0) {
        Write-Info "  [WARNING] Eksik ayar nedeniyle gonderilmedi: $($missing -join ', ')" "Yellow"
    }
    else {
        try {
            $secure = ConvertTo-SecureString $smtpPass -AsPlainText -Force
            $cred   = New-Object System.Management.Automation.PSCredential($SmtpUsername, $secure)
            $params = @{
                To         = $MailTo
                From       = $MailFrom
                Subject    = "[YONETIM] Maintenance Notification TEST"
                Body       = "Bu bir bildirim altyapisi testidir. Maintenance mail yolu calisiyor."
                SmtpServer = $SmtpHost
                Port       = $SmtpPort
                Credential = $cred
                ErrorAction = "Stop"
            }
            if ($SmtpUseSsl) { $params["UseSsl"] = $true }
            # NOT: Send-MailMessage deprecated'dir (PS 5.1'de calisir).
            Send-MailMessage @params
            Write-Info "  [OK] Test maili gonderildi -> $MailTo" "Green"
        }
        catch {
            $fail = $true
            Write-Info "  [HATA] Test maili gonderilemedi: $($_.Exception.Message)" "Red"
        }
        finally { $smtpPass = $null; $secure = $null; $cred = $null }
    }
} else {
    Write-Info ""
    Write-Info "[3] Gercek mail testi atlandi (-SendTestMail verilmedi)." "DarkGray"
}

Write-Info ""
if ($fail) { Write-Info "SONUC: Bir test adimi HATA verdi." "Red"; exit 1 }
Write-Info "SONUC: Bildirim altyapisi testi tamamlandi." "Green"
exit 0
