<#
.SYNOPSIS
    PostgreSQL veritabanı yedeği alır.

.DESCRIPTION
    pg_dump ile yönetim_db'yi custom format (.backup) olarak yedekler.
    Bağlantı bilgilerini şu öncelik sırasıyla çözer:
      1. Açık parametreler
      2. YONETIM_DB_CONNECTION ortam değişkeni
      3. src\YonetimFinansalIslemTakipSistemi.UI\appsettings.json

    Şifre hiçbir zaman komut satırına yazılmaz; PGPASSWORD env var kullanılır.

.PARAMETER Database
    Veritabanı adı. Belirtilmezse bağlantı dizesinden okunur.

.PARAMETER Host
    Sunucu adresi. Belirtilmezse bağlantı dizesinden okunur.

.PARAMETER Port
    Port. Belirtilmezse bağlantı dizesinden okunur; o da yoksa 5432.

.PARAMETER Username
    Veritabanı kullanıcısı. Belirtilmezse bağlantı dizesinden okunur.

.PARAMETER BackupDirectory
    Yedek dosyalarının kaydedileceği klasör. Yoksa oluşturulur. Varsayılan: Backups

.EXAMPLE
    .\scripts\Backup-Database.ps1
    .\scripts\Backup-Database.ps1 -BackupDirectory "D:\DBBackups"
    .\scripts\Backup-Database.ps1 -Host prod-server -Database yonetim_db -Username yonetim_app -BackupDirectory "\\server\backups"
#>
[CmdletBinding()]
param(
    [string]$Database        = "",
    [string]$Host            = "",
    [string]$Port            = "",
    [string]$Username        = "",
    [string]$BackupDirectory = "Backups"
)

$ErrorActionPreference = "Stop"

# ── Yardımcı fonksiyonlar ────────────────────────────────────────────────────

function Parse-NpgsqlConnectionString([string]$connStr) {
    $map = @{}
    $connStr -split ";" | ForEach-Object {
        $pair = $_.Trim()
        if ($pair -match "^([^=]+)=(.*)$") {
            $map[$Matches[1].Trim().ToLower()] = $Matches[2].Trim()
        }
    }
    return $map
}

function Find-ConnectionString {
    # 1. Env var
    if ($env:YONETIM_DB_CONNECTION) { return $env:YONETIM_DB_CONNECTION }

    # 2. appsettings.json (script proje kökünden çalıştırıldığını varsayar)
    $scriptDir    = if ($PSScriptRoot) { $PSScriptRoot } else { Get-Location }
    $solutionRoot = Split-Path $scriptDir -Parent
    $settingsPath = Join-Path $solutionRoot "src\YonetimFinansalIslemTakipSistemi.UI\appsettings.json"

    if (Test-Path $settingsPath) {
        $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
        return $settings.ConnectionStrings.DefaultConnection
    }

    return $null
}

# ── Bağlantı bilgilerini çöz ─────────────────────────────────────────────────

$connStr = Find-ConnectionString
$map     = if ($connStr) { Parse-NpgsqlConnectionString $connStr } else { @{} }

function Resolve-Param([string]$explicit, [string]$key, [string]$fallback) {
    if ($explicit)                     { return $explicit }
    if ($map.ContainsKey($key))        { return $map[$key] }
    return $fallback
}

$dbHost = Resolve-Param $Host     "host"     "localhost"
$dbPort = Resolve-Param $Port     "port"     "5432"
$dbName = Resolve-Param $Database "database" "yonetim_db"
$dbUser = Resolve-Param $Username "username" "postgres"

# Şifre parametreden alınmaz — sadece bağlantı dizesinden veya mevcut PGPASSWORD'dan
$dbPass = if ($map.ContainsKey("password")) { $map["password"] } else { "" }

# ── Backup klasörü ────────────────────────────────────────────────────────────

if (-not (Test-Path $BackupDirectory)) {
    New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
    Write-Host "Backup klasörü oluşturuldu: $(Resolve-Path $BackupDirectory)"
}

$timestamp  = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupDirectory "${dbName}_${timestamp}.backup"

# ── pg_dump ───────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "========================================="
Write-Host " VERITABANI BACKUP"
Write-Host "========================================="
Write-Host "  Sunucu    : $dbHost`:$dbPort"
Write-Host "  Veritabanı: $dbName"
Write-Host "  Kullanıcı : $dbUser"
Write-Host "  Çıktı     : $backupFile"
Write-Host "-----------------------------------------"

if (-not $dbPass -and -not $env:PGPASSWORD) {
    Write-Warning "Şifre bulunamadı. pg_dump parola isteyebilir veya başarısız olabilir."
    Write-Warning "Çözüm: YONETIM_DB_CONNECTION veya PGPASSWORD ortam değişkenini ayarlayın."
}

$pgArgs = @(
    "--host=$dbHost",
    "--port=$dbPort",
    "--username=$dbUser",
    "--dbname=$dbName",
    "--format=custom",
    "--no-password",
    "--file=$backupFile"
)

$pgExit = 0
try {
    if ($dbPass) { $env:PGPASSWORD = $dbPass }

    & pg_dump @pgArgs
    $pgExit = $LASTEXITCODE
}
catch {
    Write-Error "pg_dump çalıştırılamadı: $_"
    Write-Error "pg_dump PATH'te mevcut olmalıdır. PostgreSQL kurulumunu ve PATH ayarını kontrol edin."
    exit 2
}
finally {
    # Şifreyi her durumda temizle
    if ($dbPass) { $env:PGPASSWORD = "" }
}

Write-Host ""
if ($pgExit -eq 0) {
    $sizeKb = [math]::Round((Get-Item $backupFile).Length / 1KB, 1)
    Write-Host "[BASARILI] Backup tamamlandi."
    Write-Host "  Dosya: $backupFile"
    Write-Host "  Boyut: $sizeKb KB"
    exit 0
} else {
    Write-Error "[HATA] pg_dump basarisiz oldu (exit: $pgExit). Yukaridaki mesajlari inceleyin."
    exit 1
}
