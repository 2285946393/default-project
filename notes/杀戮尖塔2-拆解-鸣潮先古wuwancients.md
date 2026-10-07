# 拆解 · 鸣潮先古（wuwancients）

> **工坊** `3747583646` · **作者** supermanpower · **版本** 0.92.9 · **依赖** `BaseLib ≥ v3.2.0`
> **最低游戏版本** 0.111.0 · **形态**：dll（730KB）+ pck（**111MB**）
>
> **一句话**：把《鸣潮》的 **9 个角色**做成《杀戮尖塔 2》里的**"先古之民"（Ancient）**——
> 每个角色一个先古事件，自带**专属遗物池**，轮到谁出现是**确定性**的。
>
> **取证方式**：`ilspycmd 8.2.0` 反编译 → `projects/ref-wuwancients/decompiled/wuwancients.decompiled.cs`
> （22,957 行 / 337 个类型）。素材清单来自 pck 目录解析。所有结论带行号。

---

## 〇、先说"先古"是什么

《杀戮尖塔 2》里每幕起点有 **先古之民（Ancient）** 房间 —— 一个 NPC 给你三选一的强力遗物。
原版每幕对应一位（涅奥 / 佩尔 / 瓦库 那些）。中文本地化就叫"**先古**"
（见 `projects/meme-spire/.../map.json` 的 `LEGEND_ANCIENT.title = "先古"`、
`RELIC2_EPOCH.title = "先古之民"`）。

这个 mod 干的事：**往先古池子里塞 9 个鸣潮角色**，每人一个先古事件 + 一堆专属遗物。

---

## 一、9 个角色（`wuwancients.Ancients` 命名空间）

| 类名 | 角色 | 说明 |
|---|---|---|
| `Augusta` | 奥古斯塔 | 第 3 幕（`IsValidForAct` → `ActNumber(act) == 3`） |
| `Chisa` | 千咲 | |
| `Febe` | 菲比 | |
| `Froro` | 弗洛洛 | 有 `froro` + `froro_b` 两个场景 |
| `Galbrena` | 嘉贝莉娜 | 有 `galbrena` + `galbrena_b` |
| `Linna` | 琳奈 | |
| `Shorekeeper` | 守岸人 | |
| `Youno` | 尤诺 | 有 `youno` + `youno_b` |
| `Zani` | 赞妮 | 有 `zani` + `zani_b` |

外加两个调度类：`AncientPool`（谁出现）· `AncientScenePicker`（用哪个场景）。

---

## 二、核心机制：怎么把角色做成"先古"

每个角色都是 **`CustomAncientModel` 的子类**（`CustomAncientModel` 是 **BaseLib** 提供的基类）。

以 `Augusta`（第 21619 行）为例，要覆写这几样：

```csharp
public override Color ButtonColor        => new Color(0.85f, 0.62f, 0.08f, 0.7f);   // 按钮色
public override Color DialogueColor      => new Color(0.35f, 0.05f, 0.05f, 1f);    // 对白色
public override string? CustomMapIconPath          => "res://wuwancients/images/icons/augusta_map.png";
public override string? CustomMapIconOutlinePath   => "res://wuwancients/images/icons/augusta_map_outline.png";
public override string? CustomRunHistoryIconPath   => "res://wuwancients/images/icons/augusta_map_icon.png";
public override string? CustomScenePath => AncientScenePicker.Pick(
        Id.Entry, WuwancientsConfig.奥古斯塔场景, "res://wuwancients/scenes/augusta.tscn");

protected override OptionPools MakeOptionPools { … }   // 三个选项池
public override IEnumerable<EventOption> AllPossibleOptions => …  // 一共 10 个可选遗物
public override bool IsValidForAct(ActModel act)      => ActNumber(act) == 3;
public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient) { … }
```

### 2.1 遗物怎么办：10 选 3

```csharp
protected override OptionPools MakeOptionPools {
    List<AncientOption> list = new();
    AddIfAllowed<SunAndGriffinSeal>(list);   // 太阳与狮鹫印记
    AddIfAllowed<SundayCrown>(list);         // 星期日冠冕
    AddIfAllowed<ResidualFrequencyOfBlackTide>(list);
    AddIfAllowed<AuthorityOfThunderAndCrown>(list);
    AddIfAllowed<SmeltedFragment>(list);
    AddIfAllowed<GlowingMarigold>(list);
    AddIfAllowed<SmallAcorn>(list);
    AddIfAllowed<SweetLeafSplitBread>(list);
    AddIfAllowed<OldHairband>(list);
    AddIfAllowed<AugustaDumpling>(list);     // 奥古斯塔的饺子（！）
    var picked = AncientPool.ShuffleDeterministic(list, Id.Entry)
                            .Where(o => o != null).Take(3).ToArray();
    return new OptionPools(MakePool(picked[0]), MakePool(picked[1]), MakePool(picked[2]));
}
```

→ **每个角色自带 10 个专属遗物，每次抽 3 个**。`AllPossibleOptions` 里再声明一遍这 10 个
（给图鉴/调试用）。

数量级：`wuwancients.Relics` 命名空间 **153 个类**（含补丁类），`Cards` 45、`Powers` 41、
`Events` 21、`Enchantments` 14、`Potions` 3、`Keywords` 1。

---

## 三、★ 确定性随机（这份代码最值得抄的东西）

它**不碰游戏的随机数流**，而是自己用"**种子 + 盐 + 索引**"算。
好处：**存读档一致、可复现、不会打乱后续随机**（刷不了随机数）。

### 3.1 谁出现：`AncientPool.IsSelected`（第 21472 行）

```csharp
private const ulong SaltAncientWinner = 2654446023uL;
```

流程：

1. **去重**：扫描本局**其它幕**已经出现过的先古类型（`appearedTypes`），从候选里**剔除**
   → 保证**一个角色一局只出现一次**。（第 21480–21506 行）
2. 如果有人被设为"强制出现"（权重 100），直接选它（第 21518 行）。
3. 否则：
   ```csharp
   num2 = 解锁的原版先古数量;
   num3 = 候选数 + num2;                      // 总池 = 自己的 + 原版的
   num4 = SyncedRng.DeterministicIndex(runState, 2654446023u + actNumber, num3);
   if (num4 < 候选数) type = 候选[num4].Key;   // 落在自己这边才算中，否则让给原版
   ```
4. 日志把整个决策过程打出来（第 21534 行）：
   ```
   [wuwancients] ancient roll act=… asker=… candidates=[…] deduped=[…] original=… total=… roll=… winner=…
   ```
   —— **调试时看这一行就知道为什么是它**。

### 3.2 确定性洗牌 / 掷硬币

```csharp
internal static List<T> ShuffleDeterministic<T>(IEnumerable<T> items, string saltKey)
{
    ulong seed = runState?.Rng.Seed ?? 0;
    ulong salt = StringHelper.GetDeterministicHashCode(saltKey);
    return items.Select((item, index) => (Mix(seed, salt, index), item))
                .OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
}

internal static bool CoinFlip(string saltKey) => (Mix(seed, hash, 0) & 1) == 0;

// splitmix64 风格的混合函数（第 21561 行）
private static ulong Mix(ulong seed, ulong salt, ulong index) {
    ulong n = seed + (salt ^ (index * -7046029254386353131L)) * -4658895280553007687L;
    n = (n ^ (n >> 30)) * 13787848793156543929uL;
    n = (n ^ (n >> 27)) * 10723151780598845931uL;
    return n ^ (n >> 31);
}
```

**要点**：同一个 `saltKey` → 同一个结果；不同角色用不同 salt → 各自独立。
**这就是"往固定随机流里塞新内容但不能扰动它"的标准解法**。

### 3.3 场景选择：`AncientScenePicker`（第 21569 行）

同一个角色可以有多个立绘场景（如弗洛洛 `froro` / `froro_b`）。
配置里选"场景1 / 场景2 / 随机出现"，随机时也是用 `entry` 当盐做确定性取模。

---

## 四、配置（`wuwancients.Config`）

```csharp
enum AncientSceneMode       { 场景1, 场景2, 随机出现 }
enum AncientSingleSceneMode { 场景1, 随机出现 }
enum AncientAppearMode      { 关闭, 开启, 强制出现 }
enum EventAppearMode        { 关闭, 随机 }
```

`WuwancientsConfig`（第 653 行）里每个角色两个开关：
`XX是否出现`（正常权重 1）和 `强制XX出现`（权重 100）。另有：

- `禁用原版先古` —— 第 21508 行：如果开了它且自己的候选被去重清空了，就把候选重置回全部
  （保证不会"谁都出不来"）。
- 每个角色的场景选择项（`奥古斯塔场景` / `千咲场景` …）。

⚠️ 注意：**用中文当 C# 标识符**（`禁用原版先古`、`强制千咲出现`）—— 作者是中文环境，能编过，
但如果我们抄结构，建议还是用英文，避免编码/工具链踩坑。

---

## 五、素材结构（111MB 的 pck 里是什么）

pck 共 **1,331** 条目：

| 类型 | 数量 | 说明 |
|---|---|---|
| `.ctex` 贴图 | **334** | 角色立绘、遗物图标、地图图标 |
| `.sample` 音频 | **136** | ★ **语音**（`Zani_1.wav` / `augusta1.wav` / `bi_an_1.wav` …） |
| `.scn` 场景 | **22** | 15 个具名场景（含 `_b` 变体） |
| `.res` 资源 | 4 | `BeamGradient` / `light` / `flash` / `sunshine`（光效） |
| 其它 | ~834 | `.godot/exported` 导出缓存等 |

具名场景：`augusta` `chisa` `febe` `froro` `froro_b` `galbrena` `galbrena_b` `lina`
`monastic_pet` `shorekeeper` `suisui_mystery_shop` `youno` `youno_b` `zani` `zani_b`

**路径约定**（`res://` 下）：`wuwancients/images/icons/*` · `wuwancients/scenes/*`。
⚠️ 这游戏的 mod 是**纯 dll + pck**，pck 里的 `.tscn` 一般被导出成二进制 `.scn` + `.remap`
（见特效实战 ② 第二节）。

---

## 六、工程细节（值得学的几个点）

| 机制 | 位置 | 作用 |
|---|---|---|
| `BlockedRelicTypes` | 21623 / 21710 | **当玩家是 mod 角色时，屏蔽某些原版遗物**（避免机制打架）。比如 `HiddenSeaRecord` |
| `IsModPlayer()` | 21677 | 用 `VanillaIds = {IRONCLAD, SILENT, DEFECT, REGENT, NECROBINDER}` 反查：不在里面 = mod 角色 |
| `BgmDucker` | 1028 | 放语音时**把 BGM 压低**（`Acquire`/`Release` 引用计数） |
| `SuisuiShopAudio` / `SuisuiShopVanillaVoicePatch` | 3052 / 3075 | 苏苏神秘商店：**替换原版语音** |
| `OverlimitKillCredit` / `OverlimitUnlocks` | 1077 / 2771 | 自研"超限"机制 |
| `DemonForce` 系列 | 2743 / 2695 起 | 自研"恶鬼之力"机制（含 `DemonForceOwner` / `DemonForceId`） |
| `EnchantUtil` | 1180 | 附魔工具 |
| 26 处 Harmony/ModPatch | 全文 | 遗物大量带**自己的补丁**（如 `NoodlesKeepOptionPatch`） |

---

## 七、我们能抄什么

| 能抄的 | 为什么值 |
|---|---|
| **`CustomAncientModel` 做自定义先古** | 想往先古池塞自己的 NPC，这是 BaseLib 给的标准口子 |
| **种子+盐+索引 的确定性随机**（splitmix64 `Mix`） | ★ 想"加内容但不扰动游戏随机流"，这是标准解法；存读档一致、可复现 |
| **先古去重**（一局每人只出一次） | 多个自定义先古共存时的必做项，否则会撞车 |
| **决策日志**（`ancient roll act=… winner=…`） | 排查"为什么是它出现"时，一行顶一小时 |
| **权重 100 = 强制** 的写法 | 很省事：既能"正常随机"又能"我要测试它" |
| **`BlockedRelicTypes` 联动保护** | 自定义内容与原版冲突时的优雅处理 |
| **`BgmDucker` 引用计数压低 BGM** | 加语音 mod 必备的手感细节 |

---

## 八、和另外几个"先古"系 mod 的关系（顺带发现）

本机工坊里还有一串同题材的，可以对比着看：

| 工坊 id | 名字 | 备注 |
|---|---|---|
| `3747583646` | **wuwancients** | 本文档，鸣潮先古 |
| `3747819202` | `HadesAncients` | 哈迪斯（Hades）先古 |
| `3751266563` | `Trans_HadesAncients` | 上面那个的汉化 |
| `3748235030` | `AncientWaifus.dll` | 先古娘化 |
| `3750930021` | `AncientAffection.dll` | 先古好感度？ |
| `3747531952` | `RandomForeseer` | 随机数预测（见 CombatSolver 拆解） |

→ **"把外部 IP 做成先古"已经是一条成熟玩法线**，`CustomAncientModel` 是共同的技术底座。

---

*拆解产物：`projects/ref-wuwancients/`（`decompiled/` 22,957 行 + 原包 manifest）*
