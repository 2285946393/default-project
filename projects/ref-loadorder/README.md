# ref-loadorder — LoadOrderManager（模组加载顺序管理）

工坊 **`3747605109`** ｜ 作者 **@Bilibili我叫煎包** ｜ v**0.4.0**
`has_pck: false`（纯代码 UI）｜ `has_dll: true` ｜ `affects_gameplay: false` ｜ `min_game_version: 0.107.1`
**不依赖 RitsuLib / BaseLib**（`deps` 未声明，全自研）

拆解笔记 → `notes/杀戮尖塔2-拆解-加载顺序管理-20261009.md`

## 它是什么

给游戏自带的模组界面**补一个"加载顺序编辑器"**（本体只有启用/禁用勾选框，没有排序）。
顺带提供了：预设（多套配置）、新模组策略、导入/导出剪贴板、智能排序（依赖拓扑）。

**★ 三条最值钱的结论**（详见笔记 §2 / §4 / §5）：
1. 手动顺序只是拓扑排序的**平局决胜**；游戏启动后**会用它排好的顺序覆盖 `settings.save` 的 `mod_list`**，
   只有每条 mod 的 `IsEnabled` 会被保住。
2. **怎么正规写 `settings.save`**（反射路径全在这）+ **账户隔离守卫**（防覆盖别的账号配置）。
3. 写外部系统后留**快照 + 子序列校验**来检测"被覆盖"。

## 目录

```
mod_manifest.json                原始清单（注意：文件名是 mod_manifest.json，不是 <id>.json）
i18n/*.lang                      14 种语言的 UI 字符串（zhs.lang 是中文）
decompiled/
  LoadOrderManager/              壳（16 KB）→ 反编译 4 文件 / 507 行
    ModVersionLoader/VersionLoader.cs     ★ 版本探测 + 挑实现 + 加载（354 行）
    ModVersionLoader/GameVersion.cs       游戏版本探测（release_info.json → NGame.GetGameVersion）
    GodotPlugins.Game/Main.cs             godotsharp_game_main_init 入口
  LoadOrderManager.Impl/         实现（94 KB）→ 反编译 14 文件 / 5,023 行
    LoadOrderPanel.cs             2748 行，纯代码搭 UI 面板
    LoadOrderRuntime.cs            893 行，★ 读写 settings.save + 漂移检测
    SelfTest.cs                    407 行，环境变量驱动的端到端自检
    ModPresetStore.cs              300 行，预设存储
    I18n.cs                        326 行，独立 .lang 加载器 + 语言 tag 归一化
    LoadOrderUiInjector.cs          62 行，往 NModdingScreen 插按钮
    ModdingScreenReadyPatch.cs      28 行，Harmony Postfix 挂 NModdingScreen._Ready
    DebugLog.cs / LoadOrderEntry.cs / ModPreset.cs / PresetSaveData.cs / LoadOrderManagerMod.cs
```

合计 **18 文件 / 5,530 行**。

## 关键文件速查

| 想知道 | 看 |
|---|---|
| 壳怎么按游戏版本加载实现 | `LoadOrderManager/ModVersionLoader/VersionLoader.cs` |
| 游戏版本怎么探测 | `LoadOrderManager/ModVersionLoader/GameVersion.cs` |
| **怎么写 `settings.save`** | `LoadOrderManager.Impl/LoadOrderManager/LoadOrderRuntime.cs` :200-295 |
| **账户隔离守卫** | 同上 `:386-409` |
| 漂移检测 / 快照 | 同上 `:594-704` |
| 纯代码搭 UI | `LoadOrderPanel.cs` :310-543 |
| 两帧延迟布局自检 | 同上 `:545-704` |
| 智能排序（依赖拓扑） | 同上 `:965-1075` |
| 预设只存差异 | `ModPresetStore.cs` |
| 环境变量自检 | `SelfTest.cs` |
| 独立 `.lang` 加载器 | `I18n.cs` |

## 游戏本体侧对照（不用重新反编译）

`projects/sts2-moddev-workspace/02-游戏反编译资料/版本-0.111.0/反编译源码/MegaCrit.Sts2.Core.Modding/`

- `ModManager.cs` — `SortModList()` :345（Kahn 拓扑 + 手动顺序当优先级）；
  **覆盖 `mod_list` 的代码在 :287-299**；`AssociateAssemblyWithMod` :1194
- `ModSettings.cs` / `SettingsSaveMod.cs` / `ModSource.cs` — 设置模型

## 复现

```bash
cd "C:/Users/有花无实/Documents/Default Project"
W="D:/software/steam/steamapps/workshop/content/2868840/3747605109"
"C:/Users/有花无实/.dotnet/tools/ilspycmd.exe" -p -o decompiled/LoadOrderManager      "$W/LoadOrderManager.dll"
"C:/Users/有花无实/.dotnet/tools/ilspycmd.exe" -p -o decompiled/LoadOrderManager.Impl "$W/bin/g0.107.1/LoadOrderManager.Impl.dll"
```
