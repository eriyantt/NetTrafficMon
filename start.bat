@echo off
cd /d "%~dp0"
if not exist "NetTrafficMon.exe" (
    echo Compiling NetTrafficMon...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
)
start "" "%~dp0NetTrafficMon.exe"
