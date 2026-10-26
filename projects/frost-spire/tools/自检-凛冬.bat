@echo off
REM 【注意】本文件是 GBK 编码，别存成 UTF-8。
REM cmd 按系统 ANSI 读 .bat，UTF-8 会让里面的中文全变乱码（连 REM 注释都会）。
chcp 936 >nul
setlocal enabledelayedexpansion

REM ==============================================================
REM  凛冬尖塔 - 一键自检
REM
REM  怎么用：先启动游戏、进一局、玩到需要检查的那一步，然后保持游戏开着，
REM          双击本文件。它会读最新一份游戏日志，逐项告诉你过没过。
REM
REM  为什么需要它：mod 没加载 / 卡面英文 / 面板不显示 这类问题，
REM               单看画面分不清是"没加载"还是"加载了但代码没跑到"。
REM               日志能一刀切开。
REM ==============================================================

set "LOGDIR=%APPDATA%\SlayTheSpire2\logs"
set "MODDIR=D:\software\steam\steamapps\common\Slay the Spire 2\mods\FrostSpire"

echo.
echo ============================================================
echo   凛冬尖塔 - 自检
echo ============================================================
echo.

REM ---------- 0. 找最新日志 ----------
if not exist "%LOGDIR%" (
    echo [X] 找不到日志目录：
    echo     %LOGDIR%
    echo     游戏是不是从没在本机跑过？
    pause & exit /b 1
)

set "LATEST="
for /f "delims=" %%f in ('dir /b /o-d "%LOGDIR%\godot*.log" 2^>nul') do (
    if not defined LATEST set "LATEST=%LOGDIR%\%%f"
)
if not defined LATEST (
    echo [X] 日志目录里没有 godot*.log
    pause & exit /b 1
)

echo 最新日志：%LATEST%
for %%f in ("%LATEST%") do echo 写入时间：%%~tf
echo.

REM ---------- 1. mod 加载了吗 ----------
echo --- 1. mod 加载 ---
findstr /c:"Skipping loading mod FrostSpire" "%LATEST%" >nul 2>&1
if !errorlevel! equ 0 (
    echo [X] FrostSpire 被跳过了 - 游戏没加载它。两种可能：
    echo      a^) 游戏里的「确认加载 mod」弹窗没点过
    echo      b^) 设置里这个 mod 被禁用了
    echo     ^>^> 去游戏里确认一次，然后完全退出进程再启动（不是回主菜单）
) else (
    findstr /c:"FrostSpire initialized" "%LATEST%" >nul 2>&1
    if !errorlevel! equ 0 (
        echo [OK] FrostSpire 已加载（Initialize 跑到了）
    ) else (
        echo [?] 没看到 "Skipping"，也没看到 "initialized"
        echo     可能这次启动根本没走到 mod 加载阶段
    )
)
echo.

REM ---------- 2. 素材层 ----------
echo --- 2. 素材层（UI 素材候选链）---
findstr /c:"凛冬素材：用自己的" "%LATEST%" >nul 2>&1
if !errorlevel! equ 0 (
    echo [OK] 素材盘点跑通了：
    findstr /c:"凛冬素材：用自己的" "%LATEST%"
    findstr /c:"  没有素材" "%LATEST%" >nul 2>&1 || echo      ^(没有"走纯色兜底"的，说明每个槽位都有图可借^)
) else (
    echo [X] 没看到素材盘点日志 - 候选链没跑，或 DLL 还是旧的
    echo     旧 DLL 里没有 FrostAssets.ReportOnce^(^)，重新编译并完全重启游戏
)
echo.

REM ---------- 3. 地图入口点 ----------
echo --- 3. 裂隙入口点（原版地图上的那个点）---
findstr /c:"[裂隙点] 地图入口点注入器已就绪" "%LATEST%" >nul 2>&1
if !errorlevel! equ 0 (
    echo [OK] 注入器已就绪
) else (
    echo [X] 注入器没就绪 - FrostRiftMapNode.Install^(^) 没跑到
)
findstr /c:"注入凛冬裂隙入口" "%LATEST%" >nul 2>&1
if !errorlevel! equ 0 (
    echo [OK] 裂隙点已注入地图：
    findstr /c:"注入凛冬裂隙入口" "%LATEST%"
) else (
    findstr /c:"[裂隙点] 第" "%LATEST%" | findstr /c:"没有空位" >nul 2>&1
    if !errorlevel! equ 0 (
        echo [!] 那一行没有空位，这一幕没放裂隙入口：
        findstr /c:"没有空位" "%LATEST%"
    ) else (
        echo [?] 没看到"已注入" - 可能这一局还没生成地图（要开新一局）
    )
)
echo.

REM ---------- 4. 裂隙进出 ----------
echo --- 4. 裂隙进出 ---
findstr /c:"[裂隙]" "%LATEST%" | findstr /c:"单位卡已在入门后发放" >nul 2>&1
if !errorlevel! equ 0 (
    echo [OK] 单位卡在进门后发放（用户要求的时机）
) else (
    echo [?] 这一局还没进过裂隙（没进过就正常）
)
findstr /c:"[裂隙] 已用内存缓存还原原地图" "%LATEST%" >nul 2>&1
if !errorlevel! equ 0 echo [OK] 出裂隙走了内存缓存路径（正确路径）
findstr /c:"[裂隙] 内存缓存与 SavedMap 都没有" "%LATEST%" >nul 2>&1
if !errorlevel! equ 0 echo [!] 出裂隙走了重建兜底 - 内存缓存没命中，裂隙点可能丢，看笔记
echo.

REM ---------- 5. 中文文本 ----------
echo --- 5. 卡面中文 ---
findstr /c:"FrostLoc 自检通过" "%LATEST%" >nul 2>&1
if !errorlevel! equ 0 (
    echo [OK] FrostLoc 自检通过：
    findstr /c:"FrostLoc 自检通过" "%LATEST%"
) else (
    findstr /c:"FrostLoc 自检发现" "%LATEST%" >nul 2>&1
    if !errorlevel! equ 0 (
        echo [!] 自检发现对不上的条目 - 卡面会显示原始 key：
        findstr /c:"FrostLoc 自检发现" "%LATEST%"
    ) else (
        echo [X] 没有自检日志 - FrostLoc 没跑
    )
)
echo.

REM ---------- 6. 素材目录 ----------
echo --- 6. 素材目录 ---
if exist "%MODDIR%\assets" (
    echo [OK] mods 下已有 assets 目录
    set PNGCOUNT=0
    for /r "%MODDIR%\assets" %%f in (*.png) do set /a PNGCOUNT+=1
    echo      里面已有 !PNGCOUNT! 张 png
    if !PNGCOUNT! equ 0 echo      ^(还没放图 - 现在界面全走游戏自带兜底，正常^)
) else (
    echo [X] mods 下没有 assets 目录 - 编译时 CopyAssets 没生效
)
echo.

REM ---------- 7. pck（卡面） ----------
echo --- 7. pck（工事牌卡面）---
if exist "%MODDIR%\FrostSpire.pck" (
    echo [OK] FrostSpire.pck 在
    for %%f in ("%MODDIR%\FrostSpire.pck") do echo      大小 %%~zf 字节
    findstr /c:"\"has_pck\": true" "%MODDIR%\FrostSpire.json" >nul 2>&1
    if !errorlevel! equ 0 (
        echo [OK] manifest 里 has_pck=true
    ) else (
        echo [X] manifest 里 has_pck 不是 true - pck 不会被加载
    )
) else (
    echo [X] 没有 FrostSpire.pck - 卡面会走游戏默认渲染（不崩，但看不到自己的图）
    echo     跑 tools\build_pck.ps1 再 dotnet build
)
echo.

echo ============================================================
echo   怎么用这份结果
echo ============================================================
echo   - 第 1 项 [X]  =^> 去游戏确认 mod 弹窗，然后完全重启
echo   - 第 2 项 [X]  =^> DLL 是旧的，重新编译
echo   - 第 3 项 [?]  =^> 开新一局再看
echo   - 第 5 项 [!]  =^> 卡面 key 名对不上，看 notes 里的排查笔记
echo   - 第 7 项 [X]  =^> 卡面图看不到，跑 build_pck.ps1
echo.
echo   详细说明：notes\凛冬尖塔-M3.5UI素材层-已落地-wb.md
echo ============================================================
echo.
pause
