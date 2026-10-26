@echo off
chcp 65001 >nul
pushd "%~dp0"
title DouSpire - watch AI play
bin\Debug\net9.0\DouSpireRules.exe --demo
echo.
pause
popd
