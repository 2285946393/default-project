@echo off
chcp 65001 >nul
rem Snapshot the DSH settings document and the credential vault.
rem Keeps the newest 30 snapshots. Safe to run any time.
setlocal
set HOME_DIR=D:\software\dsh-home
set DEST=%HOME_DIR%\settings-backups
if not exist "%DEST%" mkdir "%DEST%"
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set STAMP=%%i
if exist "%HOME_DIR%\settings.yaml" copy /y "%HOME_DIR%\settings.yaml" "%DEST%\settings.yaml.%STAMP%" >nul
if exist "%HOME_DIR%\.credentials.yaml" copy /y "%HOME_DIR%\.credentials.yaml" "%DEST%\.credentials.yaml.%STAMP%" >nul
rem prune: keep the 30 newest of each
powershell -NoProfile -Command "Get-ChildItem '%DEST%\settings.yaml.*' | Sort-Object LastWriteTime -Descending | Select-Object -Skip 30 | Remove-Item -Force -ErrorAction SilentlyContinue"
powershell -NoProfile -Command "Get-ChildItem '%DEST%\.credentials.yaml.*' | Sort-Object LastWriteTime -Descending | Select-Object -Skip 30 | Remove-Item -Force -ErrorAction SilentlyContinue"
exit /b 0
