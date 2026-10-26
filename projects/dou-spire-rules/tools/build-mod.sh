#!/usr/bin/env bash
# 在 WorkBuddy 的 bash 沙箱里编译「带 NuGet 依赖」的尖塔 mod 工程。
#
# 为什么需要这个脚本：沙箱启动子进程时不带 Windows 系统变量，
# .NET 的 Environment.GetFolderPath(CommonApplicationData) 返回空 →
# NuGet 拼不出 %ProgramData%\NuGet → 报 "Value cannot be null. (Parameter 'path1')"。
# 补齐变量 + 指定 NuGet 全局包目录后就能正常还原/编译。
#
# 用法：
#   bash tools/build-mod.sh ../frost-spire
#   bash tools/build-mod.sh ../dou-spire --no-incremental
#
# ★★ 第一个参数是「工程目录」（相对当前 cwd），后面的参数原样透传给 dotnet build。
# ★★ dou-spire 专用：ModName 默认是 DouSpireDev（见 DouSpire.csproj），
#    也就是【不传参数编出来的是本地开发版】。
#    → 本地调试：bash tools/build-mod.sh ../dou-spire
#    → 要上工坊：bash tools/build-mod.sh ../dou-spire -p:ModName=DouSpire
#    忘了加 -p:ModName=DouSpire，编出来的 dll 会挂 DouSpireDev 身份，
#    拿去上架会把工坊条目改成开发版（prepare_workshop.py 有身份闸会拦，但别赌）。

set -uo pipefail

PROJ_DIR="${1:-}"; shift || true
[ -n "$PROJ_DIR" ] || { echo "用法：bash tools/build-mod.sh <工程目录> [参数...]"; exit 1; }
[ -d "$PROJ_DIR" ] || { echo "找不到工程目录：$PROJ_DIR"; exit 1; }

# —— 用户目录（沙箱里 $USERPROFILE 可能为空，从 $HOME 转 Windows 形式）——
if command -v cygpath >/dev/null 2>&1; then
  USER_HOME="$(cygpath -w "$HOME")"
else
  USER_HOME="C:\\Users\\$(basename "$HOME")"
fi

export SystemRoot='C:\Windows'
export windir='C:\Windows'
export ComSpec='C:\Windows\system32\cmd.exe'
export ProgramData='C:\ProgramData'
export ALLUSERSPROFILE='C:\ProgramData'
export APPDATA="$USER_HOME\\AppData\\Roaming"
export LOCALAPPDATA="$USER_HOME\\AppData\\Local"
export TEMP="$USER_HOME\\AppData\\Local\\Temp"
export TMP="$TEMP"
export ProgramFiles='C:\Program Files'
export CommonProgramFiles='C:\Program Files\\Common Files'
export USERPROFILE="$USER_HOME"
export HOMEDRIVE='C:'
export HOMEPATH="$USER_HOME"
export NUGET_PACKAGES="$USER_HOME\\.nuget\\packages"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

DOTNET='C:\Program Files\dotnet\dotnet.exe'

cd "$PROJ_DIR" || exit 1
CSPROJ=$(ls *.csproj 2>/dev/null | head -1)
[ -n "$CSPROJ" ] || { echo "目录里没有 .csproj"; exit 1; }

echo "── 编译 $CSPROJ"
echo "   目录：$(pwd)"
echo "   用户：$USER_HOME"
echo
"$DOTNET" build "$CSPROJ" -c Debug --nologo --no-incremental "$@"
BUILD_RC=$?

# ── 音乐文件夹里有没有歌 ───────────────────────────────────────────
# 2026-10-10 用户确认：mod 里那 4 首是**免费商用素材站**下的 ⇒ 可以随包上架，
# 不再需要"上架前撤走"。这里只在**编译时报个数**，提醒你别在不经意间多塞了歌。
# ⚠️ 你要是**自己换歌**（换成别的来源），那就得自己确认那首的授权 —— 这条提醒就是为此留的。
if [ -d assets/music ]; then
  N=$(find assets/music -type f \
        \( -name '*.ogg' -o -name '*.mp3' -o -name '*.wav' \
           -o -name '*.flac' -o -name '*.m4a' -o -name '*.aac' \) \
        2>/dev/null | wc -l | tr -d ' ')
  if [ "$N" != "0" ]; then
    echo
    echo "ℹ️ assets/music 里有 $N 个音频文件（免费商用素材，可随包上架）。"
    echo "   换歌请自行确认授权：python tools/music-switch.py status"
  fi
fi

exit $BUILD_RC
