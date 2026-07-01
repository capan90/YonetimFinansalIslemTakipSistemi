<#
.SYNOPSIS
    Yonetim Finansal Islem Takip Sistemi -- Server Info Report

.DESCRIPTION
    Uretim sunucusunun envanterini toplar ve Markdown rapor uretir. Sistemi DEGISTIRMEZ.
    Rapor; donanim, OS, ag, disk, PostgreSQL, SMB paylasimlari, firewall, sertifika,
    zamanlanmis gorevler ve (maskeli) ortam degiskenlerini icerir.

.PARAMETER OutputPath
    Rapor dosyasi. Belirtilmezse docs\Security\reports\ServerInfo-<makine>-<tarih>.md.

.PARAMETER Port
    PostgreSQL portu.

.PARAMETER ShareName
    Publish/kurulum SMB paylasim adi.

.PARAMETER CertThumbprint
    ClickOnce imzalama sertifikasi parmak izi.

.EXAMPLE
    .\docs\Security\ServerInfo.ps1
    .\docs\Security\ServerInfo.ps1 -OutputPath "C:\Temp\server.md"
#>
[CmdletBinding()]
param(
    [string]$OutputPath    = "",
    [int]   $Port          = 5432,
    [string]$ShareName     = "YonetimPublish",
    [string]$CertThumbprint = "0136460438B6DED7F20498C00F7D3AB4C1E1B203"
)

$ErrorActionPreference = "Continue"

# ── Cikti yolu ────────────────────────────────────────────────────────────────
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $reportDir = if ($PSScriptRoot) { Join-Path $PSScriptRoot "reports" } else { Join-Path (Get-Location) "reports" }
    if (-not (Test-Path $reportDir)) { New-Item -ItemType Directory -Path $reportDir -Force | Out-Null }
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputPath = Join-Path $reportDir "ServerInfo-$env:COMPUTERNAME-$stamp.md"
}

Write-Host "Server Info toplaniyor..." -ForegroundColor Cyan

$sb = New-Object System.Text.StringBuilder
function Line([string]$text = "") { [void]$sb.AppendLine($text) }

# Secret env var'lar maskelenir; deger asla rapora yazilmaz.
function Mask-Env([string]$name, [string]$scope = "Machine") {
    $val = [Environment]::GetEnvironmentVariable($name, $scope)
    if ([string]::IsNullOrWhiteSpace($val)) { return "_(ayarlanmamis)_" }
    if ($name -match "PASSWORD|CONNECTION|SECRET|KEY") { return "**** (ayarli, gizlendi)" }
    return $val
}

# ── Genel ─────────────────────────────────────────────────────────────────────
Line "# Server Info Raporu"
Line ""
Line "- **Makine:** $env:COMPUTERNAME"
Line "- **Olusturulma:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Line "- **Uygulama:** Yonetim Finansal Islem Takip Sistemi"
Line ""

# ── Isletim Sistemi ───────────────────────────────────────────────────────────
Line "## Isletim Sistemi"
Line ""
try {
    $os = Get-CimInstance Win32_OperatingSystem
    $cs = Get-CimInstance Win32_ComputerSystem
    Line "| Alan | Deger |"
    Line "|------|-------|"
    Line "| OS | $($os.Caption) |"
    Line "| Surum | $($os.Version) (Build $($os.BuildNumber)) |"
    Line "| Mimari | $($os.OSArchitecture) |"
    Line "| Uretici / Model | $($cs.Manufacturer) / $($cs.Model) |"
    Line "| Son Boot | $($os.LastBootUpTime) |"
    Line "| Toplam RAM | $([math]::Round($cs.TotalPhysicalMemory / 1GB, 1)) GB |"
}
catch { Line "_OS bilgisi alinamadi: $($_.Exception.Message)_" }
Line ""

# ── CPU ───────────────────────────────────────────────────────────────────────
Line "## CPU"
Line ""
try {
    $cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
    Line "| Alan | Deger |"
    Line "|------|-------|"
    Line "| Model | $($cpu.Name) |"
    Line "| Cekirdek | $($cpu.NumberOfCores) fiziksel / $($cpu.NumberOfLogicalProcessors) mantiksal |"
}
catch { Line "_CPU bilgisi alinamadi._" }
Line ""

# ── Disk ──────────────────────────────────────────────────────────────────────
Line "## Diskler"
Line ""
try {
    Line "| Surucu | Toplam (GB) | Bos (GB) | Doluluk |"
    Line "|--------|-------------|----------|---------|"
    Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" | ForEach-Object {
        if ($_.Size -gt 0) {
            $tot  = [math]::Round($_.Size / 1GB, 1)
            $free = [math]::Round($_.FreeSpace / 1GB, 1)
            $pct  = [math]::Round((($_.Size - $_.FreeSpace) / $_.Size) * 100, 1)
            Line "| $($_.DeviceID) | $tot | $free | %$pct |"
        }
    }
}
catch { Line "_Disk bilgisi alinamadi._" }
Line ""

# ── Ag ────────────────────────────────────────────────────────────────────────
Line "## Ag Arayuzleri (IPv4)"
Line ""
try {
    Line "| Arayuz | IP | Prefix |"
    Line "|--------|----|--------|"
    Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
        Where-Object { $_.IPAddress -ne "127.0.0.1" } | ForEach-Object {
            Line "| $($_.InterfaceAlias) | $($_.IPAddress) | /$($_.PrefixLength) |"
        }
}
catch { Line "_Ag bilgisi alinamadi._" }
Line ""

# ── PostgreSQL ────────────────────────────────────────────────────────────────
Line "## PostgreSQL"
Line ""
try {
    $pgSvc = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue
    if ($pgSvc) {
        Line "| Servis | Durum | Baslangic |"
        Line "|--------|-------|-----------|"
        $pgSvc | ForEach-Object {
            $startType = (Get-CimInstance Win32_Service -Filter "Name='$($_.Name)'" -ErrorAction SilentlyContinue).StartMode
            Line "| $($_.Name) | $($_.Status) | $startType |"
        }
    } else {
        Line "_PostgreSQL servisi bulunamadi._"
    }
    Line ""
    $listen = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($listen) {
        $addrs = ($listen | Select-Object -ExpandProperty LocalAddress -Unique) -join ", "
        Line "- Port **$Port** dinleniyor: $addrs"
    } else {
        Line "- Port **$Port** dinlenmiyor."
    }
    $pgExe = Get-Command psql -ErrorAction SilentlyContinue
    if ($pgExe) {
        $ver = (& psql --version) 2>$null
        Line "- psql: $ver"
    }
}
catch { Line "_PostgreSQL bilgisi alinamadi: $($_.Exception.Message)_" }
Line ""

# ── SMB Paylasimlari ──────────────────────────────────────────────────────────
Line "## SMB Paylasimlari"
Line ""
try {
    $share = Get-SmbShare -Name $ShareName -ErrorAction SilentlyContinue
    if ($share) {
        Line "**$ShareName** -> ``$($share.Path)``"
        Line ""
        Line "| Hesap | Erisim | Tur |"
        Line "|-------|--------|-----|"
        Get-SmbShareAccess -Name $ShareName -ErrorAction SilentlyContinue | ForEach-Object {
            Line "| $($_.AccountName) | $($_.AccessRight) | $($_.AccessControlType) |"
        }
    } else {
        Line "_'$ShareName' paylasimi bulunamadi._"
    }
}
catch { Line "_Paylasim bilgisi alinamadi._" }
Line ""

# ── Firewall (PostgreSQL portu) ───────────────────────────────────────────────
Line "## Firewall Kurallari (Port $Port)"
Line ""
try {
    $filters = Get-NetFirewallPortFilter -ErrorAction SilentlyContinue |
        Where-Object { $_.Protocol -eq "TCP" -and ($_.LocalPort -eq "$Port" -or $_.LocalPort -contains "$Port") }
    if ($filters) {
        Line "| Kural | Yon | Islem | Etkin |"
        Line "|-------|-----|-------|-------|"
        $filters | ForEach-Object {
            $r = $_ | Get-NetFirewallRule -ErrorAction SilentlyContinue
            if ($r) { Line "| $($r.DisplayName) | $($r.Direction) | $($r.Action) | $($r.Enabled) |" }
        }
    } else {
        Line "_Port $Port icin ozel firewall kurali yok._"
    }
}
catch { Line "_Firewall bilgisi alinamadi._" }
Line ""

# ── Sertifika ─────────────────────────────────────────────────────────────────
Line "## ClickOnce Imzalama Sertifikasi"
Line ""
try {
    $cert = Get-ChildItem Cert:\CurrentUser\My -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $CertThumbprint }
    if ($cert) {
        Line "| Alan | Deger |"
        Line "|------|-------|"
        Line "| Konu | $($cert.Subject) |"
        Line "| Parmak Izi | $($cert.Thumbprint) |"
        Line "| Gecerlilik | $($cert.NotBefore.ToString('yyyy-MM-dd')) - $($cert.NotAfter.ToString('yyyy-MM-dd')) |"
        Line "| Kalan Gun | $([int]([math]::Floor(($cert.NotAfter - (Get-Date)).TotalDays))) |"
    } else {
        Line "_Sertifika bulunamadi: $CertThumbprint_"
    }
}
catch { Line "_Sertifika bilgisi alinamadi._" }
Line ""

# ── Zamanlanmis Gorevler ──────────────────────────────────────────────────────
Line "## Zamanlanmis Gorevler (Yonetim/Backup)"
Line ""
try {
    $tasks = Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object { $_.TaskName -match "Yonetim|Backup|Yedek" }
    if ($tasks) {
        Line "| Gorev | Durum | Yol |"
        Line "|-------|-------|-----|"
        $tasks | ForEach-Object { Line "| $($_.TaskName) | $($_.State) | $($_.TaskPath) |" }
    } else {
        Line "_Eslesen zamanlanmis gorev yok._"
    }
}
catch { Line "_Gorev bilgisi alinamadi._" }
Line ""

# ── Ortam Degiskenleri (maskeli) ──────────────────────────────────────────────
Line "## Ortam Degiskenleri (Machine)"
Line ""
Line "> Secret degerler asla yazilmaz; yalnizca varlik gosterilir."
Line ""
Line "| Degisken | Deger |"
Line "|----------|-------|"
foreach ($n in @("YONETIM_ENVIRONMENT", "YONETIM_DB_CONNECTION", "YONETIM_SMTP_USERNAME", "YONETIM_SMTP_PASSWORD", "YONETIM_UPDATE_PATH")) {
    Line "| $n | $(Mask-Env $n) |"
}
Line ""
Line "---"
Line "_Rapor SecurityAudit.ps1 / HealthCheck.ps1 ile birlikte degerlendirilmelidir._"

# ── Yaz ───────────────────────────────────────────────────────────────────────
$sb.ToString() | Set-Content -Path $OutputPath -Encoding UTF8
Write-Host "Rapor olusturuldu: $OutputPath" -ForegroundColor Green
