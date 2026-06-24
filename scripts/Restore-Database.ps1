<#
.SYNOPSIS
    PostgreSQL veritabanını bir .backup dosyasından geri yükler.

.DESCRIPTION
    pg_restore kullanarak belirtilen backup dosyasını hedef veritabanına uygular.

    !!! UYARI — RİSKLİ İŞLEM !!!
    - Restore önce hedef veritabanındaki mevcut verilerin silinmesini gerektirebilir.
    - Üretimde çalıştırmadan önce tüm kullanıcıları uygulamadan çıkarın.
    - Bu script otomatik DROP DATABASE yapmaz; gerekirse manuel yapılmalıdır.
    - Restore onayı için "RESTORE" yazmak zorunludur.

.PARAMETER BackupFile
    Geri yüklenecek .backup dosyasının tam yolu. (Zorunlu)

.PARAMETER Database
    Hedef veritabanı adı. Varsayılan: yonetim_db

.PARAMETER Host
    Sunucu adresi. Varsayılan: localhost

.PARAMETER Port
    Port. Varsayılan: 5432

.PARAMETER Username
    Veritabanı kullanıcısı. Varsayılan: postgres

.EXAMPLE
    .\scripts\Restore-Database.ps1 -BackupFile "Backups\yonetim_db_20260624_090000.backup"
    .\scripts\Restore-Database.ps1 -BackupFile "D:\backups\yonetim_db_20260624_090000.backup" -Host prod-server
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,

    [string]$Database = "yonetim_db",
    [string]$Host     = "localhost",
    [string]$Port     = "5432",
    [string]$Username = "postgres"
)

$ErrorActionPreference = "Stop"

# ── Uyarı ve doğrulama ────────────────────────────────────────────────────────

Write-Host ""
Write-Host "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
Write-Host "!!         DİKKAT — RİSKLİ İŞLEM      !!"
Write-Host "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
Write-Host ""
Write-Host "Bu işlem aşağıdaki veritabanını ETKİLEYECEKTİR:"
Write-Host "  Sunucu    : $Host`:$Port"
Write-Host "  Veritabanı: $Database"
Write-Host "  Kullanıcı : $Username"
Write-Host "  Backup    : $BackupFile"
Write-Host ""
Write-Host "Restore öncesi yapılması gerekenler:"
Write-Host "  1. Tüm kullanıcıların uygulamadan çıkmış olması"
Write-Host "  2. Hedef veritabanının hazır olması"
Write-Host "  3. Mevcut veriler üzerine yazılacağının bilinmesi"
Write-Host ""
Write-Host "Hedef veritabanını sıfırdan restore etmek için (manuel adımlar):"
Write-Host "  -- psql -h $Host -p $Port -U $Username -c 'DROP DATABASE IF EXISTS $Database;'"
Write-Host "  -- psql -h $Host -p $Port -U $Username -c 'CREATE DATABASE $Database;'"
Write-Host ""

# Backup dosyası kontrolü
if (-not (Test-Path $BackupFile)) {
    Write-Error "Backup dosyası bulunamadı: $BackupFile"
    exit 1
}

# Onay istemi
Write-Host "Devam etmek için 'RESTORE' yazın (büyük harf, tam olarak):"
$confirmation = Read-Host

if ($confirmation -ne "RESTORE") {
    Write-Host "İşlem iptal edildi. Onay verilmedi."
    exit 0
}

# ── Şifre çözümü ─────────────────────────────────────────────────────────────

$dbPass = ""
if ($env:PGPASSWORD) {
    $dbPass = $env:PGPASSWORD
    Write-Host "Şifre: PGPASSWORD ortam değişkeninden alındı."
} elseif ($env:YONETIM_DB_CONNECTION) {
    $connParts = @{}
    $env:YONETIM_DB_CONNECTION -split ";" | ForEach-Object {
        if ($_ -match "^([^=]+)=(.*)$") { $connParts[$Matches[1].ToLower()] = $Matches[2] }
    }
    if ($connParts.ContainsKey("password")) { $dbPass = $connParts["password"] }
}

if (-not $dbPass) {
    Write-Warning "Şifre bulunamadı. pg_restore parola isteyebilir."
    Write-Warning "İpucu: PGPASSWORD veya YONETIM_DB_CONNECTION ortam değişkenini ayarlayın."
}

# ── pg_restore ────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Restore başlatılıyor..."

$pgArgs = @(
    "--host=$Host",
    "--port=$Port",
    "--username=$Username",
    "--dbname=$Database",
    "--no-password",
    "--verbose",
    $BackupFile
)

$pgExit = 0
try {
    if ($dbPass) { $env:PGPASSWORD = $dbPass }

    & pg_restore @pgArgs
    $pgExit = $LASTEXITCODE
}
catch {
    Write-Error "pg_restore çalıştırılamadı: $_"
    Write-Error "pg_restore PATH'te mevcut olmalıdır. PostgreSQL kurulumunu ve PATH ayarını kontrol edin."
    exit 2
}
finally {
    if ($dbPass) { $env:PGPASSWORD = "" }
}

Write-Host ""
if ($pgExit -eq 0) {
    Write-Host "[BASARILI] Restore tamamlandi."
    Write-Host "  Veritabanı '$Database' geri yuklendi."
} else {
    Write-Error "[HATA] pg_restore basarisiz (exit: $pgExit). Yukaridaki mesajlari inceleyin."
    exit 1
}
