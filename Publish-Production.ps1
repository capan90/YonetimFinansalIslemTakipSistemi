param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [int]$KeepLastVersions = 5
)

$ErrorActionPreference = "Stop"

$ServerPublishPath = "\\10.0.0.169\YonetimPublish"
$LocalPublishPath = "C:\Apps\Yonetim\Publish"
$ManifestName = "YonetimFinansalIslemTakipSistemi.UI.application"
$ProviderUrl = "$ServerPublishPath\$ManifestName"

$PublishLogsDir = ".\logs\publish"
$PublishHistoryFile = ".\logs\publish\PublishHistory.json"
$Date = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$PublishLogFile = Join-Path $PublishLogsDir "publish-$Version-$Date.log"

New-Item -ItemType Directory -Force $PublishLogsDir | Out-Null

function Write-Step {
    param(
        [string]$Message,
        [string]$Color = "White"
    )

    Write-Host $Message -ForegroundColor $Color
    Add-Content $PublishLogFile $Message
}

function Remove-OldClickOnceVersions {
    param(
        [string]$ApplicationFilesPath,
        [int]$Keep
    )

    if (-not (Test-Path $ApplicationFilesPath)) {
        return
    }

    $VersionDirs = Get-ChildItem $ApplicationFilesPath -Directory |
        Where-Object { $_.Name -like "YonetimFinansalIslemTakipSistemi.UI_*" } |
        Sort-Object LastWriteTime -Descending

    $OldDirs = $VersionDirs | Select-Object -Skip $Keep

    foreach ($Dir in $OldDirs) {
        Write-Step "Eski ClickOnce versiyonu siliniyor: $($Dir.FullName)" "DarkYellow"
        Remove-Item $Dir.FullName -Recurse -Force
    }
}

function Update-PublishHistory {
    param(
        [string]$HistoryFile,
        [string]$Version,
        [string]$ProviderUrl
    )

    $HistoryDir = Split-Path $HistoryFile -Parent
    New-Item -ItemType Directory -Force $HistoryDir | Out-Null

    if (Test-Path $HistoryFile) {
        $History = Get-Content $HistoryFile -Raw | ConvertFrom-Json
        if ($null -eq $History) {
            $History = @()
        }
    }
    else {
        $History = @()
    }

    if ($History -isnot [System.Array]) {
        $History = @($History)
    }

    $Entry = [PSCustomObject]@{
        version = $Version
        date = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        providerUrl = $ProviderUrl
        machine = $env:COMPUTERNAME
        user = $env:USERNAME
    }

    $History = @($History) + $Entry

    $History |
        ConvertTo-Json -Depth 5 |
        Set-Content $HistoryFile -Encoding UTF8
}

try {
    Write-Step "=== Production Publish ===" "Cyan"
    Write-Step "Version       : $Version"
    Write-Step "Local Publish : $LocalPublishPath"
    Write-Step "Server Publish: $ServerPublishPath"
    Write-Step "Provider URL  : $ProviderUrl"
    Write-Step "Log File      : $PublishLogFile"
    Write-Step ""

    # --- Ortam yapilandirmasi (GUVENLIK: bu dosyaya sifre YAZILMAZ) ---
    # 1) Uygulama Production ortamiyla derlenir/yayimlanir.
    $env:YONETIM_ENVIRONMENT = "Production"

    # 2) Gizli degerler (varsa) gitignore'daki yerel scriptten yuklenir.
    #    Ornek Publish-Production.local.ps1 icerigi:
    #      $env:YONETIM_DB_CONNECTION = "Host=10.0.0.169;Port=5432;Database=yonetim_finansal;Username=yonetim_app;Password=..."
    #      $env:YONETIM_SMTP_PASSWORD = "..."
    $LocalSecrets = Join-Path $PSScriptRoot "Publish-Production.local.ps1"
    if (Test-Path $LocalSecrets) {
        Write-Step "[0/8] Yerel gizli ayarlar yukleniyor: Publish-Production.local.ps1" "Yellow"
        . $LocalSecrets
    }

    # 3) Uretim baglanti dizesi kaynagi: appsettings.Production.json istemci paketine gomulur.
    #    Bu dosya gitignore'da olup gercek sifreyi yalnizca publish makinesinde tutar.
    $ProdAppSettings = Join-Path $PSScriptRoot "src\YonetimFinansalIslemTakipSistemi.UI\appsettings.Production.json"
    if (-not (Test-Path $ProdAppSettings)) {
        throw "appsettings.Production.json bulunamadi: $ProdAppSettings`n" +
              "Publish makinesinde bu dosyayi gercek prod baglanti dizesiyle olusturun (gitignore'da, repoya commit edilmez). " +
              "Sablon: docs\config-production-example.json"
    }

    Write-Step "[1/8] Server publish yolu kontrol ediliyor..." "Yellow"
    if (-not (Test-Path $ServerPublishPath)) {
        throw "Server publish yolu bulunamadi: $ServerPublishPath"
    }

    Write-Step "[2/8] Clean / Build / Test..." "Yellow"
    dotnet clean 2>&1 | Tee-Object -FilePath $PublishLogFile -Append
    dotnet build -c Release 2>&1 | Tee-Object -FilePath $PublishLogFile -Append
    dotnet test -c Release 2>&1 | Tee-Object -FilePath $PublishLogFile -Append

    Write-Step "[3/8] ClickOnce publish aliniyor..." "Yellow"
    $env:YONETIM_UPDATE_PATH = "$ServerPublishPath\"

    # -Environment Production: yayimlanan appsettings.json'a AppEnvironment=Production gomulur,
    # boylece istemci ortam degiskeni olmadan Production DB'ye baglanir.
    .\Publish-ClickOnce.ps1 -Version $Version -Sign $true -Environment "Production" 2>&1 |
        Tee-Object -FilePath $PublishLogFile -Append

    Write-Step "[4/8] Lokal eski ClickOnce versiyonlari temizleniyor..." "Yellow"
    Remove-OldClickOnceVersions `
        -ApplicationFilesPath (Join-Path $LocalPublishPath "Application Files") `
        -Keep $KeepLastVersions

    Write-Step "[5/8] Sunucuya kopyalaniyor..." "Yellow"
    robocopy $LocalPublishPath $ServerPublishPath /MIR /R:3 /W:5 2>&1 |
        Tee-Object -FilePath $PublishLogFile -Append

    $RoboExitCode = $LASTEXITCODE
    if ($RoboExitCode -gt 7) {
        throw "Robocopy hata kodu: $RoboExitCode"
    }

    Write-Step "[6/8] Sunucu dosyalari dogrulaniyor..." "Yellow"

    $ServerManifest = Join-Path $ServerPublishPath $ManifestName
    $ServerVersionJson = Join-Path $ServerPublishPath "version.json"

    if (-not (Test-Path $ServerManifest)) {
        throw "Sunucuda manifest bulunamadi: $ServerManifest"
    }

    if (-not (Test-Path $ServerVersionJson)) {
        throw "Sunucuda version.json bulunamadi: $ServerVersionJson"
    }

    $VersionJson = Get-Content $ServerVersionJson -Raw
    if ($VersionJson -notmatch [regex]::Escape($Version)) {
        throw "Sunucudaki version.json beklenen versiyonu icermiyor: $Version"
    }

    $ManifestContent = Get-Content $ServerManifest -Raw
    if ($ManifestContent -notmatch [regex]::Escape($ProviderUrl)) {
        throw "Sunucudaki manifest ProviderURL beklenen degeri icermiyor: $ProviderUrl"
    }

    Write-Step "[7/8] Publish history guncelleniyor..." "Yellow"
    Update-PublishHistory `
        -HistoryFile $PublishHistoryFile `
        -Version $Version `
        -ProviderUrl $ProviderUrl

    Write-Step "[8/8] Ozet..." "Yellow"
    Write-Step ""
    Write-Step "==================================" "Green"
    Write-Step "Production Publish Basarili" "Green"
    Write-Step "==================================" "Green"
    Write-Step "Version       : $Version" "Green"
    Write-Step "Tests         : PASS" "Green"
    Write-Step "Publish       : PASS" "Green"
    Write-Step "Copy          : PASS" "Green"
    Write-Step "Manifest      : PASS" "Green"
    Write-Step "ProviderURL   : $ProviderUrl" "Green"
    Write-Step "Log           : $PublishLogFile" "Green"
    Write-Step "==================================" "Green"

    exit 0
}
catch {
    Write-Step ""
    Write-Step "PUBLISH HATASI: $($_.Exception.Message)" "Red"
    exit 1
}