# 凛冬尖塔 · 波次刷怪卡死修复 + 地图六层结构（2026-10-06 21:2x）

> 用户原话：「看日志。在你查出一波怪后结束回合不再正常运行。然后这个地图节点设置应该按凛冬那个游戏来
> （有商店回血火堆啥的）。每个一次大战斗（16波）。然后下一幕然后大战斗然后下一幕然后下一幕然后大战斗然后结束」

结论：**卡死是真 bug（僵尸怪），地图结构是设计变更（已按用户描述落地 6 层）**。
另外顺带查清了一个藏了两轮的**视觉路径坑**。

---

## 一、现场：日志里写得很清楚（任务 A）

`AppData/Roaming/SlayTheSpire2/logs/godot.log`：

```
[WARN]  Asset not cached: res://scenes/creature_visuals/leaf_slime_m.tscn
[ERROR] System.InvalidOperationException: Creature Creature 青菜成精 has slot name 'first'
        but NCombatRoom.EncounterSlots is null.
   at NCombatRoom.AddCreature_Patch1(...)
   at CreatureCmd.Add(Creature creature)
   at CreatureCmd.Add[T](ICombatState combatState, String slotName)
   at FrostSpire.Relics.FrostPactRelic.SpawnMonster(...)

[INFO]  Monster LEAF_SLIME_M performing move UNSET_MOVE
[ERROR] Combat #1 turn loop died while its combat is in progress; the combat is stuck
        until the room is restarted:
        System.InvalidOperationException: No move has been set for the monster
   at MoveState.UnsetMove(...) → MoveState.PerformMove(...) → MonsterModel.PerformMove()
   at Creature.TakeTurn() → CombatManager.ExecuteEnemyTurn(...) → CombatManager.StartTurn(...)
```

现象对上用户描述：**第一波打完、刷第二波 → 结束回合 → 卡死**。

---

## 二、根因链：一只「没有 move 的僵尸怪」

三步，缺一不可：

1. **M3.4 为了让怪别叠一起，给 `CreatureCmd.Add` 传了槽位名**（`"first"` 之类）。
2. `CreatureCmd.Add` 最终走到 `NCombatRoom.AddCreature`，那里有：

   ```csharp
   if (creature.SlotName != null) {
       if (EncounterSlots == null)
           throw new InvalidOperationException(
               $"Creature {creature} has slot name '{creature.SlotName}' but NCombatRoom.EncounterSlots is null.");
       nCreature.GlobalPosition = EncounterSlots.GetNode<Marker2D>(creature.SlotName).GlobalPosition;
   }
   ```

   而 `EncounterSlots` **只在遭遇自带场景（`_visuals.Encounter.HasScene`）时才被赋值**；
   本体遭遇绝大多数没有 → 它**恒为 null** → **只要传槽位名就必抛异常**。

3. **要命的是抛异常的时机**：此时怪**已经进了战斗**（`CreatureCmd.Add` 前半段已跑完），
   只是位置没设、后续初始化被中断 → 场上留下一只「没有 move 的僵尸怪」。
   下一个敌方回合 `MonsterModel.PerformMove()` 直接抛
   `No move has been set for the monster` → **回合循环死亡，整场战斗卡死**。

> ★ 教训：M3.4「用槽位名摆位」的方向本身就错。
> **正确做法 = 不传槽位（`slotName` 本来就是可选参数）+ 自己排布。**

### 修法

- `FrostPactRelic.SpawnMonster` 去掉 `slot` 参数，全部 `CreatureCmd.Add<T>(combatState)`；
- 删掉 `PickFreeSlots` / `StandardSlots`；
- 新增 `RelayoutEnemies(combatState)` —— 算法**逐行复刻** `NCombatRoom.PositionEnemies`：

  ```csharp
  const float span = 960f;                  // 敌方区域宽度（scaling = 1）
  float gap = 70f;
  float widths = nodes.Sum(n => n.Visuals.Bounds.Size.X);
  float need = widths + (nodes.Count - 1) * gap;
  float x = Math.Max((span - need) * 0.5f, 150f);
  float lift = 0f;
  if (x + need > span && nodes.Count > 1) {
      gap = Math.Max((span - 150f - widths) / (nodes.Count - 1), 5f);
      need = widths + (nodes.Count - 1) * gap;
      x = (span - need) * 0.5f;
      if (gap < 30f) lift = float.Lerp(60f, 40f, (gap - 5f) / 25f);
  }
  for (int i = 0; i < nodes.Count; i++) {
      var node = nodes[i];
      node.Position = new Vector2(x + node.Visuals.Bounds.Size.X * 0.5f, 200f - ((i % 2 != 0) ? lift : 0f));
      x += node.Visuals.Bounds.Size.X + gap;
  }
  ```

  位置从 `NCombatRoom.Instance` + `GetCreatureNode(enemy)` 拿（两个都是 public，不用反射）。

---

## 三、任务 B：地图改成 6 层（大战斗 / 幕 交替）

### 用户要的结构

```
大战斗(16波) → 幕 → 大战斗(16波) → 幕 → 幕 → 大战斗(16波) → 结束
```

### 落地方案

`FrostRiftActMap`：`EncounterRows` 由 **2 → 6**，并区分两类层：

```csharp
/// 大战斗层：踩上去 = 打那场 16 波
private static readonly int[] BattleRows = { 1, 3, 6 };

/// 幕层候选池：商店 / 火堆 / 事件 / 宝箱（4 选 3 洗牌）
private static readonly MapPointType[] InterludePool =
{
    MapPointType.Shop, MapPointType.RestSite, MapPointType.Unknown, MapPointType.Treasure,
};
```

地图形状（3 列 × 6 层，每层 3 个岔路选 1）：

```
        col 1     col 3     col 5
  row 0         [ 起点 ]               ← Ancient
  row 1   ① ──── ② ──── ③             ← 大战斗（16 波）
  row 2   ① ──── ② ──── ③             ← 幕：商店/火堆/事件/宝箱
  row 3   ① ──── ② ──── ③             ← 大战斗（16 波）
  row 4   ① ──── ② ──── ③             ← 幕
  row 5   ① ──── ② ──── ③             ← 幕
  row 6   ① ──── ② ──── ③             ← 大战斗（16 波）
  row 7         [ 终点 ]               ← Treasure（走完 = 出裂隙）
  row 8         [ BOSS ]               ← Boss（图标隐藏；不走）
```

`Grid = new MapPoint[6, EncounterRows + 3]` = `[6, 9]`。

### 为什么「大战斗层给 `Monster`」就够，不用改波次逻辑

`FrostPactRelic` 是**遗物级**判定 —— 只要玩家带着凛冬契约，**任何战斗**都跑满 16 波：

```csharp
public int SubWavesPlanned => WaveTable.All.Length;                     // 16
public override bool ShouldStopCombatFromEnding() => SubWavesSpawned < SubWavesPlanned;
public override bool ShouldProceedToNextMapPoint() => FrostSpireConfig.LockRoom && SubWavesSpawned < SubWavesPlanned;
```

所以：

- **大战斗层**（`MapPointType.Monster`）→ 进战斗 → 自动 16 波 → 打完才放行 ✓
- **幕层**（商店/火堆/事件/宝箱）→ 不打架 → 不触发波次 ✓

### 两个刻意的设计取舍

- **幕层候选池 4 选 3**：每层必定**只少一种** → 「商店」和「火堆」**至少有一个在场**，
  玩家每层都拿得到补给/回血的选择权（用户明确要「有商店回血火堆啥的」）。
- **幕层刻意不放 `Elite`**：精英是战斗，会跟大战斗一样跑满 16 波，混进「幕」里名不副实。

---

## 四、★ 视觉路径的坑（藏了两轮，这次才彻底查清）

现象：场上建筑显示成**错误占位**（error scene）。日志：

```
[WARN]  Asset not cached: res://scenes/creature_visuals/battleworn_dummy.tscn
[ERROR] InvalidCastException: Unable to cast 'Godot.Node2D' to 'NCreatureVisuals'
```

### 走过的两次弯路

| 版本 | 路径 | 结果 |
|---|---|---|
| 初版 | `creature_visuals/battleworn_dummy` | 场景**存在**（pck 里 3194 字节），但根节点**没挂 `NCreatureVisuals` 脚本** → cast 失败 |
| 二改 | `creature_visuals/big_dummy` | **场景根本不存在**（`BigDummy` 类自己覆写了 `VisualsPath => GetScenePath("creature_visuals/defect")`） |

> ★★ 两条教训：
> 1. **「场景存在」≠「能当生物视觉」** —— 路径对不代表类型对。
>    `MonsterModel.CreateVisuals()` 是
>    `PreloadManager.Cache.GetScene(VisualsPath).Instantiate<NCreatureVisuals>()`，
>    根节点必须挂 `NCreatureVisuals`。
> 2. **「默认路径 = `creature_visuals/<怪物ID小写>`」这条规则只对没覆写 `VisualsPath` 的怪成立**。
>    拿类名直接拼路径会踩空（`BigDummy` 就是反例）。

### 正确做法 + 核查工具

挑场景前先核实「场景存在 + 根节点挂 `NCreatureVisuals`」。本仓库新增两个工具：

```bash
# 1) 列出 pck 里所有 creature_visuals 场景（只读索引，1.9GB 也是秒级）
python tools/list_pck_scenes.py "<游戏>/SlayTheSpire2.pck" creature_visuals/

# 2) 扫全部场景，标注哪些真能当生物视觉用（124 可用 / 3 不可用）
python tools/check_creature_visuals.py "<游戏>/SlayTheSpire2.pck"

# 3) 抠出某个场景看内容
python tools/extract_pck_file.py "<游戏>/SlayTheSpire2.pck" \
    scenes/creature_visuals/guardbot.tscn /tmp/g.tscn
```

**判定依据**：`.tscn` 文本里有 `path="res://src/Core/Nodes/Combat/NCreatureVisuals.cs"`。

核查结果（本体 127 个生物场景）：**124 可用，只有 3 个不可用** ——
`battleworn_dummy.tscn`、`kaiser_crab_boss.tscn`、`kaiser_crab_boss_setup.tscn`。
`battleworn_dummy` 是唯一一个「名字像假人、看着能用、实则不能」的陷阱。

最终选择：

| 建筑 | 视觉路径 | 依据 |
|---|---|---|
| 冰垒 `IceBulwarkM` | `creature_visuals/cubex_construct` | 方块构造体，堡垒感；已核实 ✓ |
| 箭塔 `ArrowTowerM` | `creature_visuals/guardbot` | 守卫机器人，炮台感；已核实 ✓ |

---

## 五、★ 副产品：Godot 4.5 pck v3 格式（实测解开）

写 `list_pck_scenes.py` 时踩到 —— **Godot 4.5.1 的 pck 是 v3，索引区挪到文件尾部**，
和网上常见的 v1/v2 格式（`file_count` 在头部）不一样：

```
头部 (112 字节)：
  +0   uint32 magic = "GDPC"
  +4   uint32 pack_version = 3
  +8   uint32 ver_major / ver_minor / ver_patch   (4.5.1)
  +20  uint32 pack_flags   (bit1 = PACK_REL_FILEBASE → offset 相对 file_base)
  +24  uint64 file_base_offset = 0x70 (112)        ← 数据区起点
  +32  uint64 directory_offset                     ← 索引区起点（★ v3 新增）
  +40  64 字节 reserved

索引区 @directory_offset：
  uint32 file_count
  × file_count:
    uint32 path_len          ← ★ 是**对齐后的**长度，路径尾部可能带 \x00
    char[path_len] path
    uint64 offset            (相对 file_base)
    uint64 size
    byte[16] md5
    uint32 flags
```

> 坑：`path_len` 含 `\x00` 填充 → 解析后必须 `rstrip("\x00")`，
> 否则「明明在列表里却精确查找不到」。

---

## 六、改动清单

| 文件 | 改动 |
|---|---|
| `FrostSpireCode/Relics/FrostPactRelic.cs` | 刷怪不传槽位；删 `PickFreeSlots`/`StandardSlots`；加 `RelayoutEnemies`；加 3 个 using |
| `FrostSpireCode/Monsters/FrostBuildingMonster.cs` | 冰垒视觉 `battleworn_dummy`→`big_dummy`→**`cubex_construct`**（两轮才对）；注释重写为核实方法 |
| `FrostSpireCode/Portal/FrostRiftActMap.cs` | `EncounterRows` 2→6；`BattleRows={1,3,6}`；`InterludePool` 4 选 3 洗牌；新增 `BattleCount`/`InterludeCount` |
| `FrostSpireCode/Portal/FrostRiftSession.cs` | 进裂隙提示文案改为「3 场大战斗、3 个补给幕」 |
| `tools/list_pck_scenes.py` | **新增**：解析 pck v3 索引，列资源路径 |
| `tools/extract_pck_file.py` | **新增**：从 pck 抠出单个文件 |
| `tools/check_creature_visuals.py` | **新增**：扫场景标注「能否当生物视觉」 |

编译：**0 错 0 警**（`dotnet build FrostSpire.csproj`）。

---

## 七、部署状态与待验证

部署过程有个小插曲：游戏（`SlayTheSpire2.exe`）一度开着，mods 目录的 dll 被占用 →
`MSB3027/MSB3021` 复制失败（**注意：这只是部署失败，C# 编译本身是成功的**）。
游戏退出后最后一次 build 已**自动部署成功** —— 两份 dll MD5 一致
（`924acd42da649cb7820742d9bec53b08`，21:24 / 130048 B）。
`FrostSpire.pck` 本轮未动素材，无需重打包。

> ★ 记一条：`-p:CopyModOnBuild=false` **绕不过**这个锁 ——
> RitsuLib 的 `buildTransitive` 目标照样会去清理 `mods/STS2-RitsuLib/`，一样被锁。

用户启动游戏后请确认：

1. 刷第二波怪后**结束回合不再卡死**（本轮核心修复）；
2. 裂隙地图是 **6 层**，且 row 1/3/6 是战斗、row 2/4/5 有**商店/火堆/事件/宝箱**三选一；
3. 冰垒显示**方块构造体**、箭塔显示**守卫机器人**（不再是错误占位）；
4. 每场大战斗跑满 16 波才放行。

---

## 八、遗留

- M3.4 未核项：敌人 AI 是否优先打我方随从。
- 「幕」目前只落成**地图层**，没有切 `state.Act`（背景/遭遇池不变）。
  若用户想要真·换幕（换背景），需要另做 `RunState.Act` 切换 —— 待确认语义。
