@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo ========================================
echo   Pixiv 半自动上传器
echo ========================================
echo.
python gui.py
if errorlevel 1 (
    echo.
    echo 出错了。按任意键退出。
    pause >nul
)
