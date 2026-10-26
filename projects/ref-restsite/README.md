# ref-restsite · 休息处扩展 RestSiteExpansion

工坊 id **3802537170** · 作者 **Dixie** · 版本 **v1.1.0**
依赖 **STS2-RitsuLib ≥ 0.5.20** · `min_game_version: 0.107.1` · `has_dll + has_pck`

> 这是**反编译参考目录**，不是我们的工程。只读，不改。
> 我们的 mod 源码归国内 WorkBuddy 维护（2026-10-09 起分工）。

## 为什么拆它

**它和 DouSpire 是同一个域** —— 都在给游戏本体的**休息处（篝火）**加选项。
我们的 `projects/dou-spire/DouSpireCode/Table/DouSpireRestSite.cs` 开头就写着
「照着 RestSiteExpansion / Miyabists2 两个现成 mod 抄的」。
这次是把那个"抄"补成"懂"。

它把休息处从 2 个选项扩到 **14 个**，并且顺手解决了两个我们一定会撞上的问题：
- **选项超过 5 个一行放不下** → 劫持 `%ChoicesContainer` + 加滚动 + 重算 FocusNeighbor
- **多人联机是按下标同步选项的** → 必须确定性排序，否则两端串位

## 目录

```
RestSiteExpansion.json    原始清单
zhs-relics.json           从 pck 里提出的中文文案（135 条，含全部选项名/说明/设置项）
decompiled/               ilspycmd 项目模式产物（51 文件 / 4,205 行）
  RestSiteExpansion/      主逻辑（4,205 行里绝大部分）
  RestSiteExpansion.Cards / .Powers / .Relics   模板占位（各 10~40 行，空壳）
  RestSiteExpansion.csproj
```

## 关键文件速查

| 文件 | 行数 | 讲什么 |
|---|---|---|
| `RestSiteExpansionSingleton.cs` | 150 | **★ 官方口子 `TryModifyRestSiteOptions` 的用法** + 确定性排序表 `DesiredOrder` |
| `RestMenuState.cs` | 285 | ★★ 子菜单机制（4 组定义 / 开合状态 / 可见性判定 / 自检） |
| `RestSiteSubmenuFilter.cs` | 259 | ★★ 按子菜单过滤可见性 + 排序 + 重排 |
| `RestSiteChoicesLayout.cs` | 465 | ★★★ 布局适配（唯一名劫持 + 校验回滚 + 滚动 + 字高实测 + FocusNeighbor） |
| `RestSiteChoicesLayoutPatch.cs` | 13 | 挂 `NRestSiteRoom._Ready` Prefix |
| `RestSiteExpansionRuntimeConfig.cs` | 263 | ★★ 联机房主权威（RitsuLib `RunSavedData`） |
| `SleepBuffCombatSingleton.cs` | 146 | ★★ `HookedSingletonModel : base(HookType.Combat)` 战斗内生效 |
| `BuriedGoldRuntime.cs` | 88 | ★ 两层状态（局内 `RunSavedData` + 持久 `ModDataStore`） |
| `BuryCardRestSiteOption.cs` | 119 | 具体选项怎么写（选牌 / 说明多分支 / 埋藏） |
| `TentBalanceGate.cs` | 87 | 帐篷平衡（读游戏自己的 `RestSiteChoices` 历史） |
| `Heal/SmithRestSiteOptionTentBalance*Patch.cs` | 22×4 | 给**原版选项**打补丁 |
| `ModConfigPage.cs` | 302 | RitsuLib 设置页（13 项） |
| `RestSiteOptionAccess.cs` | 17 | 反射读 `protected Owner` |

## 一句话结论（详见笔记）

**这是「给游戏本体加玩法选项」的标准答案。** 它不用 Harmony 硬打，走的是
游戏官方的 `AbstractModel.TryModifyRestSiteOptions` 钩子；
UI 层用「唯一名劫持 + 校验回滚」解决容器不够放的问题；
联机用「房主写快照」解决配置分歧；并且**明确处理了"选项是按 uint 下标同步的"这个隐形炸弹**。

完整拆解见：`notes/杀戮尖塔2-拆解-休息处扩展-20261009.md`

## 游戏侧 & 框架侧配套源码（本机已有，不用反编译）

```
projects/sts2-moddev-workspace/02-游戏反编译资料/版本-0.111.0/反编译源码/
  MegaCrit.Sts2.Core.Entities.RestSite/RestSiteOption.cs          ← 基类
  MegaCrit.Sts2.Core.Entities.RestSite/{Smith,Dig,Heal,…}.cs      ← 原版选项
  MegaCrit.Sts2.Core.Hooks/Hook.cs                                ← ModifyRestSiteOptions
  MegaCrit.Sts2.Core.Models/AbstractModel.cs                      ← TryModifyRestSiteOptions 定义
  MegaCrit.Sts2.Core.Nodes.RestSite/NRestSiteButton.cs            ← 按钮 / 下标同步
  MegaCrit.Sts2.Core.Multiplayer.Game/RestSiteSynchronizer.cs     ← ★ 按下标同步
  MegaCrit.Sts2.Core.Models.Relics/Shovel.cs                      ← 遗物加选项的标准写法

projects/ref-ritsulib/upstream/STS2-RitsuLib-main/src/
  Scaffolding/Content/ModRestSiteOptionTemplate.cs
  Models/HookedSingletonModel.cs                                  ← HookType: None/Combat/Run
```
