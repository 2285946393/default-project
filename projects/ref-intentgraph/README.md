# ref-intentgraph — Intent Graph（意图图）参考目录

> 工坊 **`3747528152`** · id `intentgraph2` · 作者 **Chaofan** · v **1.6.0**
> `deps: []` · `affects_gameplay: false` · `min_game_version: 0.107.1`
> 拆解日期 **2026-10-09** · 对应笔记 **`notes/杀戮尖塔2-拆解-意图图-20261009.md`**
>
> 原包路径：`D:/software/steam/steamapps/workshop/content/2868840/3747528152/`

## 这是什么

把**怪物意图画成状态机图**：悬停敌人时弹出，显示下一步可能出什么招、
什么条件分支、概率、冷却。不改玩法。**没有开源仓库**（清单里没有链接），全部靠反编译。

## 目录

| 路径 | 内容 |
|---|---|
| `manifest.json` | 原样复制的 `intentgraph2.json` 清单 |
| `pck-filelist.txt` | pck 里 130 个文件的完整清单（含 6 个 `.tscn` 场景名、18 种语言字表） |
| `decompiled/` | ilspycmd `-p` 产出，**88 文件 / 16,143 行** |
| `pck/intentgraph-BASE.json` | 本体 15 个怪的意图图补丁数据（22 KB） |
| `pck/intentgraph-ActsFromThePast.json` | 内容包「往期篇章」46 个怪（58 KB） |
| `pck/intentgraph-Act4Heart.json` | 内容包「第四幕心脏」3 个怪（3 KB） |
| `localization-zhs/intentgraph.json` | **133 条中文**：UI 字符串 + **133 条怪物分支条件说明** |
| `localization-zhs/settings_ui.json` | 设置页 20 条中文（含 `AttackDamageFormat` 的 `{base}/{modified}` 说明） |

## 关键文件速查（`decompiled/`）

| 文件 | 行数 | 为什么看它 |
|---|---|---|
| `intentgraph2/IntentGraph2.Initializer/Initializer.cs` | 87 | **壳**：版本选择 + 兄弟 dll 加载 + 反射调核心（§4.1/4.2） |
| `intentgraph2core/IntentGraph2/IntentGraphMod.cs` | 410 | 主入口：双框架探测、跨 mod 内容包加载、配置读写（§4.3/4.4） |
| `.../IntentGraph2.Utils.GraphGenerator/MonsterStateNodeConverter.cs` | 496 | **反射怪物状态机**的完整逻辑（§2 第一层） |
| `.../IntentGraph2.Utils.GraphGenerator/MoveDetailResolver.cs` | 886 | **扫 IL 字节码**还原招式内容（§2 第二层）★ 最野的一招 |
| `.../IntentGraph2.Utils.GraphGenerator/MonsterStateNodeSimplifier.cs` | 391 | 状态图化简：合并同构节点、识别简单循环、去不可达 |
| `.../IntentGraph2.Utils.Variable/VariableContext.cs` | 227 | **表达式变量表**（`m.*` / `mm.*` / `act` / `ascension`…）§6 |
| `.../IntentGraph2.Utils.Expression/ExpressionParserHelper.cs` | 83 | ANTLR → 表达式树（运算符表） |
| `.../IntentGraph2.Patches/StateLogPatches.cs` | 110 | 打 `MonsterMoveStateMachine.SetCurrentState`，**用栈帧判断"是不是卡牌引起的"** |
| `.../IntentGraph2.Patches/MonsterSetupPatch.cs` | 25 | 敌人进场钩子 `CombatManager.AfterCreatureAdded` |
| `.../IntentGraph2.Patches/ShowIntentGraphPatches.cs` | 35 | 悬停钩子 `NCreature.OnFocus/OnUnfocus/_ExitTree` |
| `.../IntentGraph2.Patches/BestiaryPatches.cs` | 76 | 图鉴页注入 + `PreloadManager.Cache.GetScene` |
| `.../IntentGraph2.Patches/DevConsoleConstructorPatch.cs` | 40 | **怎么注册控制台命令** |
| `.../IntentGraph2.Patches/HasAscensionLevelPatches.cs` | 45 | **怎么伪造进阶等级**（预览别的难度） |
| `.../IntentGraph2.Utils/IntentGraphHost.cs` | 321 | 面板挂载/定位/避让悬浮提示的完整做法 |
| `.../IntentGraph2.Models/IntentDefinition.cs` | 20 | **数据格式 schema**（§5） |
| `.../IntentGraph2.Utils/IntentGraphModConfig.cs` | 61 | 框架无关的配置模型 |
| `intentgraph2baselib/.../HelperModConfig.cs` | 135 | BaseLib 侧配置写法（`SimpleModConfig` + `[ConfigSection]`） |
| `intentgraph2ritsulib/.../RitsuLibHelper.cs` | 107 | RitsuLib 侧配置写法（`DataStore` + `ModSettingsValueBinding`） |

## 复现

```bash
W="D:/software/steam/steamapps/workshop/content/2868840/3747528152"
for d in intentgraph2 intentgraph2baselib intentgraph2ritsulib intentgraph2core; do
  ~/.dotnet/tools/ilspycmd.exe -p -o "decompiled/$d" "$W/$d.dll"
done
python ../frost-spire/tools/list_pck_scenes.py "$W/intentgraph2.pck" > pck-filelist.txt
python ../frost-spire/tools/extract_pck_file.py "$W/intentgraph2.pck" \
       "intentgraph2/intentgraph.json" pck/intentgraph-BASE.json
```

## 包体构成（原包，未复制 dll）

| 文件 | 大小 | 作用 |
|---|---|---|
| `intentgraph2.dll` | 7 KB | 壳 |
| `intentgraph2core.dll` | 305 KB | 核心（当前版本） |
| `intentgraph2core-lt-0.110.0.dll` | 305 KB | 核心（游戏 < 0.110.0） |
| `intentgraph2baselib.dll` | 8 KB | BaseLib 适配器 |
| `intentgraph2ritsulib.dll` | 15 KB | RitsuLib 适配器 |
| `Antlr4.Runtime.Standard.dll` | 192 KB | ANTLR4 运行时 |
| `intentgraph2.pck` | 434 KB | 数据 + 场景 + 18 语言字表 |
