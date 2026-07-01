@echo off
setlocal
rem UTF-8 kod sayfasi: kurulum ekranindaki onay isaretleri duzgun gozuksun.
chcp 65001 >nul
title Yonetim Finansal Islem Takip Sistemi - Kurulum

echo.
echo   Yonetim Finansal Islem Takip Sistemi kurulumu baslatiliyor...
echo.

rem PowerShell mevcut mu?
where powershell >nul 2>nul
if errorlevel 1 (
  echo   [HATA] PowerShell bu bilgisayarda bulunamadi. Kurulum yapilamiyor.
  echo   Lutfen BT ekibine basvurun.
  echo.
  pause
  exit /b 1
)

rem Kurulum motorunu bu bat ile ayni klasorden calistir. Ekstra parametreler aktarilir.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Yonetim.ps1" -NoPause %*
set "RC=%ERRORLEVEL%"

echo.
if not "%RC%"=="0" (
  echo   Kurulum tamamlanamadi. Yukaridaki mesaji ve log yolunu BT ekibine iletin.
)
echo.
pause
exit /b %RC%
