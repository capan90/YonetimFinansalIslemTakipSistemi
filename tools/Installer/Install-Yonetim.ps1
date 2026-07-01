<#
.SYNOPSIS
    Yonetim Finansal Islem Takip Sistemi -- Kurumsal Kurulum Motoru

.DESCRIPTION
    Canli kullanicinin tek adimda uygulamayi kurabilmesi icin on-kontrolleri yapar
    ve mevcut ClickOnce paketini baslatir. Kullanici genelde bunu dogrudan degil,
    yanindaki Kurulum.bat uzerinden calistirir.

    Adimlar:
      1) Ortam (Windows 10/11) kontrolu
      2) .NET Desktop Runtime kontrolu (gerekirse kurulum/yonlendirme)
      3) Sunucu paylasimi erisim kontrolu
      4) ClickOnce manifest kontrolu
      5) Uygulamayi kurma (ClickOnce baslatma)
      6) Tamamlandi + (opsiyonel) masaustu kisayolu

    - Kullaniciya teknik hata degil, ACIKLAYICI mesaj gosterilir; teknik ayrinti
      yalnizca log dosyasina yazilir.
    - Log: %LOCALAPPDATA%\Yonetim\InstallerLogs\install-<tarih>.log
    - PowerShell 5.1 uyumlu; Windows 10/11 hedeflenir.
    - Mevcut Publish-Production.ps1 / Publish-ClickOnce.ps1 ciktisiyla uyumludur.

.PARAMETER ShareRoot
    ClickOnce publish paylasimi (manifestin bulundugu UNC).

.PARAMETER ManifestName
    ClickOnce deployment manifest dosya adi.

.PARAMETER MinDotnetMajor
    Gerekli minimum .NET Desktop Runtime ana surumu (uygulama net9.0-windows).

.PARAMETER RuntimeDownloadUrl
    Runtime yerelde yoksa gosterilecek resmi indirme adresi.

.PARAMETER RuntimeInstallerPath
    Kurulum klasorundeki runtime installer (opsiyonel). Bos ise otomatik aranir.

.PARAMETER CreateDesktopShortcut
    Verilirse kurulum sonrasi masaustune kisayol (appref-ms) kopyalanmaya calisilir.

.PARAMETER NoPause
    Kurulum.bat cagirdiginda pencere yonetimini bat'a birakmak icin.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File Install-Yonetim.ps1
#>
[CmdletBinding()]
param(
    [string]$ShareRoot            = "\\10.0.0.169\YonetimPublish",
    [string]$ManifestName         = "YonetimFinansalIslemTakipSistemi.UI.application",
    [int]   $MinDotnetMajor       = 9,
    # Resmi aka.ms DOGRUDAN indirme (x64 Desktop Runtime) -- otomatik kurulum icin.
    [string]$RuntimeDirectUrl     = "https://aka.ms/dotnet/9.0/windowsdesktop-runtime-win-x64.exe",
    # Resmi indirme SAYFASI -- yalnizca otomatik kurulum basarisiz olursa son secenek.
    [string]$RuntimeDownloadUrl   = "https://dotnet.microsoft.com/download/dotnet/9.0",
    [string]$RuntimeInstallerPath = "",
    [switch]$CreateDesktopShortcut,
    [switch]$NoPause
)

$ErrorActionPreference = "Stop"

# Kutu cizimi/onay isaretleri icin UTF-8 (basarisiz olursa gorsel etki, islevsel degil).
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$OK = [char]0x2714   # ✔
$NO = [char]0x2716   # ✖

$AppName    = "Yonetim Finansal Islem Takip Sistemi"
$ScriptDir  = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
# $Manifest, ShareRoot dogrulamasindan SONRA hesaplanir (asagida).

# ── Log kurulumu ──────────────────────────────────────────────────────────────
$LogDir  = Join-Path $env:LOCALAPPDATA "Yonetim\InstallerLogs"
try { if (-not (Test-Path $LogDir)) { New-Item -ItemType Directory -Path $LogDir -Force | Out-Null } } catch { }
$LogFile = Join-Path $LogDir ("install-{0}.log" -f (Get-Date -Format "yyyy-MM-dd_HH-mm-ss"))

function Log([string]$text) {
    try { Add-Content -Path $LogFile -Value ("[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $text) -Encoding UTF8 } catch { }
}
function Say([string]$msg, [string]$color = "Gray") { Write-Host $msg -ForegroundColor $color }
function StepOk([string]$msg)   { Write-Host ("  {0} {1}" -f $OK, $msg) -ForegroundColor Green;  Log "OK: $msg" }
function StepInfo([string]$msg) { Write-Host ("    {0}"    -f $msg)      -ForegroundColor Gray;   Log "INFO: $msg" }
function StepWarn([string]$msg) { Write-Host ("  ! {0}"    -f $msg)      -ForegroundColor Yellow; Log "WARN: $msg" }

# Kullaniciya kurulum log klasorunu acma secenegi sunar (L -> explorer).
function Show-LogFolderOption {
    Write-Host ""
    $open = Read-Host "  Kurulum log klasorunu acmak icin L, cikmak icin Enter"
    if ($open -match '^\s*[lL]') {
        try { Start-Process explorer.exe $LogDir; Log "Log klasoru kullanici tarafindan acildi: $LogDir" }
        catch { Log "Log klasoru acilamadi: $($_.Exception.Message)" }
    }
}

# Kullaniciya anlasilir mesaj; teknik ayrinti yalnizca log dosyasina.
function Fail([string]$userMsg, [string]$tech) {
    Write-Host ""
    Write-Host ("  {0} {1}" -f $NO, $userMsg) -ForegroundColor Red
    Log "FAIL: $userMsg | TECH: $tech"
    Write-Host ""
    Write-Host "  Kurulum tamamlanamadi. Lutfen su log dosyasini BT ekibine iletin:" -ForegroundColor Yellow
    Write-Host "    $LogFile" -ForegroundColor Yellow
    if (-not $NoPause) { Write-Host ""; Read-Host "Kapatmak icin Enter'a basin" | Out-Null }
    exit 1
}

# ── Baslik ────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "   $AppName" -ForegroundColor Cyan
Write-Host "   Kurulum Sihirbazi" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Log "Kurulum baslatildi. Kullanici=$env:USERNAME Makine=$env:COMPUTERNAME"

# ── ShareRoot dogrulamasi ─────────────────────────────────────────────────────
# ShareRoot ASLA %CD% / %~dp0 uzerinden turetilmez; parametreden ya da varsayilandan gelir.
# Kurulum.bat de bunu acikca -ShareRoot "\\10.0.0.169\YonetimPublish" olarak gecirir.
$ShareRoot = "$ShareRoot".Trim().TrimEnd('\')
Log "ShareRoot=$ShareRoot"

$invalidShare =
    [string]::IsNullOrWhiteSpace($ShareRoot) -or
    $ShareRoot -eq '\' -or $ShareRoot -eq '\\' -or
    ($ShareRoot -match '^[A-Za-z]:$') -or                # surucu koku (C:)
    (-not ($ShareRoot -match '^\\\\[^\\]+\\[^\\]+'))      # gecerli UNC degil (\\sunucu\pay bekleniyor)

if ($invalidShare) {
    Fail "Kurulum sunucusu yolu gecersiz. Lutfen BT ekibine basvurun." "invalid ShareRoot: '$ShareRoot' (beklenen: \\10.0.0.169\YonetimPublish)"
}

# Dogrulanmis ShareRoot ile hedef yollar.
$Manifest = Join-Path $ShareRoot $ManifestName

# ── 1) Ortam kontrolu ─────────────────────────────────────────────────────────
Say "  Ortam kontrol ediliyor..." "Cyan"
try {
    $os  = Get-CimInstance Win32_OperatingSystem -ErrorAction Stop
    $ver = [version]$os.Version
    Log "Windows surumu: $($os.Caption) ($($os.Version)) $($os.OSArchitecture)"
    if ($ver.Major -lt 10) {
        Fail "Bu uygulama Windows 10 veya Windows 11 gerektirir. Bilgisayariniz: $($os.Caption)." "OSVersion=$($os.Version)"
    }
    $winName = if ($ver.Build -ge 22000) { "Windows 11" } else { "Windows 10" }
    StepOk "Ortam uygun ($winName)"
}
catch { Fail "Windows surumu dogrulanamadi." "$($_.Exception.Message)" }

# ── 2) .NET Desktop Runtime kontrolu ──────────────────────────────────────────
# WPF uygulamasi icin GEREKLI: Microsoft.NETCore.App 9.x + Microsoft.WindowsDesktop.App 9.x (x64).
# Yalnizca 'dotnet --list-runtimes' cikstisina guvenmeyiz; x64 paylasim klasorlerini de dogrulariz.
Say "  .NET Desktop Runtime kontrol ediliyor..." "Cyan"

# x64 .NET her zaman '%ProgramFiles%\dotnet' altindadir (x86 ise 'Program Files (x86)').
$DotnetX64Root = Join-Path $env:ProgramFiles "dotnet"

function Test-VersionDir([string]$dir, [int]$minMajor) {
    if (-not (Test-Path $dir)) { return $false }
    $hit = Get-ChildItem $dir -Directory -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -match '^(\d+)\.' -and [int]$Matches[1] -ge $minMajor
    }
    return [bool]$hit
}

function Test-Runtimes([int]$minMajor) {
    # Kaynak 1: dotnet CLI (x86 yollari haric)
    $raw = ""
    try { $raw = (& dotnet --list-runtimes 2>$null | Out-String) } catch { $raw = "" }

    $netcoreCli = $false; $desktopCli = $false
    foreach ($line in ($raw -split "`r?`n")) {
        if ($line -match '^Microsoft\.NETCore\.App\s+(\d+)\.\d+\.\d+\s+\[(.+)\]$' -and
            [int]$Matches[1] -ge $minMajor -and $Matches[2] -notmatch '\(x86\)') { $netcoreCli = $true }
        if ($line -match '^Microsoft\.WindowsDesktop\.App\s+(\d+)\.\d+\.\d+\s+\[(.+)\]$' -and
            [int]$Matches[1] -ge $minMajor -and $Matches[2] -notmatch '\(x86\)') { $desktopCli = $true }
    }

    # Kaynak 2: x64 dosya sistemi
    $netcoreFs = Test-VersionDir (Join-Path $DotnetX64Root "shared\Microsoft.NETCore.App") $minMajor
    $desktopFs = Test-VersionDir (Join-Path $DotnetX64Root "shared\Microsoft.WindowsDesktop.App") $minMajor

    $netcore = ($netcoreCli -or $netcoreFs)
    $desktop = ($desktopCli -or $desktopFs)
    return [pscustomobject]@{
        Raw = $raw.Trim(); NetCore = $netcore; Desktop = $desktop; Ok = ($netcore -and $desktop)
    }
}

# Runtime installer'i indirir (aka.ms dogrudan x64). Basarisizsa $null.
function Get-RuntimeInstaller([string]$url) {
    $dest = Join-Path $env:TEMP "windowsdesktop-runtime-9-win-x64.exe"
    try {
        try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 } catch { }
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing -ErrorAction Stop
        if ((Test-Path $dest) -and ((Get-Item $dest).Length -gt 1MB)) { return $dest }
        return $null
    }
    catch { Log "Runtime indirilemedi: $($_.Exception.Message)"; return $null }
}

$rt = Test-Runtimes $MinDotnetMajor
Log "dotnet --list-runtimes ciktisi:`r`n$($rt.Raw)"
Log "NETCore.App >= $MinDotnetMajor (x64) bulundu: $($rt.NetCore)"
Log "WindowsDesktop.App >= $MinDotnetMajor (x64) bulundu: $($rt.Desktop)"

if (-not $rt.Ok) {
    StepWarn ".NET $MinDotnetMajor Desktop Runtime eksik (NETCore=$($rt.NetCore), Desktop=$($rt.Desktop)). Otomatik kurulum deneniyor..."

    # Kaynak onceligi: 1) parametre 2) kurulum klasorundeki yerel exe 3) aka.ms indirme.
    $installer = $null; $installerSource = ""
    if (-not [string]::IsNullOrWhiteSpace($RuntimeInstallerPath) -and (Test-Path $RuntimeInstallerPath)) {
        $installer = $RuntimeInstallerPath; $installerSource = "parametre"
    }
    if (-not $installer) {
        $localRt = Get-ChildItem -Path $ScriptDir -Filter "windowsdesktop-runtime-9*-win-x64.exe" -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending | Select-Object -First 1
        if ($localRt) { $installer = $localRt.FullName; $installerSource = "yerel klasor" }
    }
    if (-not $installer) {
        StepInfo "Gerekli bilesen indiriliyor (aka.ms, x64)..."
        $installer = Get-RuntimeInstaller $RuntimeDirectUrl
        if ($installer) { $installerSource = "aka.ms indirme" }
    }
    Log "Runtime installer kaynagi: $installerSource | dosya: $installer"

    if ($installer) {
        StepInfo "Gerekli bilesen kuruluyor, bu birkac dakika surebilir (yonetici izni istenebilir)..."
        try {
            $p = Start-Process -FilePath $installer -ArgumentList "/install", "/quiet", "/norestart" -Verb RunAs -Wait -PassThru
            Log "Runtime kurulum exit code: $($p.ExitCode)"
        }
        catch { Log "Runtime kurulum hatasi: $($_.Exception.Message)" }

        # Kurulumdan sonra tekrar dogrula.
        $rt = Test-Runtimes $MinDotnetMajor
        Log "Kurulum sonrasi NETCore=$($rt.NetCore) Desktop=$($rt.Desktop)"
    }

    # Hala eksikse: SON secenek olarak resmi sayfaya yonlendir.
    if (-not $rt.Ok) {
        Log "Runtime otomatik kurulamadi. Son secenek: $RuntimeDownloadUrl"
        Write-Host ""
        Write-Host "  Otomatik kurulum tamamlanamadi. Lutfen 'Desktop Runtime $MinDotnetMajor (x64)' surumunu" -ForegroundColor Yellow
        Write-Host "  acilan sayfadan indirip kurun, sonra kurulumu tekrar baslatin:" -ForegroundColor Yellow
        Write-Host "    $RuntimeDownloadUrl" -ForegroundColor Cyan
        try { Start-Process $RuntimeDownloadUrl | Out-Null } catch { }
        Fail "Gerekli .NET $MinDotnetMajor Desktop Runtime kurulamadi. Yukaridaki adresten kurup tekrar deneyin." "runtime install failed (NETCore=$($rt.NetCore), Desktop=$($rt.Desktop))"
    }
    StepOk ".NET $MinDotnetMajor Desktop Runtime kuruldu ve dogrulandi"
}
else {
    StepOk ".NET $MinDotnetMajor Desktop Runtime dogrulandi (NETCore + WindowsDesktop, x64)"
}

# ── 3) Sunucu baglantisi ──────────────────────────────────────────────────────
Say "  Sunucu baglantisi kontrol ediliyor..." "Cyan"
if (-not (Test-Path $ShareRoot)) {
    Fail "Kurulum sunucusuna ulasilamadi. Sirket aginiza bagli oldugunuzdan emin olup tekrar deneyin." "Test-Path failed: $ShareRoot"
}
StepOk "Sunucu baglantisi tamam"

# ── 4) Kurulum paketi (manifest) + guncelleme (version.json) kontrolu ─────────
Say "  Kurulum paketi kontrol ediliyor..." "Cyan"
$VersionJson = Join-Path $ShareRoot "version.json"
$manifestOk  = Test-Path $Manifest
$versionOk   = Test-Path $VersionJson
Log "Publish manifest erisim: $manifestOk ($Manifest)"
Log "version.json erisim: $versionOk ($VersionJson)"

if (-not $manifestOk -and -not $versionOk) {
    Fail "Kurulum/guncelleme sunucusuna ulasilamadi. Aginizi kontrol edip tekrar deneyin veya BT ekibine basvurun." "manifest & version.json missing/unreachable at $ShareRoot"
}
if (-not $manifestOk) {
    Fail "Kurulum paketi sunucuda bulunamadi. Lutfen BT ekibine basvurun." "manifest not found: $Manifest"
}
if (-not $versionOk) {
    # Kurulum yapilabilir ama guncelleme kontrolu etkilenebilir -> uyar, durdurma.
    StepWarn "version.json bulunamadi; kurulum surecek ancak guncelleme kontrolu etkilenebilir."
}
StepOk "Kurulum paketi dogrulandi"

# ── 5) Uygulamayi kur (ClickOnce baslat) ──────────────────────────────────────
Say "  Uygulama kuruluyor..." "Cyan"
try {
    # ClickOnce: .application manifestini baslatmak kurulum/guncelleme akisini tetikler.
    Start-Process $Manifest | Out-Null
    Log "ClickOnce baslatildi: $Manifest"
}
catch { Fail "Uygulama kurulumu baslatilamadi. Lutfen tekrar deneyin veya BT ekibine basvurun." "$($_.Exception.Message)" }

# ClickOnce ayri bir pencerede acilir; kisa bir sure taninir.
Start-Sleep -Seconds 3

Write-Host ""
Write-Host ("  {0} ClickOnce kurulum penceresi acildi." -f $OK) -ForegroundColor Cyan
Write-Host "  Lutfen ekrandaki Install/Kur butonuna basarak kurulumu tamamlayin." -ForegroundColor Cyan
Write-Host ""
Write-Host "  Not: 'Unknown Publisher' (Bilinmeyen Yayimci) uyarisi cikabilir -- bu bir HATA DEGILDIR." -ForegroundColor Gray
Write-Host "       Kurulumu surdurmek icin Install/Kur deyin. (Kalici cozum: code signing sertifikasi.)" -ForegroundColor Gray
Write-Host ""
Write-Host "  ClickOnce'in basari/iptal durumu buradan guvenilir sekilde OTOMATIK algilanamaz;" -ForegroundColor DarkGray
Write-Host "  bu nedenle asagida sizden onay isteniyor." -ForegroundColor DarkGray
Write-Host ""

# ── Kullanici onayi (tamamlandi / iptal) ──────────────────────────────────────
$confirm = Read-Host "  Kurulum ekranindaki adimlari tamamladiysaniz Enter'a basin. Iptal ettiyseniz C yazip Enter'a basin"

if ($confirm -match '^\s*[cC]') {
    Log "Kurulum kullanici tarafindan iptal edildi."
    Write-Host ""
    Write-Host ("  {0} Kurulum kullanici tarafindan iptal edildi." -f $NO) -ForegroundColor Yellow
    Write-Host "  Kurulumu daha sonra tekrar baslatabilirsiniz." -ForegroundColor Gray
    Show-LogFolderOption
    exit 1
}

Log "Kurulum tamamlandi kabul edildi."

# ── Opsiyonel masaustu kisayolu (kurulum onaylandiktan SONRA) ─────────────────
if ($CreateDesktopShortcut) {
    Say "  Masaustu kisayolu hazirlaniyor..." "Cyan"
    try {
        # ClickOnce, Start Menu altinda '<AppName>.appref-ms' olusturur.
        $startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
        $desktop   = [Environment]::GetFolderPath("Desktop")
        $deadline  = (Get-Date).AddSeconds(30)
        $apprefs   = @()
        do {
            Start-Sleep -Seconds 3
            $apprefs = Get-ChildItem $startMenu -Recurse -Filter "*.appref-ms" -ErrorAction SilentlyContinue |
                Where-Object { $_.BaseName -like "*Yonetim*" }
        } while ($apprefs.Count -eq 0 -and (Get-Date) -lt $deadline)

        if ($apprefs.Count -gt 0) {
            Copy-Item $apprefs[0].FullName (Join-Path $desktop $apprefs[0].Name) -Force
            StepOk "Masaustu kisayolu olusturuldu"
        } else {
            StepWarn "Kisayol henuz bulunamadi; Baslat menusunden erisebilirsiniz."
        }
    }
    catch { StepWarn "Masaustu kisayolu olusturulamadi (Baslat menusunden erisebilirsiniz)." ; Log "shortcut error: $($_.Exception.Message)" }
}

# ── Tamamlandi ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host ("  {0} Kurulum tamamlandi." -f $OK) -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  $AppName artik Baslat menusunden acilabilir." -ForegroundColor Green
Write-Host "  Kurulum logu: $LogFile" -ForegroundColor DarkGray
Log "Kurulum akisi sorunsuz tamamlandi (kullanici onayli)."

# Log klasorunu acma secenegi
Show-LogFolderOption

Write-Host ""
Write-Host "  Bu pencere 5 saniye icinde kapanacaktir." -ForegroundColor DarkGray
Start-Sleep -Seconds 5
exit 0
