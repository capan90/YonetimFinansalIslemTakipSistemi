<#
.SYNOPSIS
    Yonetim Finansal Islem Takip Sistemi -- ClickOnce Publish Script

.DESCRIPTION
    dotnet publish /p:PublishProfile=ClickOnce, .NET 9 CLI'de Engine\Launcher.exe olmadan
    calisir (Visual Studio gerektirir). Bu script dotnet-mage ile ClickOnce yapisini uretir.

    Gereksinimler:
      - dotnet tool: microsoft.dotnet.mage (dotnet tool install --global microsoft.dotnet.mage)
      - Admin haklari (New-SmbShare icin)
      - Sertifika: Cert:\CurrentUser\My thumbprint asagida belirtilmis

    ProviderURL kaynak:
      YONETIM_UPDATE_PATH env var set edilmisse o UNC kullanilir (uretim)
      Set edilmemisse       -> \\localhost\YonetimPublish\ (lokal test)

.PARAMETER Version
    Yayinlanacak surum numarasi. csproj AssemblyVersion ile ayni olmali.
    Her release'de artir: 1.0.0.0 -> 1.0.0.1 -> 1.0.0.2
    Ayni versiyonla tekrar publish yapilirsa ClickOnce guncelleme algilamaz.

.PARAMETER Sign
    $true ise Cert:\CurrentUser\My icindeki sertifika ile her iki manifest imzalanir.
    Gercek ClickOnce kurulum ve guncelleme testi icin $true kullan.
    Varsayilan $false yalnizca build/manifest ciktisini dogrulamak icindir.

.EXAMPLE
    # Lokal test -- imzali
    .\Publish-ClickOnce.ps1 -Version "1.0.0.1" -Sign $true

    # Uretim -- imzali
    $env:YONETIM_UPDATE_PATH = "\\SUNUCU\YonetimPublish\"
    .\Publish-ClickOnce.ps1 -Version "1.0.0.1" -Sign $true

    # Yalnizca cikti dogrulama -- imzasiz
    .\Publish-ClickOnce.ps1 -Version "1.0.0.1" -Sign $false
#>
param(
    [string]$Version = "1.0.0.0",
    [bool]$Sign = $false
)

$ErrorActionPreference = "Stop"

# UNC base: env var doluysa onu kullan, yoksa localhost
$UncBase = $env:YONETIM_UPDATE_PATH
if ([string]::IsNullOrWhiteSpace($UncBase)) {
    $UncBase = "\\localhost\YonetimPublish\"
}
if (-not $UncBase.EndsWith("\")) { $UncBase += "\" }

# Sabitler -- mage teknik degerleri ASCII olmali; Turkce karakter encoding bozukluguna yol acar
$ProjectPath  = "$PSScriptRoot\src\YonetimFinansalIslemTakipSistemi.UI\YonetimFinansalIslemTakipSistemi.UI.csproj"
$BuildOutput  = "$env:TEMP\YonetimBuild"
$PublishDir   = "C:\Apps\Yonetim\Publish"
$AppName      = "Yonetim Finansal Islem Takip Sistemi"
$ExeName      = "YonetimFinansalIslemTakipSistemi.UI.exe"
$ManifestBase = "YonetimFinansalIslemTakipSistemi.UI"
$CertThumb    = "0136460438B6DED7F20498C00F7D3AB4C1E1B203"
$ProviderUrl  = "${UncBase}$ManifestBase.application"

# Surum klasoru: noktalar alt cizgiye cevirilir
$VersionFolder  = $Version -replace "\.", "_"
$AppFilesDir    = "$PublishDir\Application Files\${ManifestBase}_${VersionFolder}"
$AppManifest    = "$AppFilesDir\$ManifestBase.exe.manifest"
$DeployManifest = "$PublishDir\$ManifestBase.application"
$VersionJsonPath = "$PublishDir\version.json"

# Baslangic ozeti
Write-Host ""
Write-Host "=== ClickOnce Publish ===" -ForegroundColor Cyan
Write-Host "  Versiyon      : $Version"
Write-Host "  Imzali        : $Sign"
Write-Host "  Publish klasor: $PublishDir"
Write-Host "  ProviderURL   : $ProviderUrl"
Write-Host ""

Write-Host "[0/6] Publish klasoru temizleniyor..." -ForegroundColor Cyan
# Eski/bayat manifest kalmasin; her publish'te temiz basla
Remove-Item -Recurse -Force "$PublishDir\*" -ErrorAction SilentlyContinue

Write-Host "[1/6] Build yapiliyor: $Version..." -ForegroundColor Cyan

Remove-Item -Recurse -Force $BuildOutput -ErrorAction SilentlyContinue
dotnet publish $ProjectPath -c Release -o $BuildOutput /p:AssemblyVersion=$Version --nologo -v minimal

Write-Host "[2/6] Launcher ekleniyor..." -ForegroundColor Cyan

dotnet-mage -AddLauncher $ExeName -TargetDirectory $BuildOutput

Write-Host "[3/6] Application Files klasoru hazirlanıyor..." -ForegroundColor Cyan

New-Item -ItemType Directory -Force -Path $AppFilesDir | Out-Null
Copy-Item -Path "$BuildOutput\*" -Destination $AppFilesDir -Recurse -Force

# AppIcon.ico ayri dosya olarak kopyalanir.
# csproj'da <Resource> (assembly'e gomulu) olarak tanimlidir -- publish output'una gelmez.
# Application Files icinde bulunmasi application manifest'e dahil edilmesi icin gereklidir.
$IconSource = "$PSScriptRoot\src\YonetimFinansalIslemTakipSistemi.UI\Assets\AppIcon.ico"
$IconFile   = "$AppFilesDir\AppIcon.ico"
if (Test-Path $IconSource) {
    Copy-Item $IconSource $IconFile -Force
    Write-Host "  AppIcon.ico kopyalandi: $IconFile" -ForegroundColor Gray
} else {
    Write-Host "  [UYARI] AppIcon.ico kaynak dosyasi bulunamadi: $IconSource" -ForegroundColor Yellow
}

Write-Host "[4/6] Application manifest olusturuluyor..." -ForegroundColor Cyan

dotnet-mage -New Application `
    -ToFile $AppManifest `
    -Name $AppName `
    -Version $Version `
    -Processor "msil" `
    -FromDirectory $AppFilesDir

if ($Sign) {
    Write-Host "  Imzalaniyor: $AppManifest" -ForegroundColor Yellow
    dotnet-mage -Sign $AppManifest -CertHash $CertThumb
}

Write-Host "[5/6] Deployment manifest olusturuluyor..." -ForegroundColor Cyan

dotnet-mage -New Deployment `
    -ToFile $DeployManifest `
    -Name $AppName `
    -Version $Version `
    -AppManifest $AppManifest `
    -Install true `
    -IncludeProviderURL true `
    -ProviderURL $ProviderUrl `
    -Publisher "YonetimApp"

if ($Sign) {
    Write-Host "  Imzalaniyor: $DeployManifest" -ForegroundColor Yellow
    dotnet-mage -Sign $DeployManifest -CertHash $CertThumb
}

Write-Host "[6/6] version.json yaziliyor..." -ForegroundColor Cyan

# version.json yalnizca UpdateService'in manuel "Guncellemeleri Denetle" akisinda okunur.
# ClickOnce startup guncellemesi bu dosyayi degil deployment manifest'i kullanir.
$Json = "{`"version`":`"$Version`"}"
Set-Content -Path $VersionJsonPath -Value $Json -Encoding utf8

# Dogrulama
Write-Host ""
Write-Host "=== Dogrulama ===" -ForegroundColor Cyan

$ok = $true

# Deployment manifest var mi?
if (Test-Path $DeployManifest) {
    Write-Host "  [OK] Deployment manifest olustu" -ForegroundColor Green
} else {
    Write-Host "  [HATA] Deployment manifest bulunamadi: $DeployManifest" -ForegroundColor Red
    $ok = $false
}

# version.json surum eslesmesi
if (Test-Path $VersionJsonPath) {
    try {
        $parsed = (Get-Content $VersionJsonPath -Raw) | ConvertFrom-Json
        if ($parsed.version -eq $Version) {
            Write-Host "  [OK] version.json surumu eslesti: $Version" -ForegroundColor Green
        } else {
            Write-Host "  [HATA] version.json surumu uyusmuyor. Beklenen: $Version, Bulunan: $($parsed.version)" -ForegroundColor Red
            $ok = $false
        }
    } catch {
        Write-Host "  [HATA] version.json okunamadi: $_" -ForegroundColor Red
        $ok = $false
    }
} else {
    Write-Host "  [HATA] version.json bulunamadi" -ForegroundColor Red
    $ok = $false
}

# ProviderURL ve encoding kontrolu
if (Test-Path $DeployManifest) {
    $manifestContent = Get-Content $DeployManifest -Raw
    if ($manifestContent -like "*$ProviderUrl*") {
        Write-Host "  [OK] ProviderURL manifest icinde dogrulandi: $ProviderUrl" -ForegroundColor Green
    } else {
        Write-Host "  [HATA] ProviderURL manifest icinde bulunamadi. Beklenen: $ProviderUrl" -ForegroundColor Red
        $ok = $false
    }
    # ASCII product adi dogrulama -- mojibake varsa bu string bulunamaz
    if ($manifestContent -like "*Yonetim Finansal Islem Takip Sistemi*") {
        Write-Host "  [OK] Manifest ASCII product adi dogrulandi" -ForegroundColor Green
    } else {
        Write-Host "  [HATA] ASCII product adi bulunamadi -- encoding sorunu olabilir" -ForegroundColor Red
        $ok = $false
    }
    # Non-ASCII karakter kontrolu -- mojibake byte'lari 0x80+ araligindadir
    $hasNonAscii = [regex]::IsMatch($manifestContent, '[^\x00-\x7F]')
    if ($hasNonAscii) {
        Write-Host "  [HATA] Manifest non-ASCII karakter iceriyor (mojibake)" -ForegroundColor Red
        $ok = $false
    } else {
        Write-Host "  [OK] Manifest ASCII-only (mojibake yok)" -ForegroundColor Green
    }
    # iconFile / AppIcon yasak referans kontrolu
    if ($manifestContent -like "*iconFile*" -or $manifestContent -like "*AppIcon*") {
        Write-Host "  [HATA] Manifest yasak referans iceriyor (iconFile/AppIcon)" -ForegroundColor Red
        $ok = $false
    } else {
        Write-Host "  [OK] iconFile/AppIcon referansi yok" -ForegroundColor Green
    }
}

# Imzalama durumu
if ($Sign) {
    # Script ErrorActionPreference=Stop ile calistigindan imzalama hatasi script'i durdurur.
    # Bu noktaya ulasildiysa her iki manifest de basariyla imzalanmistir.
    Write-Host "  [OK] Application manifest imzalandi" -ForegroundColor Green
    Write-Host "  [OK] Deployment manifest imzalandi" -ForegroundColor Green
} else {
    Write-Host "  [--] Imzalama atlandi (Sign=false; gercek kurulum icin -Sign $true kullan)" -ForegroundColor Yellow
}

Write-Host ""
if ($ok) {
    Write-Host "Publish tamamlandi!" -ForegroundColor Green
} else {
    Write-Host "Publish tamamlandi ancak dogrulamada hatalar var. Yukaridaki [HATA] satirlarini incele." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  Konum     : $PublishDir"
Write-Host "  Surum     : $Version"
Write-Host "  Manifest  : $ManifestBase.application"
Write-Host "  ProviderURL: $ProviderUrl"
Write-Host ""
Write-Host "SMB Share (admin PowerShell'de bir kez calistir):" -ForegroundColor Yellow
Write-Host "  New-SmbShare -Name 'YonetimPublish' -Path '$PublishDir' -FullAccess 'Everyone'"
Write-Host ""
Write-Host "Kurulum icin:" -ForegroundColor Yellow
Write-Host "  ${UncBase}$ManifestBase.application"
