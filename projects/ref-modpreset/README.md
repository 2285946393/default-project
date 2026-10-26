# ref-modpreset — Mod Preset Sync（模组预设同步）

工坊 `3774789540` ｜ mod id `ModPresetManager` ｜ 作者 **Flaaax** ｜ v0.5.4
`affects_gameplay: false` ｜ `dependencies: []` ｜ `has_pck: true` ｜ `min_game_version: 0.107.1`

> **详细笔记 → `notes/杀戮尖塔2-拆解-模组预设同步-20261009.md`**
> 这个目录只放原始产物 + 索引。

---

## 为什么拆它

本体是「模组配置导出/导入」工具，但它顺手挖出四样高价值情报：

1. **游戏加载顺序 + 开关的完整机制**（`ModManager.cs` 主流程 / 去重 / 拓扑排序 / 回写）
2. **`affects_gameplay` 的官方口径**：联机必要 = `affects_gameplay:true` + 依赖闭包
3. **一个真实的游戏 bug**：同 id 双来源时，用户「按来源分别开关」会被启动流程吞掉
   （作者写了 `DuplicateSourceSettingsGuard` 专门绕）
4. **第三代「壳 + 版本化实现」框架 `JmcModLib.Dispatch`** —— 目前见过最规范的一套

---

## 目录

```
ref-modpreset/
├── ModPresetManager.json              # 游戏清单（id/name/version/deps/affects_gameplay）
├── ModPresetManager.dispatch.json     # ★ JmcModLib 的分发描述文件（多版本选择表）
├── decompiled/
│   ├── shell/                         # 外壳 ModPresetManager.dll（入口 + 依赖解析器）
│   │   └── JmcModLib.Dispatch.Bootstrap/
│   │       ├── DispatchBootstrap.cs              (72)
│   │       ├── DispatchDescriptor.cs            (123)
│   │       ├── DispatchEntry.cs                  (95)
│   │       ├── BootstrapDependencyResolver.cs   (449)  ← ★★ 完整依赖解析器
│   │       ├── BootstrapLog.cs                   (43)
│   │       └── GameVersionInfo.cs                 (5)
│   ├── runtime-0.107.1/               # 0.107.1 实现
│   └── runtime-0.111.0/               # 0.111.0 实现（当前主力）
│       ├── GodotPlugins.Game/Main.cs             (35)   ← ★ Godot .NET 引导入口
│       ├── ModPresetManager.Scripts/
│       │   └── Entry.cs                          (44)
│       ├── ModPresetManager.Scripts.Presets/
│       │   ├── ModPresetService.cs              (643)  ← ★★★ 核心逻辑
│       │   ├── WorkshopSubscriptionBatch.cs     (226)  ← ★★ Steamworks 批量下载
│       │   ├── GameRestartService.cs             (52)
│       │   ├── ModPresetDocument.cs / ModPresetEntry.cs
│       │   ├── PresetImport*.cs / PresetApply*.cs / PresetExport*.cs
│       │   ├── CurrentModEntry.cs / ModConfigurationComparison.cs
│       │   └── WorkshopDownload*.cs
│       ├── ModPresetManager.Scripts.Patches/
│       │   ├── DuplicateSourceSettingsGuard.cs  (133)  ← ★★★ 绕游戏 bug
│       │   └── ModPresetScreenPatch.cs           (61)  ← 往原生模组界面插按钮
│       ├── ModPresetManager.Scripts.Ui/
│       │   ├── ModPresetModal.cs               (2912)  ← 弹窗（纯代码构建）
│       │   ├── PresetActionButton.cs            (660)
│       │   └── PresetSplitActionButton.cs       (518)
│       └── ModPresetManager.Scripts.Localization/
│           └── ModLocalization.cs                (23)  ← ★ mod 自带本地化
└── pck/                               # 从 pck 提取的文件
    ├── zhs_main_menu_ui.json          # ★ 中文本地化表（114 键，扁平 key→文本）
    └── global_script_class_cache.cfg  # 8 字节（基本空）
```

总计 **6,569 行**（含 0.107.1 与 0.111.0 两份实现）。

---

## 三个最该看的东西

### 1. `BootstrapDependencyResolver.cs` —— 完整的 mod 依赖解析器

解决「mod 自带的兄弟 dll 解析不到」这个老问题（见工作区记忆 §4），
比之前见过的任何一份都完整：

- 用**当前 ALC**（不是 `Default`）：`AssemblyLoadContext.GetLoadContext(typeof(...).Assembly)`
- **同时挂** `loadContext.Resolving` 和 `AppDomain.CurrentDomain.AssemblyResolve`
- `[ThreadStatic] HashSet<string> resolvingNames` —— **重入保护**，防无限解析循环
- `IsCompatible`：名字忽略大小写 + `publicKeyToken` 必须匹配 + **候选版本 ≥ 请求版本**
- `probeDirectories` + `probeAllDlls`（枚举目录下所有 `*.dll`）
- **同名程序集冲突检测**：若同标识的程序集已从别的路径加载 → 抛出**可操作的**错误：
  「请将 Runtime 项目的 AssemblyName 设为不同于入口 DLL 的名称，例如 MyMod.Runtime。」
- `Install()` 用 `Interlocked.Exchange` 保证只装一次

### 2. `DuplicateSourceSettingsGuard.cs` —— 一个真 bug 的绕法

`ModManager.cs:293` 回写 `settings.save` 时**只按 `Id` 匹配、不带 `Source`**：

```csharp
bool isEnabled = _settings.ModList
    .FirstOrDefault(m => m.Id == mod.manifest?.id)?.IsEnabled ?? true;   // ★ 丢了 Source
```

→ 同一个 mod 同时有本地版和工坊版时，两条拿到同一个 `IsEnabled`，
用户「只关工坊版」的选择被抹平。

作者的绕法：**Init 阶段抄快照（按 `(Id, Source)`）→ `CallDeferred` 等
`ModManager.State == Initialized`（最多 120 帧）→ 把被吞的开关写回**。

### 3. `ModPresetService.FindMultiplayerDependencyClosure()` —— `affects_gameplay` 的官方口径

「联机必要」= `affects_gameplay:true` 的 mod **+ 沿 `manifest.dependencies` 的传递闭包**。
可以直接抄来写「联机推荐配置」或「开房前自检」。

---

## 版本适配成本（实测）

`runtimes/0.107.1` vs `runtimes/0.111.0`：6,569 行里**只有 4 个文件不同，
真正的逻辑差异只有 3 处**：

| # | 变化 | 性质 |
|---|---|---|
| 1 | `ParseWorkshopIdFromPath(mod.path)` → `mod.workshopId ?? ParseWorkshopIdFromPath(mod.path)` | 游戏新增 `Mod.workshopId` 属性（0.111.0+） |
| 2 | `((NClickableControl)node).ForceClick()` → `node.ForceToggleTick()` | `NTickbox.ForceToggleTick()` 是新方法 |
| 3 | 新增 `WarnWhenUsingNewerVersionFallback()` | 非 API 变更，是「0.112+ 用 0.111 Runtime」的警告 |

> **跨 4 个小版本，实际适配 = 2 处 API + 1 处警告。**
> 印证了「数值/文案变化自动继承，只有签名真变了才动手」。

---

## 复现

```bash
ILSPY="C:/Users/有花无实/.dotnet/tools/ilspycmd.exe"
W="D:/software/steam/steamapps/workshop/content/2868840/3774789540"

"$ILSPY" -p -o decompiled/shell            "$W/ModPresetManager.dll"
"$ILSPY" -p -o decompiled/runtime-0.107.1  "$W/runtimes/0.107.1/ModPresetManager.Runtime.dll"
"$ILSPY" -p -o decompiled/runtime-0.111.0  "$W/runtimes/0.111.0/ModPresetManager.Runtime.dll"

diff -rq decompiled/runtime-0.107.1 decompiled/runtime-0.111.0   # ★ 适配成本实证
```
