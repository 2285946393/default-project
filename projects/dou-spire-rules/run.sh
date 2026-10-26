#!/usr/bin/env bash
# 跑斗地主规则自测。
#
# ⚠️ 为什么不能直接 `dotnet run`：
#   WorkBuddy 的 bash 沙箱启动子进程时缺少 Windows 系统环境变量
#   （SystemRoot / ProgramData 等），导致 .NET 的 Environment.GetFolderPath
#   返回空 → NuGet 拼不出机器级配置路径 → 报 "Value cannot be null. (Parameter 'path1')"。
#   本脚本补齐这些变量，并用 -p:RestoreProjectStyle= 让 MSBuild 跳过 NuGet 设置加载。
#
#   在普通 CMD / PowerShell 里 `dotnet run` 是正常的，不需要这个脚本。
#   （该项目零第三方依赖，所以绕过 NuGet 是安全的。）
#
# 用法：bash run.sh

set -e
cd "$(dirname "$0")"

export SystemRoot='C:\Windows'
export windir='C:\Windows'
export ComSpec='C:\Windows\system32\cmd.exe'
export ProgramData='C:\ProgramData'
export ALLUSERSPROFILE='C:\ProgramData'
export APPDATA='C:\Users\有花无实\AppData\Roaming'
export LOCALAPPDATA='C:\Users\有花无实\AppData\Local'
export TEMP='C:\Users\有花无实\AppData\Local\Temp'
export TMP="$TEMP"
export ProgramFiles='C:\Program Files'
export CommonProgramFiles='C:\Program Files\Common Files'

if [ "$#" -gt 0 ]; then
  # 带参数时插一个 -- 把参数透传给程序本身（dotnet run 的约定）
  exec dotnet run --project DouSpireRules.csproj -c Debug --nologo \
    -p:MSBuildEnableWorkloadResolver=false \
    -p:RestoreProjectStyle= -- "$@"
else
  exec dotnet run --project DouSpireRules.csproj -c Debug --nologo \
    -p:MSBuildEnableWorkloadResolver=false \
    -p:RestoreProjectStyle=
fi
