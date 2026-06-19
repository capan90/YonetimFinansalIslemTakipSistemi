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

.PARAMETER Version
    Yayınlanacak sürüm numarası. csproj AssemblyVersion ile aynı olmalı.
    Örnek: "1.0.0.0"

.PARAMETER Sign
    $true ise sertifika ile imzalar. Varsayılan: $false (yerel test için)

.EXAMPLE
    .\Publish-ClickOnce.ps1 -Version "1.0.0.1" -Sign $false
#>
param(
    [string]$Version = "1.0.0.0",
    [bool]$Sign = $false
)

$ErrorActionPreference = "Stop"
$env:DOTNET_ROLL_FORWARD = "Major"

# Sabitler
$ProjectPath = "$PSScriptRoot\src\YonetimFinansalIslemTakipSistemi.UI\YonetimFinansalIslemTakipSistemi.UI.csproj"
$BuildOutput  = "$env:TEMP\YonetimBuild"
$PublishDir   = "C:\Apps\Yonetim\Publish"
$AppName      = "Yönetim Finansal İşlem Takip Sistemi"
$ExeName      = "YonetimFinansalIslemTakipSistemi.UI.exe"
$ManifestBase = "YonetimFinansalIslemTakipSistemi.UI"
$CertThumb    = "0136460438B6DED7F20498C00F7D3AB4C1E1B203"
$ProviderUrl  = "\\localhost\YonetimPublish\YonetimFinansalIslemTakipSistemi.application"

# Sürüm klasörü: noktalar alt çizgiye çevrilir
$VersionFolder = $Version -replace "\.", "_"
$AppFilesDir   = "$PublishDir\Application Files\${ManifestBase}_${VersionFolder}"

Write-Host "[1/6] Build yapılıyor: $Version..." -ForegroundColor Cyan

# Temizle + build
Remove-Item -Recurse -Force $BuildOutput -ErrorAction SilentlyContinue
dotnet publish $ProjectPath -c Release -o $BuildOutput /p:AssemblyVersion=$Version --nologo -v minimal

Write-Host "[2/6] Launcher ekleniyor..." -ForegroundColor Cyan

dotnet-mage -AddLauncher $ExeName -TargetDirectory $BuildOutput

Write-Host "[3/6] Application Files klasörü hazırlanıyor..." -ForegroundColor Cyan

New-Item -ItemType Directory -Force -Path $AppFilesDir | Out-Null
Copy-Item -Path "$BuildOutput\*" -Destination $AppFilesDir -Recurse -Force

Write-Host "[4/6] Application manifest oluşturuluyor..." -ForegroundColor Cyan

$AppManifest = "$AppFilesDir\$ManifestBase.exe.manifest"
dotnet-mage -New Application `
    -ToFile $AppManifest `
    -Name $AppName `
    -Version $Version `
    -Processor "msil" `
    -FromDirectory $AppFilesDir

Write-Host "[5/6] Deployment manifest oluşturuluyor..." -ForegroundColor Cyan

$DeployManifest = "$PublishDir\$ManifestBase.application"
dotnet-mage -New Deployment `
    -ToFile $DeployManifest `
    -Name $AppName `
    -Version $Version `
    -AppManifest $AppManifest `
    -Install true `
    -IncludeProviderURL true `
    -ProviderURL $ProviderUrl `
    -Publisher "YonetimApp"

# İsteğe bağlı imzalama
if ($Sign) {
    Write-Host "  İmzalanıyor: $AppManifest" -ForegroundColor Yellow
    dotnet-mage -Sign $AppManifest -CertHash $CertThumb
    Write-Host "  İmzalanıyor: $DeployManifest" -ForegroundColor Yellow
    dotnet-mage -Sign $DeployManifest -CertHash $CertThumb
}

Write-Host "[6/6] version.json yazılıyor..." -ForegroundColor Cyan

$Json = "{`"version`":`"$Version`"}"
Set-Content -Path "$PublishDir\version.json" -Value $Json -Encoding utf8

Write-Host ""
Write-Host "Publish tamamlandi!" -ForegroundColor Green
Write-Host "  Konum    : $PublishDir"
Write-Host "  Surum    : $Version"
Write-Host "  Manifest : $ManifestBase.application"
Write-Host ""
Write-Host "SMB Share (admin PowerShell'de bir kez calistir):" -ForegroundColor Yellow
Write-Host "  New-SmbShare -Name 'YonetimPublish' -Path '$PublishDir' -FullAccess 'Everyone'"
Write-Host ""
Write-Host "Kurulum icin: \\localhost\YonetimPublish\$ManifestBase.application"
