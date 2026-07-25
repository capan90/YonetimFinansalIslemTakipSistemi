<#
.SYNOPSIS
    ClickOnce guncellemelerinde KIRILMAYAN, sabit bir baslatici kisayolu olusturur.

.DESCRIPTION
    Sorun: ClickOnce uygulamasi her surumde farkli bir versiyonlu klasorden calisir
    (%LocalAppData%\Apps\2.0\<hash>\...). Kullanici CALISAN uygulamayi gorev cubuguna
    sabitlerse pin bu degisken yola baglanir; guncelleme sonrasi yol degisince pin kirilir
    ve uygulama ayri bir simge olarak acilir.

    Cozum iki parcalidir:
      1) Uygulama sabit bir AppUserModelID bildirir (kod tarafinda yapildi - App.OnStartup).
      2) Kullanici, CALISAN exe yerine SABIT yoldaki bu kisayolu sabitler. Kisayol,
         ClickOnce'in .appref-ms baslaticisina isaret eder (kimlik-tabanli; her zaman
         GUNCEL surumu acar) ve ayni AUMID'i tasir (calisan uygulama bu pin'i "yakar").

    Bu script kisayolu Masaustu'nde olusturur. Kullanici bir kez bu kisayolu gorev
    cubuguna sabitler; sonraki tum guncellemelerde pin calismaya devam eder.

.NOTES
    - Kullanici oturumunda (kurulumdan SONRA, en az bir kez uygulama acildiktan sonra)
      calistirilir; .appref-ms'in olusmus olmasi gerekir.
    - Yonetici gerektirmez (kullanici profiline yazar).
#>

param(
    # Uygulama ile AYNI olmali (App.xaml.cs -> AppUserModelId). Degistirilirse ikisi birlikte degismeli.
    [string]$AppUserModelId = "ErdemSoftTekstil.YonetimFinansalIslemTakip",
    # Kisayol adi (gorev cubugunda/Masaustunde gorunecek)
    [string]$ShortcutName   = "Yonetim Finansal",
    # ClickOnce Baslat Menusu kisayolu adi (Publish-ClickOnce.ps1 -> $AppName)
    [string]$ApprefName     = "Yonetim Finansal Islem Takip Sistemi"
)

$ErrorActionPreference = "Stop"

# 1) ClickOnce'in olusturdugu .appref-ms baslaticisini bul (kurulumda Baslat Menusu'ne dusurulur).
$startMenu   = [Environment]::GetFolderPath("Programs")
$apprefPath  = Join-Path $startMenu "YonetimApp\$ApprefName.appref-ms"

if (-not (Test-Path $apprefPath)) {
    throw "ClickOnce baslaticisi bulunamadi: $apprefPath`n" +
          "Once uygulamayi kurup en az bir kez calistirin (Baslat Menusu kisayolu o zaman olusur)."
}

# 2) Uygulama ikonunu bul (kurulu surumun exe'sinden). Bulunamazsa .appref-ms ikonu kullanilir.
$iconPath = $null
$appsRoot = Join-Path $env:LOCALAPPDATA "Apps\2.0"
if (Test-Path $appsRoot) {
    $exe = Get-ChildItem $appsRoot -Recurse -Filter "YonetimFinansalIslemTakipSistemi.UI.exe" -ErrorAction SilentlyContinue |
           Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($exe) { $iconPath = $exe.FullName }
}

# 3) Masaustunde .lnk olustur; hedef = .appref-ms (kimlik-tabanli, surumden bagimsiz).
$desktop      = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktop "$ShortcutName.lnk"

$shell    = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath  = $apprefPath
$shortcut.Description = "Yonetim Finansal Islem Takip Sistemi"
if ($iconPath) { $shortcut.IconLocation = "$iconPath,0" }
$shortcut.Save()

Write-Host "[OK] Kisayol olusturuldu: $shortcutPath" -ForegroundColor Green
Write-Host "     Hedef: $apprefPath" -ForegroundColor Gray

# 4) Kisayola sabit AppUserModelID damgala (calisan uygulamayla ayni kimlik -> pin gruplanir).
#    En iyi caba: basarisiz olursa kisayol yine de calisir, yalnizca gruplama garanti olmaz.
try {
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class ShortcutAumid
{
    // System.AppUserModel.ID -> {9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}, 5
    static readonly PropertyKey AumidKey = new PropertyKey(
        new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);

    public static void Set(string lnkPath, string aumid)
    {
        var link = (IPersistFile)new ShellLink();
        link.Load(lnkPath, 0); // STGM_READ
        var store = (IPropertyStore)link;

        var key = AumidKey; // ref icin yerel kopya (static readonly ref gecilemez)
        var pv = new PropVariant();
        pv.SetString(aumid);
        store.SetValue(ref key, ref pv);
        store.Commit();
        pv.Clear();

        link.Save(lnkPath, true);
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    class ShellLink { }

    [ComImport, Guid("0000010b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    [ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPropertyStore
    {
        void GetCount(out uint cProps);
        void GetAt(uint iProp, out PropertyKey pkey);
        void GetValue(ref PropertyKey key, out PropVariant pv);
        void SetValue(ref PropertyKey key, ref PropVariant pv);
        void Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PropertyKey
    {
        Guid fmtid; int pid;
        public PropertyKey(Guid f, int p) { fmtid = f; pid = p; }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct PropVariant
    {
        [FieldOffset(0)] ushort vt;
        [FieldOffset(8)] IntPtr p;

        public void SetString(string val)
        {
            vt = 31; // VT_LPWSTR
            p  = Marshal.StringToCoTaskMemUni(val);
        }
        public void Clear()
        {
            if (p != IntPtr.Zero) { Marshal.FreeCoTaskMem(p); p = IntPtr.Zero; }
            vt = 0;
        }
    }
}
"@ -ErrorAction Stop

    [ShortcutAumid]::Set($shortcutPath, $AppUserModelId)
    Write-Host "[OK] AppUserModelID damgalandi: $AppUserModelId" -ForegroundColor Green
}
catch {
    Write-Host "[UYARI] AppUserModelID damgalanamadi (kisayol yine calisir): $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Simdi bu Masaustu kisayoluna SAG TIK -> 'Gorev cubuguna sabitle' yapin." -ForegroundColor Cyan
Write-Host "Calisan uygulamayi DEGIL, bu kisayolu sabitleyin; boylece pin guncellemelerde kirilmaz." -ForegroundColor Cyan
