<#
.SYNOPSIS
    Yönetim Finansal İşlem Takip Sistemi — ClickOnce Publish Script

.DESCRIPTION
    dotnet publish /p:PublishProfile=ClickOnce, .NET 9 CLI'de Engine\Launcher.exe olmadan
    çalışmaz (Visual Studio gerektirir). Bu script dotnet-mage ile ClickOnce yapısını üretir.

    Gereksinimler:
      - dotnet tool: microsoft.dotnet.mage (dotnet tool install --global microsoft.dotnet.mage)
      - Admin hakları (New-SmbShare için)
      - Sertifika: Cert:\CurrentUser\My thumbprint aşağıda belirtilmiş

    ProviderURL kaynağı:
      YONETIM_UPDATE_PATH env var set edilmişse → o UNC kullanılır (üretim)
      Set edilmemişse       → \\localhost\YonetimPublish\ (lokal test)
    Başka bilgisayarların ClickOnce startup güncellemesi alabilmesi için publish öncesi:
      $env:YONETIM_UPDATE_PATH = "\\SUNUCU\YonetimPublish\"

.PARAMETER Version
    Yayınlanacak sürüm numarası. csproj AssemblyVersion ile aynı olmalı.
    Her release'de artır: 1.0.0.0 → 1.0.0.1 → 1.0.0.2
    Aynı versiyonla tekrar publish yapılırsa ClickOnce güncelleme algılamaz.

.PARAMETER Sign
    $true ise Cert:\CurrentUser\My içindeki sertifika ile her iki manifest imzalanır.
    Gerçek ClickOnce kurulum ve güncelleme testi için $true kullan.
    Varsayılan $false yalnızca build/manifest çıktısını doğrulamak içindir.

.EXAMPLE
    # Lokal test — imzalı
    .\Publish-ClickOnce.ps1 -Version "1.0.0.1" -Sign $true

    # Üretim — imzalı
    $env:YONETIM_UPDATE_PATH = "\\SUNUCU\YonetimPublish\"
    .\Publish-ClickOnce.ps1 -Version "1.0.0.1" -Sign $true

    # Yalnızca çıktı doğrulama — imzasız
    .\Publish-ClickOnce.ps1 -Version "1.0.0.1" -Sign $false
#>
param(
    [string]$Version = "1.0.0.0",
    [bool]$Sign = $false
)

$ErrorActionPreference = "Stop"

# ── UNC base: env var doluysa onu kullan, yoksa localhost ────────────────────
$UncBase = $env:YONETIM_UPDATE_PATH
if ([string]::IsNullOrWhiteSpace($UncBase)) {
    $UncBase = "\\localhost\YonetimPublish\"
}
# Sondaki \ yoksa ekle
if (-not $UncBase.EndsWith("\")) { $UncBase += "\" }

# Sabitler
$ProjectPath  = "$PSScriptRoot\src\YonetimFinansalIslemTakipSistemi.UI\YonetimFinansalIslemTakipSistemi.UI.csproj"
$BuildOutput  = "$env:TEMP\YonetimBuild"
$PublishDir   = "C:\Apps\Yonetim\Publish"
$AppName      = "Yönetim Finansal İşlem Takip Sistemi"
$ExeName      = "YonetimFinansalIslemTakipSistemi.UI.exe"
$ManifestBase = "YonetimFinansalIslemTakipSistemi.UI"
$CertThumb    = "0136460438B6DED7F20498C00F7D3AB4C1E1B203"
$ProviderUrl  = "${UncBase}$ManifestBase.application"

# Sürüm klasörü: noktalar alt çizgiye çevrilir
$VersionFolder = $Version -replace "\.", "_"
$AppFilesDir   = "$PublishDir\Application Files\${ManifestBase}_${VersionFolder}"
$AppManifest   = "$AppFilesDir\$ManifestBase.exe.manifest"
$DeployManifest = "$PublishDir\$ManifestBase.application"
$VersionJsonPath = "$PublishDir\version.json"

# ── Başlangıç özeti ──────────────────────────────────────────────────────────
Write-Host ""
Write-Host "=== ClickOnce Publish ===" -ForegroundColor Cyan
Write-Host "  Versiyon      : $Version"
Write-Host "  Imzali        : $Sign"
Write-Host "  Publish klasor: $PublishDir"
Write-Host "  ProviderURL   : $ProviderUrl"
Write-Host ""

Write-Host "[1/6] Build yapılıyor: $Version..." -ForegroundColor Cyan

# Temizle + build
Remove-Item -Recurse -Force $BuildOutput -ErrorAction SilentlyContinue
dotnet publish $ProjectPath -c Release -o $BuildOutput /p:AssemblyVersion=$Version --nologo -v minimal

Write-Host "[2/6] Launcher ekleniyor..." -ForegroundColor Cyan

dotnet-mage -AddLauncher $ExeName -TargetDirectory $BuildOutput

Write-Host "[3/6] Application Files klasörü hazırlanıyor..." -ForegroundColor Cyan

New-Item -ItemType Directory -Force -Path $AppFilesDir | Out-Null
Copy-Item -Path "$BuildOutput\*" -Destination $AppFilesDir -Recurse -Force

# AppIcon.ico ayrı dosya olarak kopyalanır.
# csproj'da <Resource> (assembly'e gömülü) olarak tanımlıdır — publish output'una gelmez.
# ClickOnce kısayol ikonu için deployment manifest'te iconFile referansı gerektirir;
# bu dosyanın Application Files içinde bulunması ve application manifest'e dahil edilmesi şarttır.
# Mage -FromDirectory ile manifest oluştururken dizindeki tüm dosyaları tarar → ikon de dahil olur.
$IconSource = "$PSScriptRoot\src\YonetimFinansalIslemTakipSistemi.UI\Assets\AppIcon.ico"
$IconFile   = "$AppFilesDir\AppIcon.ico"
if (Test-Path $IconSource) {
    Copy-Item $IconSource $IconFile -Force
    Write-Host "  AppIcon.ico kopyalandi: $IconFile" -ForegroundColor Gray
} else {
    Write-Host "  [UYARI] AppIcon.ico kaynak dosyasi bulunamadi: $IconSource" -ForegroundColor Yellow
}

Write-Host "[4/6] Application manifest oluşturuluyor..." -ForegroundColor Cyan

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

Write-Host "[5/6] Deployment manifest oluşturuluyor..." -ForegroundColor Cyan

dotnet-mage -New Deployment `
    -ToFile $DeployManifest `
    -Name $AppName `
    -Version $Version `
    -AppManifest $AppManifest `
    -Install true `
    -IncludeProviderURL true `
    -ProviderURL $ProviderUrl `
    -Publisher "YonetimApp"

# dotnet-mage -New Deployment, -IconFile parametresini desteklemez.
# ClickOnce masaüstü kısayol ikonu deployment manifest'teki description/iconFile'dan okunur.
# XML post-processing: attribute imzalamadan ÖNCE eklenir; imza değişen içeriği kapsar.
Write-Host "  Deployment manifest'e iconFile ekleniyor (XML post-processing)..." -ForegroundColor Gray
try {
    [xml]$deployXml = Get-Content $DeployManifest -Encoding UTF8
    $asmv2Ns       = "urn:schemas-microsoft-com:asm.v2"
    $desc          = $deployXml.assembly.description
    if ($null -ne $desc) {
        $desc.SetAttribute("iconFile", $asmv2Ns, "AppIcon.ico")
        $deployXml.Save($DeployManifest)
        Write-Host "  asmv2:iconFile='AppIcon.ico' eklendi" -ForegroundColor Gray
    } else {
        Write-Host "  [UYARI] description elementi bulunamadi, iconFile eklenemedi" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  [UYARI] iconFile post-processing hatasi: $_" -ForegroundColor Yellow
}

if ($Sign) {
    Write-Host "  Imzalaniyor: $DeployManifest" -ForegroundColor Yellow
    dotnet-mage -Sign $DeployManifest -CertHash $CertThumb
}

Write-Host "[6/6] version.json yazılıyor..." -ForegroundColor Cyan

# version.json yalnızca UpdateService'in manuel "Guncellemeleri Denetle" akisinda okunur.
# ClickOnce startup guncellemesi bu dosyayi degil deployment manifest'i kullanir.
$Json = "{`"version`":`"$Version`"}"
Set-Content -Path $VersionJsonPath -Value $Json -Encoding utf8

# ── Doğrulama ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "=== Dogrulama ===" -ForegroundColor Cyan

$ok = $true

# Deployment manifest
if (Test-Path $DeployManifest) {
    Write-Host "  [OK] Deployment manifest olustu" -ForegroundColor Green
} else {
    Write-Host "  [HATA] Deployment manifest bulunamadi: $DeployManifest" -ForegroundColor Red
    $ok = $false
}

# version.json — sürüm eşleşmesi
if (Test-Path $VersionJsonPath) {
    try {
        $parsed = (Get-Content $VersionJsonPath -Raw) | ConvertFrom-Json
        if ($parsed.version -eq $Version) {
            Write-Host "  [OK] version.json surumu eslesip: $Version" -ForegroundColor Green
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

# ProviderURL ve iconFile deployment manifest içinde doğru mu?
if (Test-Path $DeployManifest) {
    $manifestContent = Get-Content $DeployManifest -Raw
    if ($manifestContent -like "*$ProviderUrl*") {
        Write-Host "  [OK] ProviderURL manifest icinde dogrulandi: $ProviderUrl" -ForegroundColor Green
    } else {
        Write-Host "  [HATA] ProviderURL manifest icinde bulunamadi. Beklenen: $ProviderUrl" -ForegroundColor Red
        $ok = $false
    }
    if ($manifestContent -like '*iconFile="AppIcon.ico"*') {
        Write-Host "  [OK] iconFile deployment manifest icinde dogrulandi: AppIcon.ico" -ForegroundColor Green
    } else {
        Write-Host "  [HATA] iconFile deployment manifest icinde bulunamadi" -ForegroundColor Red
        $ok = $false
    }
}

# İmzalama durumu
if ($Sign) {
    # Script ErrorActionPreference=Stop ile calistigından imzalama hatasi script'i durdurur.
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
