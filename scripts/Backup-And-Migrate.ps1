<#
.SYNOPSIS
    Backup alır, ardından EF migration uygular.

.DESCRIPTION
    Güvenli migration akışı:
      1. Backup-Database.ps1 çağrılır.
      2. Backup başarısız ise migration ÇALIŞTIRILMAZ (veri kaybı riski).
      3. Backup başarılı ise "dotnet ef database update" çalıştırılır.

    Migration komutu şu proje yapısını varsayar:
      src/YonetimFinansalIslemTakipSistemi.Infrastructure
      src/YonetimFinansalIslemTakipSistemi.UI  (appsettings.json burada)

    Bağlantı bilgisi YONETIM_DB_CONNECTION env var veya appsettings.json'dan okunur.

.PARAMETER BackupDirectory
    Backup dosyalarının konumu. Varsayılan: Backups

.PARAMETER Database
    Veritabanı adı (Backup-Database.ps1'e geçirilir).

.PARAMETER Host
    Sunucu adresi (Backup-Database.ps1'e geçirilir).

.PARAMETER Port
    Port (Backup-Database.ps1'e geçirilir).

.PARAMETER Username
    Kullanıcı adı (Backup-Database.ps1'e geçirilir).

.EXAMPLE
    .\scripts\Backup-And-Migrate.ps1
    .\scripts\Backup-And-Migrate.ps1 -BackupDirectory "D:\DBBackups"
#>
[CmdletBinding()]
param(
    [string]$BackupDirectory = "Backups",
    [string]$Database        = "",
    [string]$Host            = "",
    [string]$Port            = "",
    [string]$Username        = ""
)

$ErrorActionPreference = "Stop"

$scriptDir    = if ($PSScriptRoot) { $PSScriptRoot } else { Get-Location }
$backupScript = Join-Path $scriptDir "Backup-Database.ps1"

Write-Host ""
Write-Host "========================================="
Write-Host " BACKUP + MİGRATION AKIŞI"
Write-Host "========================================="
Write-Host ""

# ── Adım 1: Backup ───────────────────────────────────────────────────────────

Write-Host "[1/2] Backup alınıyor..."

$backupArgs = @{
    BackupDirectory = $BackupDirectory
}
if ($Database) { $backupArgs["Database"] = $Database }
if ($Host)     { $backupArgs["Host"]     = $Host     }
if ($Port)     { $backupArgs["Port"]     = $Port     }
if ($Username) { $backupArgs["Username"] = $Username }

try {
    & $backupScript @backupArgs
    $backupExit = $LASTEXITCODE
}
catch {
    Write-Error "Backup scripti çalıştırılamadı: $_"
    Write-Error "Migration iptal edildi. Hiçbir değişiklik yapılmadı."
    exit 1
}

if ($backupExit -ne 0) {
    Write-Host ""
    Write-Error "Backup başarısız oldu (exit: $backupExit). Migration güvenlik nedeniyle iptal edildi."
    Write-Error "Lütfen backup hatasını giderin ve tekrar deneyin."
    exit 1
}

Write-Host ""
Write-Host "[OK] Backup başarılı. Migration başlatılıyor..."
Write-Host ""

# ── Adım 2: EF Migration ─────────────────────────────────────────────────────

Write-Host "[2/2] EF migration uygulanıyor..."
Write-Host ""

# Migration komutunun proje kökünden çalışması gerekiyor
$solutionRoot = Split-Path $scriptDir -Parent

$efArgs = @(
    "ef",
    "database",
    "update",
    "--project",
    "src/YonetimFinansalIslemTakipSistemi.Infrastructure",
    "--startup-project",
    "src/YonetimFinansalIslemTakipSistemi.Infrastructure"
)

try {
    Push-Location $solutionRoot
    & dotnet @efArgs
    $efExit = $LASTEXITCODE
}
catch {
    Pop-Location
    Write-Error "dotnet ef çalıştırılamadı: $_"
    Write-Error "dotnet-ef tool kurulu olmalıdır: dotnet tool install --global dotnet-ef"
    exit 1
}
finally {
    Pop-Location
}

Write-Host ""
if ($efExit -eq 0) {
    Write-Host "========================================="
    Write-Host "[BASARILI] Backup + Migration tamamlandi."
    Write-Host "  Backup : $BackupDirectory klasörüne kaydedildi"
    Write-Host "  Migration: uygulandı"
    Write-Host "========================================="
    exit 0
} else {
    Write-Error "[HATA] Migration başarısız (exit: $efExit)."
    Write-Error "Backup dosyası '$BackupDirectory' klasöründe mevcut — gerekirse restore edilebilir."
    exit 1
}
