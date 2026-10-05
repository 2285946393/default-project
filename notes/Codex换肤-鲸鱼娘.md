# Codex 换肤（鲸鱼娘）· 2026-09-23

> 给"新任务里回来的我"看的。用户不会敲命令，换肤/切换/还原都由 agent 执行，
> 或者用户点桌面上那两个图标。

## 结论：赌赢了

上游文档里写着「商店版 Codex 从 26.715 起 CDP 被关、换肤失效」（issue #235、#395 至今 Open），
**但这台机器实测是通的**：Codex 26.917.6896.0（商店版）+ Dream Skin v1.5.18，CDP 端口 9335
正常监听，主题注入成功，`verify-dream-skin.ps1` 报告 `pass: true`、`stylePresent: true`。

所以：以后遇到"上游说不行"的结论，这台机器上仍值得实测一次。

## 装了什么

| 组件 | 位置 |
|---|---|
| Codex Dream Skin v1.5.18（换肤引擎，托盘常驻） | `%LOCALAPPDATA%\CodexDreamSkin` + `%LOCALAPPDATA%\Programs\CodexDreamSkin` |
| 主题：深海女仆工坊（日间/夜间） | `%LOCALAPPDATA%\CodexDreamSkin\themes\maid-atelier-day|night` |
| 当前生效主题 | `%LOCALAPPDATA%\CodexDreamSkin\active-theme` |
| 安装包与素材（已核对官方 SHA-256） | `D:\software\codex-dream-skin\`、`D:\software\codex-deep-whale\` |
| 本次安装脚本（定时任务方式） | `D:\software\codex-dream-skin\gamble.ps1` |

主题来源：`github.com/queshirun/codex-deep-whale`（把 `Small-tailqwq/dsh-deep-whale` 的鲸鱼娘
「深海女仆工坊」适配成 Codex Dream Skin 主题，CC BY-NC-SA 4.0，署名链见该仓库 NOTICE.md）。
跟他 DSH 里那套是同一个角色。

## 关键操作事实

- **皮肤只在"用 Dream Skin 的方式启动 Codex"时出现。** 直接点平时的 Codex 图标打开 = 没有皮肤，
  因为普通启动不会开 CDP 调试口。这是该方案的固有限制，不是坏了。
- **日常入口**（已放桌面）：`打开 Codex（鲸鱼娘版）.lnk` → 跑 `start-dream-skin.ps1 -PromptRestart`，
  Codex 正在跑时会弹框问要不要重启（点确定即可）。
- **切换主题**：把 `themes\maid-atelier-day` 或 `-night` 里的 `theme.json / theme.css / background.png`
  覆盖到 `active-theme\` 即可，**实时生效，不用重启**（实测）。
- **还原**：桌面 `鲸鱼娘皮肤面板（切换·还原）.lnk` → 托盘菜单「完全恢复 Codex」；或卸载 Dream Skin。
  恢复脚本会还原 Codex 外观键，并保留 `config.before-dream-skin.toml` 备份。
- **Codex 更新后皮肤大概率失效**：先关 Codex，重跑 `D:\software\codex-dream-skin\` 里的安装器
  （或下载更新版），再点桌面入口。

## 它动了哪些系统设置

- 只往 `~/.codex/config.toml` 加了外观键：`appearanceTheme = "dark"`、`appearanceLightCodeThemeId`、
  `appearanceLightChromeTheme`。
- 原配置完整备份：`%LOCALAPPDATA%\CodexDreamSkin\config.before-dream-skin.toml`。
  **已核对**：模型（deepseek-flash / deepseek provider）、`web_search = "disabled"`、
  `model_catalog_json`、28 条插件配置全部保留，没被动过。
- 安装器要求"Codex 完全关闭"才肯装，所以用了 Windows 定时任务的方式先杀掉 Codex 再装
  （参考 `gamble.ps1`，跑完会自己注销任务）。

## 安全边界（要不要留，看这一条）

CDP 只绑定 `127.0.0.1`，但**没有鉴权**——同一台电脑上的其他进程理论上能连上去读取或控制
Codex 的渲染进程。这是 Dream Skin 这类换肤工具的固有代价，不是本项目引入的。不想要就托盘里
「完全恢复 Codex」，风险窗口随之结束。
