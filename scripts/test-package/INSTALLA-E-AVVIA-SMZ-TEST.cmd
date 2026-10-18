@echo off
setlocal
title Installazione SMZ TEST - Mario Rossi 12345
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Installa-SMZ-Test.ps1"
if errorlevel 1 (
    echo.
    echo Installazione non completata. Leggere il messaggio qui sopra.
    pause
    exit /b 1
)
exit /b 0
