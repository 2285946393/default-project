# IL2CPP 第七批：REA 接入评估 + 霓虹深渊2 道具体系全量

> 时间：2026-10-12 下午
> 触发：用户装了 4 个 skill 包（45 个 skill，落在 `C:\Users\有花无实\.claude\skills\`），
> 其中 `reverse-engineer-anything`（REA）就是之前说的"让别的 AI 下的新工具"。
> 同时按暂停令：**不新开游戏反编译**，只吃存量 dump。

---

## 一、REA 实测评估（结论：这条 IL2CPP 链路上暂时用不上）

### 装了什么

| 项 | 值 |
|---|---|
| 版本 | `rea@6.4.0` |
| CLI 路径 | `D:\work\AI产出\rea-cli\node_modules\.bin\rea` |
| skill | `C:\Users\有花无实\.claude\skills\reverse-engineer-anything\`（SKILL.md 13.5KB + 6 篇 reference） |
| 标称工具数 | 170（metadata 里写的） |
| MCP | **没注册**。SKILL.md 明说"装 skill 只给说明书，不注册 MCP server"，所以只能走 CLI |

### `rea doctor` 结果

```
healthy: false
  node      ok  22.22.2
  host      ok  win32 x64
  hopper    ✗  missing_analysis_engine
  ghidra    ✗  GHIDRA_INSTALL_DIR is not set
  ida       ✗  IDA MCP registration is not configured
  skill:identity ✗ config_drift（装进来的 skill 身份过期）
  registration: claude_code / codex / gemini / copilot 全部 ✗ config_drift
```

### `rea capabilities`：18 项里本机只有 8 项能用

```
OK   decode_interface_builder        （Apple nib/storyboard，本机无目标）
OK   extract_artifact                （ZIP/APK/IPA/DMG 解包）
OK   inspect_artifact                （包内清单 + 观察）
OK   inspect_keyed_archive           （Apple keyed archive）
OK   inspect_managed_artifact        ★ 托管 PE/CLI
OK   inspect_managed_members         ★ 托管成员清单
OK   inspect_managed_native_boundaries
OK   trace_dylib_resolution
---  capture_native_ui_scenario / demangle_swift / inspect_asset_catalog
     inspect_macho / inspect_native_dispatch_metadata / inspect_plist
     inspect_signature / list_architectures / observe_native_calls
     observe_native_ui                        ← 全部 macOS-only 或需引擎
```

### 实测（不是猜的）

对 `训牌师/Decktamer_Data/Managed/Assembly-CSharp.dll` 跑
`rea inspect-managed-artifact --format json`：

- 只出：PE 身份、sha256、evidence_id、3 个 file-offset 位置
- **limitations 原文**：
  `Static inspection did not load the assembly, resolve dependencies through a CLR,
   decompile C#, or execute target code.`
- 即：**明确不反编译 C#**，只给元数据清单

### 对我这条链路的三条判断

| 场景 | REA 能补什么 | 结论 |
|---|---|---|
| **Mono 游戏**（有 `Assembly-CSharp.dll`） | 只有成员清单 | ❌ **不如 ilspycmd**——我已经能出全量可读 C# 源码 |
| **IL2CPP 游戏**（`GameAssembly.dll` + `global-metadata.dat`） | 文档里搜不到 `il2cpp` / `unity` 任何一条；数值在 Unity 资产序列化流里，REA 不管这个格式 | ❌ **帮不上**，还是得靠 Il2CppDumper + 自研 `parse_with_types.py` |
| **原生反编译 / 伪代码** | 需要 Ghidra 12.1.x 或 Hopper 或 IDA | ❌ **本机都没有**；且 REA setup 明确不代装 Java/Ghidra/IDA |

### 什么时候 REA 才真有用

1. 目标换成 **Electron / ASAR**（比如 WorkBuddy 的 `app.asar`、别的 Electron 客户端）
   → `analyze-javascript-application` / `recover-javascript-sources`，这块是它强项
2. 目标换成 **APK / IPA / Mach-O / 固件**
3. **愿意装 Ghidra 12.1.x + Java** 并设 `GHIDRA_INSTALL_DIR`
   → 才能啃 `GameAssembly.dll` 里的硬编码公式本体（例如霓虹深渊2 的 `luckyFormula` 怎么求值）

**当前动作：REA 归档为"备胎"，主力仍走 Il2CppDumper 链。不为了用 REA 而改用 REA。**

---

## 二、霓虹深渊2 道具体系全量（本次真收获）

> 工具：`C:\Temp\powerups_extract.py`（新建，已可用）
> 用法：`python powerups_extract.py <dump.cs> <out.json> <命名空间>`
> ★ 坑：Il2CppDumper 的命名空间是**独立的 `// Namespace: X` 注释行**，不写在类名里。
>   第一版按类名前缀 `NEON.Game.PowerUps` 找，结果 0 个类——必须先记 namespace 再拼全名。

### 规模

| 指标 | 数 |
|---|---|
| `NEON.Game.PowerUps` 下类数 | **1214** |
| 可序列化字段总数（含继承链展开） | **70821** |
| 直接基类分布 | StatBoost 318 / LuckStatBoost 203 / 无基类 97 / ManaSecSkillPowerUp 63 / PowerUp 42 / SecSkillPowerUp 21 / ExtraJumpPowerUp 19 / CriticalPowerUp 17 / MonsterPowerUp 17 / … |

### 继承链（三层，"表现 ↔ 数值" 分离）

```
Loot                  sprite / isTestItem / needUse / id / curse / quality / qualityIsShow
 └ PowerUp            表现层：Name / Description / SoundEffect / BackGroundPrefab /
      (42)           PickUpPrefabId / showBanner / suit / subSuits / isSuit /
                     ConversionPowerUp（可转化）/ UniqueDrop / isRoleOwn / textShowCd
   └ StatBoost        数值层：values(PlayerStateStringFloat) / tagDamageMult(string 公式) /
        (318)         tagDamageMultRange(Vector2 区间) / 外观覆盖（帽子/头发/枪口偏移…）
     └ LuckStatBoost  幸运层：luckyFormula(string) / luckDeviateMin-Max / CD /
          (203)       luckyFormulaInBattle(string) / luckDeviateMin-MaxInBattle / CDInBattle /
                      luckLevel / newCd / newCdList(float[])
```

### ★ 直接能抄的 10 条数值设计手法

1. **公式存字符串** — `luckyFormula` / `luckyFormulaInBattle` 是 string，不是硬编码。
   改数值不用改代码、不用重新编译，热更直接换串。
2. **数值区间化** — `tagDamageMultRange`（Vector2）、`luckDeviateMin/Max`、
   `level1DropRange`（Vector2Int[]）。**不给单值，给范围**，每次落在区间内随机。
3. **战斗内 / 战斗外两套独立参数** — `CD` vs `CDInBattle`、
   `luckyFormula` vs `luckyFormulaInBattle`。同一道具在两种语境下曲线不同。
4. **分级数组按等级索引** — `newCdList: float[]`，配合 `luckLevel` 取档，
   不用写 `if(level==1)…` 的分支。
5. **保底 / 递减概率**（`CompanyChest`）——
   ```
   initialSuccessRate      初始成功率
   successRateDecrement    成功后降多少
   failureRateDecrement    失败后降多少
   minSuccessRate          地板值
   ```
   四件套就是一套完整的动态难度 + 保底，比"固定 5% 出货"高级得多。
6. **成本按等级递增 + 掉落分区间**（`GambleChest`）——
   `costCountPerLevel: int[]` 配 `level1DropRange` / `blackLevel1DropRange`（黑箱版），
   一个道具同时是"抽奖机"和"成长曲线"。
7. **把数学期望显式写进配置**——`GambleChest` 有 `奖励期望: float[]` 和 `浮动值: float[]`
   （是的，**开发组用了中文标识符**）。设计者自己把 EV 算好填进去，调数值时一眼看到盈亏。
8. **诅咒值做负向平衡** — `Loot.curse: int`，道具自带"代价"维度，
   强力道具用 curse 对冲，不用另开一套 debuff 系统。
9. **套装体系** — `suit` / `subSuits` / `isSuit`，配合 `Tags`、`KeyWords` 做交叉检索
   （和金属之泪的 `SynergyFamily` 是同一思路）。
10. **元数据与数值分层** — `PowerUp` 只管"长什么样、叫什么、属于哪套"，
    数值全下沉到 `StatBoost`。美术/策划改表现不会碰到平衡。

### 对 DouSpire 的直接用处

- `luckyFormula` 那套 → 高级难度 AI 的"出牌倾向公式"可以同样存成字符串，
  调难度只改串，不动dll。
- `curse` 维度 → 斗地主的"强力梗卡"可以挂一个代价字段（比如明牌/让对手多看一张），
  用同一个字段做平衡，不用给每张卡写特判。
- `minSuccessRate` 保底四件套 → 抽卡/发牌的保底机制直接照抄。

---

## 三、当前状态与下一步

### 已完成
- [x] REA 6.4.0 接入评估（有实测证据，结论：本链路暂不用）
- [x] 霓虹深渊2 道具体系全量抽取（1214 类 / 70821 字段 →
      `C:\Temp\parsed\霓虹深渊2_powerups.json`）
- [x] 新工具 `powerups_extract.py`（按命名空间抽类+继承链字段，通用）

### 三款补跑（本轮到此为止，用户叫停）

| 游戏 | 产物 | 状态 |
|---|---|---|
| **寄居蟹** | `寄居蟹_parsed.json` **947KB / 1862 个对象** | ✅ **完成** |
| Shroom and Gloom | `ShroomandGloom_scripts.json` 385KB（4993 脚本） | ⏸ 映射建好，**解析没跑完** |
| BALLxPIT | `BALLxPIT_scripts.json` 236KB（映射已有） | ⏸ **没开始** |

**寄居蟹明细**：`resources.assets` 451/2060（超时中断）· `sharedassets0.assets`
**990/2061 完整** · `sharedassets7.assets` 297/1093（超时）· `level7` 114/1189（超时）·
`globalgamemanagers.assets` 10/32。
命中类 TOP：`ActivateOnPlayerConstraint` 679 · `WaterLevelVisualApplicator` 537 ·
**`GeneralAiEntityStatsData` 38 · `BasicAiEating/Fleeing/IdlingStatsData` 各 20** ·
`DifficultyData` 21 · `DifficultyRuntimeLogic` 21
→ **寄居蟹的敌人 AI 是「同一套 StatsData 换数值」的分级做法**，和咖啡星球工匠的 Brain 组件是同一个思路（已第二次见到）。

**下次续跑（一条命令）**：
```bash
export PYTHONUNBUFFERED=1
PY="C:/Users/有花无实/.workbuddy/binaries/python/versions/3.13.12/python.exe"
TOOLS="C:/Users/有花无实/Documents/Default Project/tools/il2cpp"
GAME="D:/BaiduNetdiskDownload/game"
for pair in "ShroomandGloom|Shroom and Gloom" "BALLxPIT|BALLxPIT"; do
  key="${pair%%|*}"; g="${pair##*|}"
  dd=$(find "$GAME/$g" -maxdepth 2 -type d -name "*_Data" | head -1)
  "$PY" "$TOOLS/parse_with_types.py" "/c/Temp/dumps/$key/dump.cs" "$dd" \
       "/c/Temp/parsed/${key}_parsed.json" \
       --scripts "/c/Temp/parsed/${key}_scripts.json" \
       --maxfile 120 --deadline 120 --topfiles 6
done
```

### 待办
- [ ] **PlayMakerFSM 打通**（Blue Prince 6982 个状态机）
      - 现状：`resources.assets` 上 `--only PlayMakerFSM` 得 **0/265**
        （该文件的 265 个 MonoBehaviour 里没有 FSM；FSM 在 `level2`，108MB）
      - 下一步：往 `level2` 上跑；若仍 0，走"绕开多态 FsmStateAction"路线——
        数值其实在 `Fsm.variables` 的 `floatVariables/intVariables/boolVariables/stringVariables`
        和 `ActionData` 的 `fsmFloatParams/fsmIntParams/fsmStringParams` 里，
        不必解出多态 action 实例
      - PlayMakerFSM 在 MonoScript 映射里只有 4 条（疑似跨文件重复），需先去重
- [ ] 金属之泪 `Zaia.AI` 229 个状态机
- [ ] v39 三款（东丁王 / 推币机 / Enter the Chronosphere）：试 `roytu/Il2CppDumper` v39 fork
- [ ] `C:\Temp\dumps`（265MB dump.cs）+ `C:\Temp\parsed` 会被系统清理。
      **dump.cs 不进 git**（265MB），只把 `*_parsed.json` / `*_powerups.json` / `*_scripts.json`
      这类几 MB 的成品挪进仓库

### ⚠️ 提醒
用户明确说过 **"先别急着反编译其他的"**——本批没有新开任何游戏的 dump。

---

## 四、性能事故复盘：解析器为什么会"卡死"，怎么修的

> 起因：用户问"怎么还在运行"——后台跑了 30 分钟没任何输出。这是两个独立问题叠加。

### 症状

`parse_with_types.py` 扫寄居蟹，33 分钟零输出，看起来像死机。

### 根因一：看不见 ≠ 没在动（输出缓冲）

批量脚本里写的是 `"$PY" ... 2>&1 | tail -25`。
**Python 的 stdout 一旦接管道就从行缓冲变成块缓冲**，日志全攒着，
只有进程退出才一次性吐出来。`tail` 也会等到输入结束才输出。

→ 结果：**它其实在算，但一句都不吭声**。这类"假死"排查第一件事是先确认是不是缓冲。

修复：
- 脚本里去掉 `| tail`，设 `export PYTHONUNBUFFERED=1`
- 所有 `print` 加 `flush=True`

### 根因二：数组逐元素重解析导致成本相乘（真慢）

`Reader.read_field` 的数组分支里，为了取到每个元素的值，是这样写的：

```python
probe.layout(flds2(einfo), start)      # 先探测一个元素的大小
for i in range(n):                      # 再对每个元素重新完整解析一遍
    probe2.layout(flds2(einfo), start + i * esz)
```

单个元素是常量级没事，但**元素里再套数组就是乘法**：外层 1000 个 ×
内层 1000 个 = 100 万次解析。寄居蟹正好有这种"数组的数组"。

修复（三道闸）：
1. **`FIELD_BUDGET = 20000`** —— 每个对象的字段读取总预算，
   从 `Reader.__init__` 一路透传到所有子 Reader（`probe` / `probe2` / `r` / `sub`），
   用尽就 `raise EOFError` → 判定布局有误并丢弃，不会无限算下去。
2. **`MAX_ARRAY_ELEMS = 64`** —— 元素数超限时**只取前 64 个的值，其余按 `esz` 直接跳过**。
   关键点：跳过的**字节数仍然正确**，所以外层的长度自校验照常生效，只是丢了尾部的值。
3. **`--deadline <秒>` 每文件预算** + **`--topfiles <N>` 只取最大的 N 个资产文件**。

### 根因三：超时判断写错了位置（二次翻车）

第一版修复把超时检查放在了日志打印里：

```python
if n_tot % 2000 == 0:          # ← 只有扫满 2000 个才进来
    el = _time.time() - t0
    if deadline and el > deadline:
        return
```

结果 `level7` 这个文件**对象数不到 2000 却跑了 10 分钟**——
因为单个对象极慢，`n_tot` 永远到不了 2000，超时判断一次都没执行。

修复：把超时检查挪到**每个对象之后**（`time.time()` 相对每对象几十毫秒的开销可忽略），
打印日志仍然每 2000 个一次：

```python
n_tot += 1
if deadline:
    el = _time.time() - t0
    if el > deadline:
        print('[超时] ...'); return
if n_tot % 2000 == 0:
    print('      ... 已扫 %d 个，命中 %d，用时 %.0fs' % ...)
```

### 教训（写进做法）

1. **后台长任务必须有实时进度输出**，且要确认它没被缓冲。看不到进度等于没法判断死活。
2. **跑之前先量规模**：对象数、最大文件大小、有没有嵌套数组。
   我这次是没量就全量跑，属于判断失误。
3. **超时检查不能依赖"处理完 N 个才触发"**，慢就慢在单个任务上，必须按墙钟时间判定。
4. 修完要**用已知结果做对照**验证没改坏：
   这次用 Blue Prince `resources.assets`（改前 0/265 / 3 秒）对照，改后完全一致。
