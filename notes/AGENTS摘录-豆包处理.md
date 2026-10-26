# 三点七、豆包（Doubao）：抢图片关联 + 桌面悬浮球（2026-10-10 处理完）

> **本文件是从全局声明 `D:\software\dsh-home\AGENTS.md` 搬出来的章节**（2026-10-17 瘦身）。
> 内容是原文一字未改，只是挪了地方。

---

## 三点七、豆包（Doubao）：抢图片关联 + 桌面悬浮球（2026-10-10 处理完）

用户 2026-10-10 原话：「**你给我把点击图片的打开方式选择为最好的那个，删掉豆包的图片打开方式，
永远禁止豆包的悬浮框**」

### 豆包是什么 / 装在哪

- 程序目录 `D:\software\豆包\Doubao\`（外层启动器 `Doubao.exe`，主体在 `app\Doubao.exe`），  
  卸载器 `uninstall.exe`。**Chromium 内核**，用户数据在 `%LOCALAPPDATA%\Doubao\User Data`。
- 它注册了两套关联：`Doubao.Image`（**图片查看器**，命令 `Doubao.exe --image-viewer-file="%1"`，  
  就是它抢的图片关联）和 `DoubaoHTM.*` / `Doubao.3UOW3FZTAPA7V7I2UQI54FDHUU`（浏览器）。
- **它会在启动后重新注册**（2026-10-10 实测踩过一次：清干净 → 重启豆包 → 头几十秒查是干净的，  
  过几分钟 `Doubao.Image` 又回来了）。**但默认打开方式不会被抢走**，只是右键"打开方式"列表里  
  会再冒出豆包。→ 所以清理得**常态化**，交给守护脚本周期做（见下），**不要指望清一次就完事**。

### 做成了什么

1. **15 种图片格式的默认程序 = Windows 照片**：  
   `.png .jpg .jpeg .jpe .jfif .gif .bmp .dib .webp .ico .avif .heic .heif .tif .tiff`
   - 用的 ProgId 是 **`AppX4mntx4h978m1v9gtzv0ewksfd6pmwsre`**。  
     > ⚠️ 别用错：`AppX43hnxtbyyps62jhe9sqpdzxn1790zetc` 是**旧版照片包**（2026.11080），  
     > 当前装的是 `2026.11090.22001.0`。**ProgId 里的 hash 随包版本变**，换版本后重新查：  
     > 读 `HKCU\Software\Classes\AppX*\Application\ApplicationName`，看 `ms-resource://...` 里的包名。
   - 方法见上面「踩过的坑」第 14 条（SFTA + Set-FTA）。
2. **豆包的图片打开方式已全部删除**：`Doubao.Image` ProgId 树、`RegisteredApplications` 条目、  
   `AppProtocolHandlers`、各扩展名 `OpenWithProgids` / `OpenWithList` 里的豆包条目（含 `.dib`、`.svg`、`.webp`）。  
   **由守护脚本每 30 秒复查一次**，豆包重建就秒删（实测重建后 5 秒内被清掉）。
3. **悬浮球已永久禁止**（做法见下）。

### 悬浮球 + 注册表 是怎么长期压住的

豆包悬浮球 = `Doubao.exe` 的一个 `Chrome_WidgetWin_1` 窗口，**52×52 ~ 78×78**（尺寸会变），  
带 `WS_EX_TOOLWINDOW | WS_EX_TOPMOST`、无标题。**它没有持久化开关**——  
`public_config.json` 里的 `text_picker` 是"划词"不是它，IndexedDB / Local Storage 里也找不到开关，  
所以**重启豆包它必然回来**（实测确认）。图片关联同理。

于是用一个常驻脚本一次管两件事：**`D:\work\AI产出\豆包处理\tools\doubao_guard.pyw`**

- 每 2 秒扫一遍顶层窗口，命中上面那组特征就发 `WM_CLOSE`（走豆包自己的关闭流程，比强杀窗口干净）
- 每 30 秒清一次 `Doubao.Image` 相关注册表项（ProgId 树 / RegisteredApplications / 各扩展名 OpenWithProgids）
- 日志 `D:\work\AI产出\豆包处理\no_ball.log`；启动器 `tools\start_guard.vbs`
- 开机自启：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\DoubaoGuard`
- 跑在系统 Python 上：`D:\software\Python\pythonw.exe`（**别用 WorkBuddy 托管的 python，怕被清**）
- **启动方式必须走 explorer**（见"踩过的坑"第 13 条）：`explorer.exe "D:\work\AI产出\豆包处理\tools\start_guard.vbs"`
- **卸载**：删掉 Run 值 `DoubaoGuard` + 结束 `pythonw.exe`（或重启电脑）

### 备份 / 复原

处理前的注册表全量备份：  
`D:\work\AI产出\豆包处理\注册表备份\registry-backup-20261010-152125.json`  
（27 个键的完整树，含 `Doubao.Image`、`RegisteredApplications`、各 `FileExts\<ext>`）

---
