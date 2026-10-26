# -*- coding: utf-8 -*-
"""在桌面创建「Codex 人格切换」快捷方式（ps1 必须带 UTF-8 BOM，否则中文乱码）。"""
import os
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8")

BASE = r"D:\work\AI产出\Codex人格切换"
PS1 = os.path.join(BASE, "建快捷方式.ps1")

PS = r'''$ErrorActionPreference = 'Stop'
$desktop = [Environment]::GetFolderPath('Desktop')
$lnk = Join-Path $desktop 'Codex 人格切换.lnk'
$W = New-Object -ComObject WScript.Shell
$s = $W.CreateShortcut($lnk)
$s.TargetPath = 'D:\software\Python\pythonw.exe'
$s.Arguments = '"D:\work\AI产出\Codex人格切换\切换模式.pyw"'
$s.WorkingDirectory = 'D:\work\AI产出\Codex人格切换'
$s.IconLocation = 'D:\work\AI产出\Codex人格切换\图标.ico'
$s.Description = 'Codex 人格切换'
$s.WindowStyle = 1
$s.Save()
if (Test-Path $lnk) { Write-Output "OK $lnk" } else { Write-Output "FAIL" }
'''

with open(PS1, "w", encoding="utf-8-sig", newline="\r\n") as f:
    f.write(PS)

pwsh = r"D:\software\pwsh\pwsh.exe"
if not os.path.isfile(pwsh):
    pwsh = "powershell.exe"

r = subprocess.run(
    [pwsh, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", PS1],
    capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=90,
)
print("exit:", r.returncode)
print("out:", (r.stdout or "").strip())
if r.stderr:
    print("err:", r.stderr.strip()[:500])

# 回读快捷方式确认它真的能解析出目标
chk = subprocess.run(
    [pwsh, "-NoProfile", "-Command",
     "$W=New-Object -ComObject WScript.Shell;"
     "$s=$W.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Desktop')) 'Codex 人格切换.lnk'));"
     "'target=' + $s.TargetPath; 'args=' + $s.Arguments"],
    capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=60,
)
print(chk.stdout.strip())
