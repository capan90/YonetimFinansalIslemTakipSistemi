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
$Manifest   = Join-Path $ShareRoot $ManifestName

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
Log "Kurulum baslatildi. Kullanici=$env:USERNAME Makine=$env:COMPUTERNAME ShareRoot=$ShareRoot"

# ── 1) Ortam kontrolu ─────────────────────────────────────────────────────────
Say "  Ortam kontrol ediliyor..." "Cyan"
try {
    $os  = Get-CimInstance Win32_OperatingSystem -ErrorAction Stop
    $ver = [version]$os.Version
    if ($ver.Major -lt 10) {
        Fail "Bu uygulama Windows 10 veya Windows 11 gerektirir. Bilgisayariniz: $($os.Caption)." "OSVersion=$($os.Version)"
    }
    $winName = if ($ver.Build -ge 22000) { "Windows 11" } else { "Windows 10" }
    StepOk "Ortam uygun ($winName)"
}
catch { Fail "Windows surumu dogrulanamadi." "$($_.Exception.Message)" }

# ── 2) .NET Desktop Runtime kontrolu ──────────────────────────────────────────
Say "  .NET Desktop Runtime kontrol ediliyor..." "Cyan"

function Test-DesktopRuntime([int]$minMajor) {
    $lines = $null
    try { $lines = & dotnet --list-runtimes 2>$null } catch { $lines = $null }
    if (-not $lines) { return [pscustomobject]@{ Found = $false; Version = $null } }
    $found = $null
    foreach ($l in $lines) {
        if ($l -match '^Microsoft\.WindowsDesktop\.App\s+(\d+)\.(\d+)\.(\d+)') {
            if ([int]$Matches[1] -ge $minMajor) { $found = "$($Matches[1]).$($Matches[2]).$($Matches[3])" }
        }
    }
    return [pscustomobject]@{ Found = [bool]$found; Version = $found }
}

$rt = Test-DesktopRuntime $MinDotnetMajor
if (-not $rt.Found) {
    StepWarn ".NET Desktop Runtime $MinDotnetMajor bulunamadi."

    # Yerel installer: parametreyle verilen ya da kurulum klasorunde aranan.
    $installer = $null
    if (-not [string]::IsNullOrWhiteSpace($RuntimeInstallerPath) -and (Test-Path $RuntimeInstallerPath)) {
        $installer = $RuntimeInstallerPath
    } else {
        $localRt = Get-ChildItem -Path $ScriptDir -Filter "windowsdesktop-runtime-*.exe" -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending | Select-Object -First 1
        if ($localRt) { $installer = $localRt.FullName }
    }

    if ($installer) {
        StepInfo "Gerekli bilesen kuruluyor, bu birkac dakika surebilir..."
        Log "Runtime installer: $installer"
        try {
            $p = Start-Process -FilePath $installer -ArgumentList "/install", "/quiet", "/norestart" -Verb RunAs -Wait -PassThru
            Log "Runtime installer exit code: $($p.ExitCode)"
        }
        catch { Fail "Gerekli bilesen kurulamadi. Kurulumu yonetici izniyle tekrar deneyin." "$($_.Exception.Message)" }

        $rt = Test-DesktopRuntime $MinDotnetMajor
        if (-not $rt.Found) {
            Fail "Gerekli .NET bileseni kurulduktan sonra dogrulanamadi. Bilgisayari yeniden baslatip tekrar deneyin." "runtime still missing after local install"
        }
        StepOk ".NET Desktop Runtime $($rt.Version) kuruldu"
    }
    else {
        # Yerel installer yok -> resmi indirme sayfasina yonlendir.
        Log "Runtime yok, yerel installer yok. Yonlendirme: $RuntimeDownloadUrl"
        Write-Host ""
        Write-Host "  Uygulamanin calismasi icin 'Microsoft .NET Desktop Runtime $MinDotnetMajor' gereklidir." -ForegroundColor Yellow
        Write-Host "  Acilan sayfadan 'Desktop Runtime' (x64) surumunu indirip kurun, sonra kurulumu tekrar calistirin." -ForegroundColor Yellow
        Write-Host "    $RuntimeDownloadUrl" -ForegroundColor Cyan
        try { Start-Process $RuntimeDownloadUrl | Out-Null } catch { }
        Fail "Gerekli .NET bileseni eksik. Yukaridaki adresten kurup kurulumu tekrar baslatin." "runtime missing, no local installer"
    }
}
else {
    StepOk ".NET Desktop Runtime $($rt.Version) bulundu"
}

# ── 3) Sunucu baglantisi ──────────────────────────────────────────────────────
Say "  Sunucu baglantisi kontrol ediliyor..." "Cyan"
if (-not (Test-Path $ShareRoot)) {
    Fail "Kurulum sunucusuna ulasilamadi. Sirket aginiza bagli oldugunuzdan emin olup tekrar deneyin." "Test-Path failed: $ShareRoot"
}
StepOk "Sunucu baglantisi tamam"

# ── 4) Kurulum paketi (manifest) kontrolu ─────────────────────────────────────
Say "  Kurulum paketi kontrol ediliyor..." "Cyan"
if (-not (Test-Path $Manifest)) {
    Fail "Kurulum paketi sunucuda bulunamadi. Lutfen BT ekibine basvurun." "manifest not found: $Manifest"
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
StepOk "Kurulum baslatildi (ekrandaki adimlari onaylayin)"

# ── 6) Opsiyonel masaustu kisayolu ────────────────────────────────────────────
if ($CreateDesktopShortcut) {
    Say "  Masaustu kisayolu hazirlaniyor..." "Cyan"
    try {
        # ClickOnce, Start Menu altinda '<AppName>.appref-ms' olusturur. Kurulum bitince kopyalanir.
        $startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
        $desktop   = [Environment]::GetFolderPath("Desktop")
        $deadline  = (Get-Date).AddSeconds(90)
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
Write-Host "  (Ilk kurulumda ClickOnce onay ekranini onaylamaniz gerekebilir.)" -ForegroundColor Gray
Write-Host ""
Write-Host "  Kurulum logu: $LogFile" -ForegroundColor DarkGray
Log "Kurulum akisi sorunsuz tamamlandi."

if (-not $NoPause) { Write-Host ""; Read-Host "Kapatmak icin Enter'a basin" | Out-Null }
exit 0
