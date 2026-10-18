@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%~dp0Avvia-SMZ-Test.ps1"
exit /b %errorlevel%
