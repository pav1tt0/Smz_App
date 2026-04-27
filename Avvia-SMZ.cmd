@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0Avvia-SMZ.ps1"
exit /b %errorlevel%
