# IL2CPP 第六批：路线大修正 —— 根本不需要 Cpp2IL

> 时间：2026-10-12
> 结论先说：**上一批我判断错了。** "剩下的 IL2CPP 游戏都得走 Cpp2IL 出 dummy dll" 这句话是错的。
> 官方 Il2CppDumper v6.7.46 本来就支持 metadata v31，而游戏库里 11 款 IL2CPP 游戏有 **7 款是 v31**。
> 而且 Blue Prince 的 dummy dll **10-10 那天就已经生成好躺在 C:\Temp\bp_dump 里了**，我自己忘了。

---

## 一、metadata 版本全景（11 款全扫）

| 版本 | 游戏 | metadata 大小 | Il2CppDumper v6.7.46 |
|---|---|---|---|
| **31** | 金属之泪 (ToM) | 30.5MB | ✅ 直接通 |
| **31** | 霓虹深渊2 | 24.2MB | ✅ 直接通 |
| **31** | 寄居蟹 | 22.7MB | ✅ 直接通 |
| **31** | Shroom and Gloom | 20.2MB | ✅ 直接通 |
| **31** | 咖啡星球工匠 | 18.3MB | ✅ 直接通 |
| **31** | BALLxPIT | 15.2MB | ✅ 直接通 |
| **31** | Blue Prince | 13.7MB | ✅ 直接通（早就有） |
| 29 | Wildfrost | 13.9MB | ✅ 直接通 |
| **39** | Enter the Chronosphere | 23.2MB | ❌ 不支持 |
| **39** | 东丁王 (HoleIsMine) | 19.9MB | ❌ 不支持 |
| **39** | 推币机 | 19.8MB | ❌ 不支持 |

**8 款能通，只有 3 款（v39）走不了。** 之前那句"要拿数值都得走 Cpp2IL"，是把东丁王的特例当成了通例。

### Cpp2IL 实测结果（别再试了）
- 官方最新 release 停在 **2022.0.7（2022-09-26）**，四年没发新版
- 本机实跑报错：`Unsupported metadata version found! We support 24-29, got 31`
- development 分支据说支持更新的 metadata，但要自己用 .NET 9 编译，且 CI 构建要从作者的 NuGet Feed 拉
- **结论：本机 11 款游戏，Cpp2IL 一款都帮不上。**（v39 那 3 款它也不行，得找 `roytu/Il2CppDumper` 的 v39 fork）

---

## 二、批量 dump 成果（7 款，一次跑完）

```
Il2CppDumper.exe <GameAssembly.dll> <global-metadata.dat> <输出目录>
```
产物：`dump.cs`（全部类定义）/ `DummyDll/`（82 个 dll）/ `script.json` / `il2cpp.h`

| 游戏 | dump.cs 大小 | 类数 |
|---|---|---|
| 金属之泪 | 64.6 MB | 6389 |
| 霓虹深渊2 | 46.7 MB | 8856 |
| 寄居蟹 | 46.8 MB | 6108 |
| 咖啡星球工匠 | 39.7 MB | 4119 |
| BALLxPIT | 31.3 MB | 3714 |
| Shroom and Gloom | 31.8 MB | 4708 |
| Blue Prince（旧） | 26.1 MB | 6720 |

产物在 `C:\Temp\dumps\<游戏名>\`。**这是纯文本，可以直接 grep，不需要任何工具。**

---

## 三、新工具链（已归档到 `tools/il2cpp/`）

| 脚本 | 作用 |
|---|---|
| `build_script_map.py` | ★ 扫 `*_Data` 建 **pathID → 类名** 映射。没有这张表就不知道裸二进制对应哪个类 |
| `parse_with_types.py` | ★★ 核心：用 dump.cs 的字段类型**自己重算 Unity 序列化布局**，解出资产真实数值，带长度自校验 |
| `dumpcs_types.py` | 从 dump.cs 抽「类 → 字段列表」，带命名空间/字段类型频次统计 |

### 用法
```bash
python build_script_map.py "<游戏>_Data" scripts.json
python parse_with_types.py dump.cs "<游戏>_Data" out.json --scripts scripts.json
```

### 自校验机制（关键设计）
解析结束时消耗的字节数**必须等于资产实际长度**（差 ≤3 对齐余量）。
判断错了就丢弃，不会给出错数值。命中率从 4/265 一路提到 277/2295。

---

## 四、踩坑清单（8 个，血泪）

| # | 坑 | 症状 | 修法 |
|---|---|---|---|
| 1 | **嵌套类覆盖本体** | `DOTweenSettings.SafeModeOptions` 被正则截断成 `DOTweenSettings`，本体字段被覆盖，只剩 11 个 bool | 类名正则改成 `([\w\.]+)`，点号一起抓 |
| 2 | **ScriptableObject 的 m_Script 在 m_Name 之后** | 少跳 12 字节，所有 SO 解不出来 | 加变体 C：`body_off = align4(0x20+len) + 12` |
| 3 | **PPtr 读错位置** | 读到 fileID 当 pathID | PPtr = int32 fileID @base + **int64 pathID @base+4** |
| 4 | **enum 当空结构** | `not info['fields']` → 抛 EOFError | 识别 enum，按 int32 读 |
| 5 | **[SerializeField] 私有字段拿不到** | DOTweenSettings 只解出 12/148 字节 | ★ dump.cs 里 `[SerializeField]` 是**独立一行**，解析它 → 命中率 4→22 |
| 6 | **字段元组变 3 元** | `too many values to unpack` | 加 `flds2()` 转回 2 元 |
| 7 | **MonoScript 不在业务资产里** | 265 个 MonoBehaviour 只映射到 1 个类 | MonoScript 集中在 `globalgamemanagers.assets`（Blue Prince 4521 个），必须先建全局映射 |
| 8 | **大文件被跳过** | 霓虹深渊2 主数值在 202MB 的 resources.assets，被 `--maxfile 120` 跳过 | 单独指定大文件跑 |

### 三种头部布局（必须都试，靠自校验挑）
```
MonoBehaviour（挂 GameObject）:
  0x00 PPtr<GameObject>(12) | 0x0C m_Enabled(4) | 0x10 PPtr<MonoScript>(12) | 0x1C m_Name | 字段区

ScriptableObject（Unity 2019.3+）:
  0x00 int hideFlags | 0x04 PPtr m_CorrespondingSourceObject(12)
  0x10 PPtr m_PrefabInstance(12) | 0x1C m_Name | PPtr<MonoScript>(12) | 字段区
      ★ m_Name 也在 0x1C，但 m_Script 在它**后面**
```
两者 m_Name 都在 0x1C，极易混淆。

---

## 五、霓虹深渊2：架构拆解（2932 个 NEON 类）

### 命名空间分布
| 命名空间 | 类数 | 是什么 |
|---|---|---|
| NEON.Game.PowerUps | 675 | **道具系统**（最肥） |
| NEON.Game | 391 | 游戏主逻辑 |
| NEON.Behaviors | 315 | 行为组件 |
| NEON.UI | 206 | UI |
| NEON.Game.PowerUps.Skills | 96 | 技能 |
| NEON.Game.Actors / Bullets | 49 / 39 | 角色 / 子弹 |
| NEON.Game.BeliefSystem | 37 | 信仰系统 |
| NEON.Game.ExtractionMode | 29 | 撤离模式 |

### 道具继承体系（roguelike 教科书式）
```
PowerUp
 ├─ StatBoost                    ← 纯属性加成
 │   └─ LuckStatBoost            ← 带触发/幸运判定的加成
 │       ├─ BaseSpriteDeath      ← 精灵死亡时触发
 │       ├─ OnGetRecoverItemPowerUp ← 拾取回复道具时
 │       └─ ...（几百个具体道具）
 ├─ FatePerkBase                 ← 命运 perk
 ├─ GunPartsPowerUp              ← 枪械部件
 ├─ ThunderPowerUp               ← 雷击
 └─ OnMinionPowerUp              ← 召唤物相关
```
**每个道具 = 一个继承特定触发时机基类的 C# 类。** 触发时机由继承表达，不需要配置表。

### StatBoost 关键字段
```csharp
public PlayerStateStringFloat values;        // 属性加成表（字符串键 → float）
public string tagDamageMult;                 // 标签伤害倍率
public Vector2 tagDamageMultRange;           // ★ 倍率是个区间，不是定值
public float gunpointOffset, gunpointOffsetY;
public bool needShowOverheadIcon;
private List<WeaponTagDamageModifier> weaponDamageModifiers;
```

### LuckStatBoost 关键字段（★ 最值得抄）
```csharp
public string luckyFormula;              // ★ 幸运公式用字符串配置，可热改
public int luckDeviateMin, luckDeviateMax;   // 偏差区间
public float CD;                          // 冷却
// ↓ 战斗内是**另一套完整参数**
public string luckyFormulaInBattle;
public int luckDeviateMinInBattle, luckDeviateMaxInBattle;
public float CDInBattle;
public StatBoostLuckLevel luckLevel;
public float newCd;
public float[] newCdList;                 // ★ 分级冷却（按等级给不同 CD）
public BadgeBoostBase[] badgeBoost;       // 徽章加成（可叠加）
```

**可抄手法：**
1. **战斗外 / 战斗内两套独立参数** —— 同一个道具在探索期和战斗期数值分开配，避免"探索期无敌"或"战斗期没存在感"
2. **公式写进字符串字段**（`luckyFormula`）而不是硬编码 —— 策划改数值不用改代码
3. **数值是区间不是定值**（`tagDamageMultRange`、`luckDeviateMin/Max`）—— 每次触发在范围内取，制造波动感
4. **分级冷却数组**（`newCdList`）—— 按道具等级索引取 CD，比公式直观

### 实测数值样本（resources.assets 解出）
```
MinionsHeartsByHitCount（召唤物按受击次数扣心）
  curHearts=6  maxHearts=6  delayDestroyTime=1.0
  limitHitCount=1  factions=1  typeOfDamageAllowed=3/11  deductCd=2.0

OrbitalMinionConfig（环绕召唤物）
  radius=4.0  rotationSpeed=120.0  intervalDistance=0.0  rotationOffset=0.0

TreeEntry（解锁树节点，36 个）
  id / page / unlockContentId / position / sortY
```

---

## 六、金属之泪 (ToM)：架构拆解（6389 类）

| 模块 | 类数 | 说明 |
|---|---|---|
| Unity.Entities | 605 | **用 DOTS ECS 架构** |
| Zaia.UI | 243 | 自家 UI |
| Zaia.AI | 229 | AI 状态机（ShootingState / SkirmishingState / TauntState...） |
| Mirror | 195 | 网络同步（联机） |
| Pathfinding | 193 | A* Pathfinding Project |
| MxM | 144 | Motion Matching 动画 |
| Zaia.Economy | 8 | 商店：Shop / ShopInventory / ShopItemButton / InventoryTab |

技术栈：DOTS ECS + Mirror 联机 + A* 寻路 + Motion Matching + Wwise 音频 + EOS（Epic 联机服务）。

### UpgradeData（升级项，通用性很强）
```csharp
public string UpgradeID;
public UpgradePersistence Persistence;   // 持久化方式
public bool CanStack;                    // 能否叠加
public UpgradeFamily Family;             // 家族（用于协同/synergy）
public string FamilyString;
public UpgradeFamily SynergyFamily;      // ★ 与哪个家族产生协同
public int Tier;                         // 品阶
public string RunStatModifier;           // ★ 本局属性修正（字符串）
public float BaseDropOdds;               // ★ 基础掉落概率
public string RequiredCharacter;         // 角色限定
public List<string> Prerequisites;       // ★ 前置依赖（解锁链）
public List<string> Tags;
public int CurrentLevel;
public string NameLocKey, DescLocKey;    // 本地化
public int UpgradeCost;
public StackInfo UpgradeStackInfo;       // ★ 叠加信息单独成类
public const int MAX_EMBLEM_LEVEL = 4;   // 徽章上限硬编码 4
```

### ShopInventory
```csharp
public List<CurrencyType> ShopCurrencyType;  // ★ 一个商店可支持多种货币
private List<ShopItemData> _allItemData;
private List<ShopItemButton> _allItems;
```

**可抄手法：**
1. **升级项带前置依赖链**（`Prerequisites`）—— 做成 DAG，能做出"先解锁 A 才能出 B"的深度构筑
2. **协同用 Family 字段表达**（`Family` + `SynergyFamily`）—— 不用为每对组合写死
3. **掉落概率是 UpgradeData 的一个字段，不是独立表** —— 数值跟着内容走
4. **上限写成 const**（`MAX_EMBLEM_LEVEL = 4`）—— 硬上限，防止无限叠

---

## 六之二、咖啡星球工匠 (Astro Prospector)：537 个对象 / 2120 个字段值

命名空间：`AstroProspector.Runtime.*`（之前猜它和丁东王"同模板"是错的，那是资产组织方式，不是命名空间）

### ★ 意外发现：FG_ / PR_ 三套配置又出现了
```
ShipsData（飞船全局配置，2 个实例）
  'FG_Global_ShipsData'  _economyCondition=0.75  _dodgeCondition=0.25
                         _laserCondition=7  _flowCondition=20  _lifeCondition=14  _waveCondition=7
  'PR_Global_ShipsData'  全部 = 0
```
**这个 `FG_` / `PR_` 前缀跟丁东王（FlyingRocks）里的 `FG_/PR_/PT_` 三套并行配置是同一套命名习惯。**
→ 两款游戏很可能是同一家团队 / 同一套资源工作流出来的。丁东王那批的 `FG_`（正式）/ `PR_`（压测?）/ `PT_`（?），
这里 `FG_` 有真数值、`PR_` 全 0，符合"一套默认值 + 一套清零的测试值"的形态。
（上批我猜咖啡星球工匠和丁东王"同模板"被推翻了 —— 那是资产字节布局，这次是**命名习惯层面的关联**，两回事。）

### 敌人 AI 配置（同一组件多套数值 = 敌人分级）
```
SingleShooterBrain（6 个实例，逐个不同）
  speed=4.0   _rotationSpeed=15.0  _angleSpread=45.0  _attackCooldown=1.8
  _numberOfAttacksInPosition=2  _bulletLifeTime=4.5  _bulletsPerShoot=3  _timeBetweenBullets=0.65
  ---
  speed=3.0   _rotationSpeed=12.0  _angleSpread=20.0  _attackCooldown=0.5
  _bulletsPerShoot=1  _bulletLifeTime=1.75  _timeBetweenBullets=0.6
  ---
  speed=4.0   _angleSpread=15.0  _attackCooldown=5.0  _numberOfAttacksInPosition=3
  _bulletLifeTime=0.8  _bulletsPerShoot=1  _timeBetweenBullets=0.3

AsteroidBrain（8 个）
  speed=2.0  _overrideSpeed=True  _slowDownTime=0.5  _initialSprint=True  _initialSprintTime=2.0

ContactDamage（22 个）  Damage = 2 / 4 / 5 ...
```
**可抄手法：敌人的手感参数是"射速/弹数/散布/弹速/冷却"五个旋钮分开配**，
同一个 Brain 组件挂不同数值就是不同敌人 —— 不加新代码只加配置，敌人种类能做很多。

---

## 七、Blue Prince 的真相：数值在 PlayMaker 里

- level2 有 **34756 个 MonoBehaviour，其中 6982 个是 `PlayMakerFSM`**
- 前面的类名 TOP：TextMeshPro 16619 / PlayMakerFSM 6982 / MJCornerButtonHandler 2304
- 也就是说 **Blue Prince 的游戏逻辑不是 C# 脚本，是 PlayMaker 可视化状态机**
- 数值存在 FSM 的变量里：
```
PlayMakerFSM
 └ [SerializeField] private Fsm fsm
     ├ string name / startState
     ├ FsmState[] states / FsmEvent[] events / FsmTransition[] globalTransitions
     └ FsmVariables variables
         ├ FsmFloat[]  floatVariables   → NamedVariable{ useVariable, name, tooltip, ... } + float value
         ├ FsmInt[]    intVariables
         ├ FsmString[] stringVariables
         └ FsmBool / FsmVector2/3 / FsmColor / FsmGameObject / FsmArray / FsmEnum ...
```
- 目前 PlayMakerFSM 解析 **0/6982 命中**（FsmState → ActionData → FsmStateAction 多态链没打通）
- ★ 这是下一批要啃的。打通了能一次拿到 6982 个状态机的变量名+数值 = Blue Prince 全部数值

---

## 八、方法论：IL2CPP 数值提取决策树（更新版）

```
拿到一个 Unity 游戏
 ├─ 有 Managed/Assembly-CSharp.dll ?  → Mono，直接 ilspycmd，完整 C# 源码
 ├─ 有 .pck ?                          → Godot，gdre_tools --recover
 ├─ 有 data.win ?                      → GameMaker
 └─ IL2CPP（有 GameAssembly.dll）
     ├─ ① 读 global-metadata.dat 前 8 字节看版本
     │     magic = AF 1B B1 FA，version = int32 @offset 4
     │
     ├─ v16 ~ v31  → ② Il2CppDumper v6.7.46，出 dump.cs + DummyDll
     │                 ③ build_script_map.py 建 pathID→类名
     │                 ④ parse_with_types.py 解资产（自校验）
     │
     ├─ v39        → 需要 roytu/Il2CppDumper 的 v39 fork（本机 3 款：东丁王/推币机/Enter the Chronosphere）
     │
     └─ 任何版本都成立的兜底（东丁王就是这么拆出来的）：
          ① 扫 global-metadata.dat 的 ASCII 串 → 命名空间/类名/字段名
          ② scan_raw_fields.py 字节级扫 [int32长度][字段名][对齐][double]
             （只对"把字段名写进资产"的特殊设计有效，如丁东王）
```

**注意：** 命名空间命名习惯一样 ≠ 数据组织方式一样。上一批我猜"咖啡星球工匠和丁东王同模板"，实测 0 个值，推翻了。**只能实测，不能靠猜。**

---

## 九、待办

- [ ] **PlayMakerFSM 解析打通** —— 啃 FsmState → ActionData → FsmStateAction 多态链，拿 Blue Prince 6982 个状态机的数值
- [x] ~~补 BALLxPIT / 寄居蟹 的 scripts.json~~ → 批量跑中断，**这两款还没建 MonoScript 映射，跑时会报 FileNotFoundError**。补法：先跑 `build_script_map.py` 再跑 `parse_with_types.py`
- [x] 咖啡星球工匠 assets 解析 ✅ 537 对象 / 2120 字段值（含 FG_/PR_ 双配置 + 敌人 AI 数值）
- [ ] 霓虹深渊2 的 `NEON.Game.PowerUps` 675 个道具类字段全量提取（现在只看了 StatBoost/LuckStatBoost）
- [ ] 金属之泪的 Zaia.AI 229 个状态机（有数值价值）
- [ ] v39 三款（东丁王/推币机/Enter the Chronosphere）：试 `roytu/Il2CppDumper` v39 fork，用 dotnet 9 本地编译
- [ ] `C:\Temp\dumps` 共 259MB dump.cs + `C:\Temp\parsed` 结果，需要挪进仓库或清理（Temp 会被系统清掉）

---

## 十、可抄手法汇总（跨 13 款游戏累计）

| 手法 | 出处 | 怎么用 |
|---|---|---|
| 战斗外/战斗内两套参数 | 霓虹深渊2 LuckStatBoost | 同一个 buff 分场景配数值 |
| 公式存字符串字段 | 霓虹深渊2 `luckyFormula` | 策划改数值不改代码 |
| 数值是区间不是定值 | 霓虹深渊2 `tagDamageMultRange` | 制造波动，避免死板 |
| 分级数组索引取值 | 霓虹深渊2 `newCdList` | 比公式直观，好调 |
| 升级项前置依赖链 | 金属之泪 `Prerequisites` | 构筑深度 |
| 协同用 Family 字段 | 金属之泪 `SynergyFamily` | 不用写死每对组合 |
| 上限写成 const | 金属之泪 `MAX_EMBLEM_LEVEL=4` | 硬上限防叠爆 |
| 触发时机用继承表达 | 霓虹深渊2 PowerUp 体系 | 不用配置表 |
| 多货币 | 金属之泪 `List<CurrencyType>` | 商店支持多币种 |
| 概率与调整量解耦 | Blue Prince MajorBump/MinorBump | 改概率不动基础值 |
