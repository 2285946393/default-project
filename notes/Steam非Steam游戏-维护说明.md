# Steam「非 Steam 游戏」列表维护说明

2026-10-04 建立。桌面图标想批量塞进 Steam 启动器时用这个，别再手点。

## 工具

`tools/steam_shortcuts.py`

```powershell
python tools\steam_shortcuts.py --list            # 看现在有哪些
python tools\steam_shortcuts.py --check           # 体检：能否原样重建（identical 必须是 True）
python tools\steam_shortcuts.py --sync-desktop    # 按桌面图标清单批量添加/改名
```

## 相关文件

| 文件 | 作用 |
|---|---|
| `tools/_desktop_lnks.json` | 桌面 + 公共桌面所有 `.lnk` 的目标路径、参数、起始目录（PowerShell 导出） |
| `tools/_steam_names.json` | 每个图标在 Steam 里显示的名字。`null` = 不加；`@名字` = 已存在就改名 |
| `D:\software\steam\userdata\1572415305\config\shortcuts.vdf` | Steam 真实数据（账号「有花无实」）。每次写入前自动备份成 `.bak-时间戳` |

重新导出桌面清单（桌面加了新图标后）：

```powershell
$sh = New-Object -ComObject WScript.Shell; $out = @(); foreach ($d in @("C:\Users\有花无实\Desktop","C:\Users\Public\Desktop")) { Get-ChildItem $d -Filter *.lnk | ForEach-Object { $s = $sh.CreateShortcut($_.FullName); $out += [pscustomobject]@{ lnk=$_.BaseName; target=$s.TargetPath; args=$s.Arguments; workdir=$s.WorkingDirectory; icon=$s.IconLocation } } }; $out | ConvertTo-Json -Depth 3 | Set-Content -Encoding UTF8 "C:\Users\有花无实\Documents\Default Project\tools\_desktop_lnks.json"
```

## 两条铁律

1. **改之前必须完全退出 Steam**，否则 Steam 退出时会把文件覆盖回去。
   优雅关闭：`& 'D:\software\steam\Steam.exe' -shutdown`，然后等 `Get-Process steam` 消失。
2. **写完把 Steam 开回来**：`Start-Process 'D:\software\steam\Steam.exe'`。

## 已知不加的

- `ChatGPT Codex`：UWP 应用只能用 `explorer.exe shell:AppsFolder\...` 启动，Steam 会一直显示「正在运行」。
- `Steam` 自己、`机械革命控制中心`（快捷方式已失效，取不到路径）。
