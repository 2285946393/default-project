@echo off
chcp 65001 >nul
pushd "%~dp0"
title DouSpire - play with AI
bin\Debug\net9.0\DouSpireRules.exe --play
echo.
pause
popd
