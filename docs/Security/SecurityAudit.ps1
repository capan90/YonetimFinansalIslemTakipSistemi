<#
.SYNOPSIS
    Yonetim Finansal Islem Takip Sistemi -- Production Security Audit

.DESCRIPTION
    Uretim sunucusunda calistirilir. Sistemi DEGISTIRMEZ; yalnizca denetler ve
    her kontrol icin PASS / WARNING / FAIL sonucu uretir.

    Kontroller:
      1.  PostgreSQL Service
      2.  PostgreSQL Port
      3.  Firewall Rule
      4.  Publish Share
      5.  Kurulum Share
      6.  NTFS Permissions
      7.  Backup Folder
      8.  Logs Folder
      9.  Scheduled Tasks
      10. Certificate
      11. Environment Variables

    Cikis kodu: FAIL varsa 1, yoksa 0. (WARNING cikis kodunu etkilemez.)

.PARAMETER PublishPath
    Yerel publish klasoru (SMB paylasimin isaret ettigi klasor).

.PARAMETER ShareName
    Publish/kurulum SMB paylasim adi.

.PARAMETER InstallUnc
    Istemcilerin kurulum icin kullandigi UNC yolu.

.PARAMETER Port
    PostgreSQL portu.

.PARAMETER BackupPath
    Yedeklerin bulundugu klasor.

.PARAMETER LogsPath
    Uygulama loglarinin bulundugu klasor.

.PARAMETER CertThumbprint
    ClickOnce imzalama sertifikasi parmak izi.

.EXAMPLE
    .\docs\Security\SecurityAudit.ps1
    .\docs\Security\SecurityAudit.ps1 -PublishPath "C:\Apps\Yonetim\Publish" -Port 5432
#>
[CmdletBinding()]
param(
    [string]$PublishPath    = "C:\Apps\Yonetim\Publish",
    [string]$ShareName      = "YonetimPublish",
    [string]$InstallUnc     = "\\10.0.0.169\YonetimPublish",
    [int]   $Port           = 5432,
    [string]$BackupPath     = "C:\Apps\Yonetim\Backups",
    [string]$LogsPath       = "C:\Apps\Yonetim\Logs",
    [string]$CertThumbprint = "0136460438B6DED7F20498C00F7D3AB4C1E1B203"
)

# Denetim scripti sistemi degistirmemeli; hatalar kontrol basina yakalanir.
$ErrorActionPreference = "Continue"

# ── Sonuc toplama ─────────────────────────────────────────────────────────────
$script:Results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param(
        [Parameter(Mandatory)][string]$Check,
        [Parameter(Mandatory)][ValidateSet("PASS", "WARNING", "FAIL")][string]$Status,
        [string]$Detail = ""
    )
    $script:Results.Add([PSCustomObject]@{
        Check  = $Check
        Status = $Status
        Detail = $Detail
    })

    $color = switch ($Status) {
        "PASS"    { "Green" }
        "WARNING" { "Yellow" }
        "FAIL"    { "Red" }
    }
    Write-Host ("  [{0,-7}] {1,-22} {2}" -f $Status, $Check, $Detail) -ForegroundColor $color
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " YONETIM -- PRODUCTION SECURITY AUDIT"              -ForegroundColor Cyan
Write-Host " Makine : $env:COMPUTERNAME"                        -ForegroundColor Cyan
Write-Host " Tarih  : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# ── 1. PostgreSQL Service ─────────────────────────────────────────────────────
try {
    $pgSvc = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue
    if (-not $pgSvc) {
        Add-Result "PostgreSQL Service" "FAIL" "PostgreSQL servisi bulunamadi (postgresql*)."
    }
    elseif ($pgSvc | Where-Object { $_.Status -eq "Running" }) {
        $names = ($pgSvc | Where-Object { $_.Status -eq "Running" } | ForEach-Object { $_.Name }) -join ", "
        Add-Result "PostgreSQL Service" "PASS" "Calisiyor: $names"
    }
    else {
        Add-Result "PostgreSQL Service" "FAIL" "Servis mevcut ama calismiyor: $($pgSvc.Name -join ', ')"
    }
}
catch { Add-Result "PostgreSQL Service" "FAIL" "Kontrol hatasi: $($_.Exception.Message)" }

# ── 2. PostgreSQL Port ────────────────────────────────────────────────────────
try {
    $listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($listening) {
        # 0.0.0.0 / :: dinleniyorsa dis dunyaya aciktir -> firewall kontrolu onemli
        $addrs = ($listening | Select-Object -ExpandProperty LocalAddress -Unique) -join ", "
        $wideOpen = $listening | Where-Object { $_.LocalAddress -in @("0.0.0.0", "::") }
        if ($wideOpen) {
            Add-Result "PostgreSQL Port" "WARNING" "$Port tum arayuzlerde dinleniyor ($addrs). Firewall ile kisitlayin."
        } else {
            Add-Result "PostgreSQL Port" "PASS" "$Port dinleniyor ($addrs)."
        }
    } else {
        Add-Result "PostgreSQL Port" "FAIL" "$Port dinlenmiyor. PostgreSQL erisilemez olabilir."
    }
}
catch { Add-Result "PostgreSQL Port" "WARNING" "Port durumu okunamadi: $($_.Exception.Message)" }

# ── 3. Firewall Rule ──────────────────────────────────────────────────────────
try {
    $portFilters = Get-NetFirewallPortFilter -ErrorAction SilentlyContinue |
        Where-Object { $_.Protocol -eq "TCP" -and ($_.LocalPort -eq "$Port" -or $_.LocalPort -contains "$Port") }

    if (-not $portFilters) {
        Add-Result "Firewall Rule" "WARNING" "$Port icin acik firewall kurali bulunamadi (varsayilan politikaya bagli)."
    }
    else {
        $rules = $portFilters | ForEach-Object { $_ | Get-NetFirewallRule -ErrorAction SilentlyContinue } |
            Where-Object { $_.Enabled -eq "True" -and $_.Direction -eq "Inbound" -and $_.Action -eq "Allow" }

        if (-not $rules) {
            Add-Result "Firewall Rule" "PASS" "$Port icin etkin izin verici gelen kural yok."
        }
        else {
            $anyRemote = $false
            foreach ($r in $rules) {
                $scope = $r | Get-NetFirewallAddressFilter -ErrorAction SilentlyContinue
                if ($scope.RemoteAddress -contains "Any") { $anyRemote = $true }
            }
            if ($anyRemote) {
                Add-Result "Firewall Rule" "WARNING" "$Port her IP'ye acik (RemoteAddress=Any). Yalnizca guvenli subnet'e kisitlayin."
            } else {
                Add-Result "Firewall Rule" "PASS" "$Port icin kural mevcut ve uzak adres kisitli."
            }
        }
    }
}
catch { Add-Result "Firewall Rule" "WARNING" "Firewall kurallari okunamadi: $($_.Exception.Message)" }

# ── 4. Publish Share ──────────────────────────────────────────────────────────
try {
    $share = Get-SmbShare -Name $ShareName -ErrorAction SilentlyContinue
    if (-not $share) {
        Add-Result "Publish Share" "FAIL" "'$ShareName' SMB paylasimi bulunamadi."
    }
    else {
        Add-Result "Publish Share" "PASS" "'$ShareName' -> $($share.Path)"
        # Everyone FullAccess kontrolu (Kurulum Share icin salt okunur onerilir)
        $access = Get-SmbShareAccess -Name $ShareName -ErrorAction SilentlyContinue
        $everyoneFull = $access | Where-Object {
            $_.AccountName -match "Everyone|Herkes" -and $_.AccessRight -eq "Full"
        }
        if ($everyoneFull) {
            Add-Result "Publish Share (ACL)" "WARNING" "Everyone FULL erisime sahip. Istemciler icin Read yeterli; yaziciyi kisitlayin."
        } else {
            Add-Result "Publish Share (ACL)" "PASS" "Everyone FULL erisimi yok."
        }
    }
}
catch { Add-Result "Publish Share" "WARNING" "Paylasim okunamadi: $($_.Exception.Message)" }

# ── 5. Kurulum Share (UNC erisilebilirlik) ────────────────────────────────────
try {
    if (Test-Path $InstallUnc) {
        $manifest = Join-Path $InstallUnc "YonetimFinansalIslemTakipSistemi.UI.application"
        if (Test-Path $manifest) {
            Add-Result "Kurulum Share" "PASS" "$InstallUnc erisilebilir; deployment manifest mevcut."
        } else {
            Add-Result "Kurulum Share" "WARNING" "$InstallUnc erisilebilir ama .application manifest yok. Henuz publish alinmamis olabilir."
        }
    } else {
        Add-Result "Kurulum Share" "FAIL" "$InstallUnc erisilemiyor. Istemciler guncelleme alamaz."
    }
}
catch { Add-Result "Kurulum Share" "WARNING" "UNC erisimi denetlenemedi: $($_.Exception.Message)" }

# ── 6. NTFS Permissions ───────────────────────────────────────────────────────
try {
    if (-not (Test-Path $PublishPath)) {
        Add-Result "NTFS Permissions" "WARNING" "Publish klasoru yok: $PublishPath"
    }
    else {
        $acl = Get-Acl $PublishPath
        $risky = $acl.Access | Where-Object {
            ($_.IdentityReference -match "Everyone|Herkes") -and
            ($_.AccessControlType -eq "Allow") -and
            ($_.FileSystemRights.ToString() -match "FullControl|Modify|Write")
        }
        if ($risky) {
            Add-Result "NTFS Permissions" "WARNING" "$PublishPath uzerinde Everyone Write/Modify/Full var. Yazma yetkisini yayin hesabiyla sinirlayin."
        } else {
            Add-Result "NTFS Permissions" "PASS" "$PublishPath uzerinde Everyone yazma yetkisi yok."
        }
    }
}
catch { Add-Result "NTFS Permissions" "WARNING" "ACL okunamadi: $($_.Exception.Message)" }

# ── 7. Backup Folder ──────────────────────────────────────────────────────────
try {
    if (-not (Test-Path $BackupPath)) {
        Add-Result "Backup Folder" "FAIL" "Backup klasoru yok: $BackupPath"
    }
    else {
        $backups = Get-ChildItem $BackupPath -Filter *.backup -ErrorAction SilentlyContinue
        if (-not $backups) {
            Add-Result "Backup Folder" "WARNING" "$BackupPath mevcut ama hic .backup dosyasi yok."
        } else {
            $newest = ($backups | Sort-Object LastWriteTime -Descending | Select-Object -First 1).LastWriteTime
            Add-Result "Backup Folder" "PASS" "$($backups.Count) yedek. En yeni: $($newest.ToString('yyyy-MM-dd HH:mm'))"
        }
        # Backup klasorunun herkese acik olmamasi gerekir
        $bacl = (Get-Acl $BackupPath).Access | Where-Object {
            ($_.IdentityReference -match "Everyone|Herkes") -and ($_.AccessControlType -eq "Allow")
        }
        if ($bacl) {
            Add-Result "Backup Folder (ACL)" "WARNING" "Backup klasoru Everyone'a acik. Yedekler hassas veri icerir; erisimi kisitlayin."
        }
    }
}
catch { Add-Result "Backup Folder" "WARNING" "Kontrol hatasi: $($_.Exception.Message)" }

# ── 8. Logs Folder ────────────────────────────────────────────────────────────
try {
    if (-not (Test-Path $LogsPath)) {
        Add-Result "Logs Folder" "WARNING" "Log klasoru yok: $LogsPath (uygulama ilk calismada olusturabilir)."
    }
    else {
        $lacl = (Get-Acl $LogsPath).Access | Where-Object {
            ($_.IdentityReference -match "Everyone|Herkes") -and
            ($_.AccessControlType -eq "Allow") -and
            ($_.FileSystemRights.ToString() -match "FullControl|Modify|Write")
        }
        if ($lacl) {
            Add-Result "Logs Folder" "WARNING" "$LogsPath Everyone'a yazilabilir. Log tahrifatini onlemek icin kisitlayin."
        } else {
            Add-Result "Logs Folder" "PASS" "$LogsPath mevcut ve asiri acik degil."
        }
    }
}
catch { Add-Result "Logs Folder" "WARNING" "Kontrol hatasi: $($_.Exception.Message)" }

# ── 9. Scheduled Tasks ────────────────────────────────────────────────────────
try {
    $tasks = Get-ScheduledTask -ErrorAction SilentlyContinue |
        Where-Object { $_.TaskName -match "Yonetim|Backup|Yedek" }
    if (-not $tasks) {
        Add-Result "Scheduled Tasks" "WARNING" "Yonetim/Backup ile eslesen zamanlanmis gorev yok. Otomatik yedek onerilir."
    }
    else {
        $enabled = $tasks | Where-Object { $_.State -ne "Disabled" }
        if ($enabled) {
            Add-Result "Scheduled Tasks" "PASS" "Etkin gorev(ler): $(( $enabled | ForEach-Object { $_.TaskName }) -join ', ')"
        } else {
            Add-Result "Scheduled Tasks" "WARNING" "Ilgili gorev(ler) var ama DEVRE DISI: $(( $tasks | ForEach-Object { $_.TaskName }) -join ', ')"
        }
    }
}
catch { Add-Result "Scheduled Tasks" "WARNING" "Gorevler okunamadi: $($_.Exception.Message)" }

# ── 10. Certificate ───────────────────────────────────────────────────────────
try {
    $cert = Get-ChildItem Cert:\CurrentUser\My -ErrorAction SilentlyContinue |
        Where-Object { $_.Thumbprint -eq $CertThumbprint }
    if (-not $cert) {
        Add-Result "Certificate" "FAIL" "Imzalama sertifikasi bulunamadi: $CertThumbprint"
    }
    else {
        $daysLeft = [int]([math]::Floor(($cert.NotAfter - (Get-Date)).TotalDays))
        if ($daysLeft -lt 0) {
            Add-Result "Certificate" "FAIL" "Sertifika SURESI DOLMUS ($($cert.NotAfter.ToString('yyyy-MM-dd')))."
        } elseif ($daysLeft -lt 30) {
            Add-Result "Certificate" "WARNING" "Sertifika $daysLeft gun icinde doluyor ($($cert.NotAfter.ToString('yyyy-MM-dd')))."
        } else {
            Add-Result "Certificate" "PASS" "Gecerli, $daysLeft gun kaldi ($($cert.NotAfter.ToString('yyyy-MM-dd')))."
        }
    }
}
catch { Add-Result "Certificate" "WARNING" "Sertifika okunamadi: $($_.Exception.Message)" }

# ── 11. Environment Variables ─────────────────────────────────────────────────
# Secret DEGERLER yazdirilmaz; yalnizca varlik/ortam dogrulanir.
function Get-MachineEnv([string]$name) {
    return [Environment]::GetEnvironmentVariable($name, "Machine")
}
try {
    $envName = Get-MachineEnv "YONETIM_ENVIRONMENT"
    if ($envName -eq "Production") {
        Add-Result "Env: YONETIM_ENVIRONMENT" "PASS" "Production"
    } elseif ([string]::IsNullOrWhiteSpace($envName)) {
        Add-Result "Env: YONETIM_ENVIRONMENT" "WARNING" "Ayarlanmamis (Machine). Uretim sunucusunda Production olmali."
    } else {
        Add-Result "Env: YONETIM_ENVIRONMENT" "WARNING" "Deger '$envName' (Production degil)."
    }

    $dbConn = Get-MachineEnv "YONETIM_DB_CONNECTION"
    if ([string]::IsNullOrWhiteSpace($dbConn)) {
        Add-Result "Env: YONETIM_DB_CONNECTION" "WARNING" "Ayarlanmamis. (Opsiyonel; appsettings.Production.json kullaniliyorsa gerekmez.)"
    } else {
        Add-Result "Env: YONETIM_DB_CONNECTION" "PASS" "Ayarli (deger gizlendi)."
    }

    $smtpPass = Get-MachineEnv "YONETIM_SMTP_PASSWORD"
    if ([string]::IsNullOrWhiteSpace($smtpPass)) {
        Add-Result "Env: YONETIM_SMTP_PASSWORD" "WARNING" "Ayarlanmamis. Hata e-posta bildirimleri gonderilemez."
    } else {
        Add-Result "Env: YONETIM_SMTP_PASSWORD" "PASS" "Ayarli (deger gizlendi)."
    }
}
catch { Add-Result "Environment Variables" "WARNING" "Env var okunamadi: $($_.Exception.Message)" }

# ── Ozet ──────────────────────────────────────────────────────────────────────
# @() ile sar: PS 5.1'de tek eslesmede .Count scalar donup sayimi bozar.
$pass = @($script:Results | Where-Object { $_.Status -eq "PASS" }).Count
$warn = @($script:Results | Where-Object { $_.Status -eq "WARNING" }).Count
$fail = @($script:Results | Where-Object { $_.Status -eq "FAIL" }).Count

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host " OZET" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ("  PASS    : {0}" -f $pass) -ForegroundColor Green
Write-Host ("  WARNING : {0}" -f $warn) -ForegroundColor Yellow
Write-Host ("  FAIL    : {0}" -f $fail) -ForegroundColor Red
Write-Host ""

if ($fail -gt 0) {
    Write-Host "SONUC: FAIL -- Kritik bulgular var, giderin." -ForegroundColor Red
    exit 1
} elseif ($warn -gt 0) {
    Write-Host "SONUC: WARNING -- Iyilestirme onerileri var." -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "SONUC: PASS -- Tum kontroller basarili." -ForegroundColor Green
    exit 0
}
