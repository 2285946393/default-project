# WorkBuddy 国际版 / 国内版隔离方案（2026-10-14）

## 当前最终目录（17:55 重新安装后）

- **国际版**：`D:\software\WorkBuddy AI\WorkBuddyAI\WorkBuddyAI.exe`（5.7.6）
- **国内版**：`D:\software\WorkBuddyIntl\WorkBuddy.exe`（5.8.2）
- 安装包备份：`D:\software\WorkBuddyGlobal\WorkBuddyGlobal-setup.exe`
- `tools/wb.mjs` 已指向上面国际版新路径

## 为什么不会再互相覆盖

- 两个版本现在在**完全不同的目录**，文件名也不同
- 国内版的卸载注册项已隐藏到 `BFD312E9-...codex-hidden`，更新器找不到它
- 临时脚本：
  - `C:\Users\有花无实\Tools\wb\hide-wb-registry.cmd`
  - `C:\Users\有花无实\Tools\wb\restore-wb-registry.cmd`

## 历史记录（别再按这个找）

- 曾经国际版装在 `D:\software\WorkBuddy AI\WorkBuddyAI.exe`
- 后来被国内版更新清空；中间一度把国际版 5.7.6 备份成 `D:\software\WorkBuddyIntl\WorkBuddyAI.exe`，
  但它的 asar 校验和 5.8.2 目录不匹配，启动即崩


## 常用命令

- 隐藏国内版注册项（当前已生效，重复运行也安全）：
  `C:\Users\有花无实\Tools\wb\hide-wb-registry.cmd`
- 恢复注册项（要卸载国内版 / 让系统正常显示它时）：
  `C:\Users\有花无实\Tools\wb\restore-wb-registry.cmd`

## 相关引用

- `tools/wb.mjs` 已改回 `WORKBUDDY_APP_EXECUTABLE=D:\software\WorkBuddyIntl\WorkBuddyAI.exe`
- `tools/_desktop_lnks.json` 里还有旧的 `D:\software\WorkBuddy AI\WorkBuddyAI.exe` 记录，以后如果重建快捷方式要注意

