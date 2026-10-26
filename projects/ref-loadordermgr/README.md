# ref-loadordermgr · Load Order Manager / 模组加载顺序管理

- 工坊 id：`3747605109` · 版本 `0.4.0` · 作者 `@Bilibili我叫煎包`
- 依赖：无 · `affects_gameplay: false` · `min_game_version: 0.107.1`
- 目录结构：**壳 + 版本化实现**（和 RitsuLib / 尖塔：琐事同一套路）
  - `LoadOrderManager.dll`（16,896 B）= 壳
  - `bin/g0.107.1/LoadOrderManager.Impl.dll`（94,208 B）= 实现
  - `mod_manifest.json` ← ⚠️ **清单文件名不是 `<id>.json`**（盘点脚本要按内容找，不能按名字找）
  - `i18n/*.lang` 15 种语言

## 为什么拆它

它的说明写着「保存到 settings.save，下次启动生效」——顺着这条线挖出了
**游戏本体自带的 mod 开关与加载顺序机制**（`settings.save` → `mod_settings.mod_list[].is_enabled`）。

详见 `notes/杀戮尖塔2-拆解-mod开关与加载顺序-20261009.md`。

## 产物

- `LoadOrderManager.json` —— 原始清单
- `decompiled/LoadOrderManager.Impl.decompiled.cs`（178,807 B，ilspycmd 8.2）

## 复现

```bash
"C:/Users/有花无实/.dotnet/tools/ilspycmd.exe" -o decompiled \
  "D:/software/steam/steamapps/workshop/content/2868840/3747605109/bin/g0.107.1/LoadOrderManager.Impl.dll"
```
