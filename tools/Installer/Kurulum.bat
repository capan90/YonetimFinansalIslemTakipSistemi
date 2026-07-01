@echo off
setlocal
rem UTF-8 kod sayfasi: kurulum ekranindaki onay isaretleri duzgun gozuksun.
chcp 65001 >nul
title Yonetim Finansal Islem Takip Sistemi - Kurulum

rem UNC yoldan (\\sunucu\pay) calistirildiginda CMD "UNC paths are not supported"
rem uyarisi verir ve calisma klasorunu Windows'a dusurur. pushd, UNC yolu gecici
rem bir surucuye baglar; boylece %~dp0 dogru cozulur. Iste sonunda popd ile serbest.
pushd "%~dp0"

echo.
echo   Yonetim Finansal Islem Takip Sistemi kurulumu baslatiliyor...
echo.

rem PowerShell mevcut mu?
where powershell >nul 2>nul
if errorlevel 1 (
  echo   [HATA] PowerShell bu bilgisayarda bulunamadi. Kurulum yapilamiyor.
  echo   Lutfen BT ekibine basvurun.
  echo.
  popd
  pause
  exit /b 1
)

rem Kurulum motorunu bu bat ile ayni klasorden calistir.
rem ShareRoot ACIKCA verilir (pushd/%~dp0/%CD% uzerinden TURETILMEZ). Sondaki '\' konmaz;
rem aksi halde CMD icin "...\" ifadesi tirnagi kacirir (\" -> escape) ve yol bozulur.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Yonetim.ps1" -ShareRoot "\\10.0.0.169\YonetimPublish" -NoPause %*
set "RC=%ERRORLEVEL%"

echo.
if "%RC%"=="0" (
  rem Basari: motor zaten kapanis geri sayimini yapti. Beklemeden kapan.
  echo   Kurulum tamamlandi. Uygulama Baslat menusunden acilabilir.
  popd
  exit /b 0
)

rem Hata veya kullanici iptali (RC^!=0): kullanici mesaji okuyabilsin diye beklet.
echo   Kurulum tamamlanamadi veya iptal edildi.
echo   Yukaridaki mesaji ve log yolunu BT ekibine iletin.
echo.
popd
pause
exit /b %RC%
