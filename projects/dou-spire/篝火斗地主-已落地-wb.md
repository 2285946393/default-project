# 斗地主尖塔 · 篝火打牌（已落地 · wb 2026-10-07 深夜）

> 用户原话：「在游戏中的篝火处选项弹出可以斗地主。（单人和 ai）… 用身上的游戏金币数量作为赌注。  
> 可以通过篝火处的斗地主赚钱。不限次数。不然只能通过这个斗地主游戏玩到斗地主太纯了」
>
> **一句话**：休息处（篝火）多了一个「斗地主」选项，点开就是那张牌桌，筹码直接算成玩家身上的金币，  
> 而且**不消耗这次休息** —— 想玩几把玩几把。
>
> ⚠️ **全部未实机验证**（按用户定的规矩：测试他自己来）。编译 **0 错 0 警**、dll 已部署。

---

## 一、改了哪些文件

| 文件                                       | 改了什么                                                                                                                          |
| ---------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `DouSpireCode/Table/DouSpireRestSite.cs` | **新建**。篝火选项 `DouSpireRestSiteOption` + 注册单例 `DouSpireRestSiteSingleton`                                                       |
| `DouSpireCode/Entry.cs`                  | `Initialize()` 里加 `RegisterContentPack()`：把单例塞进 RitsuLib 内容包                                                                  |
| `DouSpireCode/Table/DouSpireTable.cs`    | ① 加静态字段 `_stakePlayer`；② `Open()` 开头清空赌注模式；③ 新增 `OpenAtRestSite(player, logger)`；④ `PayOut()` 末尾调 `SettleGold(myDelta)` 结算真金币 |

**没动**：牌桌玩法、UI、素材、语音、工坊相关的一切。F9 / 主菜单入口行为**完全不变**（恒为无赌注）。

---

## 二、关键 API（都是反编译现查的，下次别再从零试）

### 2.1 往篝火加选项的**官方口子**

```csharp
// MegaCrit.Sts2.Core.Models.AbstractModel
public virtual bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
```

- 原版 `Shovel`（铲子）遗物就是靠它加「挖掘」：`options.Add(new DigRestSiteOption(player)); return true;`
- 调用链：`RestSiteOption.Generate(player)` → `Hook.ModifyRestSiteOptions(...)` → 各模型覆写。

### 2.2 RitsuLib 的两个脚手架

- **`STS2RitsuLib.Models.HookedSingletonModel`** —— 会订阅钩子的单例模型。  
  `base(HookType.Run)` 就订阅 run 级钩子（`HookType` 是**嵌套枚举**：None=0 / Combat=1 / Run=2）。  
  这样不用写 relic、不用手搓 Harmony 补丁。
- **`STS2RitsuLib.Scaffolding.Content.ModRestSiteOptionTemplate`** —— 篝火选项基类。  
  `RestSiteOption.Title` 和 `.Icon` 是**非 virtual** 的，RitsuLib 用 Harmony 补丁把它们改成可覆盖，  
  于是能覆写 `CustomTitle` / `CustomIconPath`。**不继承它就没法改标题。**

### 2.3 注册（在 `Entry.Initialize()`）

```csharp
RitsuLibFramework.CreateContentPack(ModId)
    .Singleton<DouSpireRestSiteSingleton>()
    .Apply();
```

### 2.4 `OnSelect()` 返回 `false` 的**确切语义**（这是"不限次数"的关键）

`MegaCrit.Sts2.Core.Multiplayer.Game.RestSiteSynchronizer.ChooseOption()`：

```csharp
bool flag = await option.OnSelect();
if (!flag) return false;          // ← 提前返回：选项**不移除**、篝火**不结束**
...
restSite.options.RemoveAt(optionIndex);   // 成功才消耗掉这个选项
if (restSite.options.Count == 0) restSite.completionTaskSource.SetResult();  // 选项用完 → 篝火结束
```

⇒ 返回 `false` = **选项留着、篝火留着**，可以反复点。星见雅那个抽卡选项（`ChoukaRestSiteOption`）  
也是返回 `false`，同一个套路。

### 2.5 标题/说明的中文怎么来

`RestSiteOption.Title` 固定取本地化表 `rest_site_ui` 的 `OPTION_<OptionId>.name`；  
本 mod 没有 pck、没有本地化文件，所以**运行时往那张表里合并两条键值**（都是公开 API）：

```csharp
LocManager.Instance.GetTable("rest_site_ui").MergeWith(new Dictionary<string, string> {
    ["OPTION_DOU_SPIRE.name"] = "斗地主",
    ["OPTION_DOU_SPIRE.description"] = "…",
});
```

- `LocManager.GetTable(name)` 找不到表会**抛 LocException** —— 必须 try/catch。
- `LocTable.GetRawText(key)` 找不到键也会抛，所以**不注入就一定崩**，不是可选项。

### 2.6 图标怎么塞（本 mod `has_pck: false` 的坑）

`RestSiteOption.Icon` 只会去 `res://` 找图，而我们**不能往 res:// 放自己的图**。  
解法：把磁盘上的图**塞进游戏的资源缓存**：

```csharp
PreloadManager.Cache.SetAsset("res://images/ui/rest_site/option_dou_spire.png",
                              DouSpireAssets.One("table/crown.png"));
```

`AssetCache.SetAsset(string, Resource)` 是**公开**的。之后游戏按那个 res:// 路径取图就直接命中缓存。  
每次生成选项都重塞一遍（休息处进房会做资源 diff/卸载）。

### 2.7 金币

- `Player.Gold` 是 `public int { get; set; }`，setter 会触发 `GoldChanged`。
- `PlayerCmd.GainGold(decimal, Player, bool)` / `LoseGold(decimal, Player, GoldLossType)` / `SetGold` 都是 **async Task**。
- **本实现故意不走 PlayerCmd**：`GainGold` 里要过 `Hook.ModifyGoldGained(runState, Creature.CombatState, …)`，  
  篝火处没有战斗、`CombatState` 可能是 null，容易在钩子里炸。直接改 `Player.Gold` 并钳到 0。  
  代价：绕过"加钱加成"类遗物、不进本局金币统计。彩蛋玩法可接受。

---

## 三、怎么实现的（三句话）

1. **`DouSpireRestSiteSingleton`**（`HookedSingletonModel`）覆写 `TryModifyRestSiteOptions`，  
   每次生成休息处选项时往里 `Add` 一个 `DouSpireRestSiteOption(player)`。
2. 点这个选项 → `OnSelect()` 调 `DouSpireTable.OpenAtRestSite(Owner, …)` 开桌，然后**返回 `false`**（篝火留着）。
3. 开桌时记下 `_stakePlayer`；`PayOut()` 里把**我这一席的输赢**按 1 筹码 = 1 金币落到 `player.Gold` 上。

赌注沿用现有的 `DouSpireConfig.BaseStake`（默认 100）× 本局倍数（明牌/加倍/炸弹会乘上去）。  
输了不会变负数（钳到 0）。所以倍数一高就能一次赚很多 —— **这正是用户要的"赚钱"，但也是最大的平衡风险**。

---

## 四、⚠️ 实机验收清单（游戏**必须重启**，改了 dll 光收开桌不够）

| # | 看什么              | 期望                                                        |
| - | ---------------- | --------------------------------------------------------- |
| 1 | 进任意一层休息处         | 选项里多一&#x4E2A;**「斗地主」**（带皇冠图标），说明文字是中文，不是空/不是报错            |
| 2 | **牌桌盖不盖得住休息处界面** | 点开后整屏是牌桌。**如果牌桌被休息处挡住/只露一半**，就是层级问题，回我                    |
| 3 | 玩完一局             | 看顶部金币数有没有变；日志（`mods\DouSpire\voice-log.txt`）里应有「篝火金币结算 …」 |
| 4 | 玩完这一局，篝火还在不在     | **应该还在**，可以再点「斗地主」继续玩（不限次数）                               |
| 5 | 篝火的其他选项（治疗/锻造）   | 还能正常用 —— 我们没消耗这次休息                                        |
| 6 | 关掉牌桌后按 F9        | 应该是**无赌注**的普通桌（金币不变）                                      |

**排查用**：`mods\DouSpire\voice-log.txt`（我们自己的诊断日志）和  
`%APPDATA%\SlayTheSpire2\logs\godot.log`（游戏日志，`Entry.Logger` 写的都在里面）。

---

## 五、已知限制 / 没做的

1. ~~**多人联机没做**。~~ → **2026-10-08 已做**，见 **`斗地主尖塔-联机同坐一桌-已落地-wb.md`**。  
   结论被用户推翻了：「你得做多人。…而篝火多人代表玩家你已经在联机了。你只需要如何让主机客机坐在一起打牌」。  
   现在的做法：**复用 run 里那条已经在跑的连接**（RitsuLib Sidecar 总线），  
   主机 = run 主机，客机点「斗地主」→ 请求主机开桌 → 主机广播座位表 + 牌种子 →  
   两端各自 `GameState.ViewFrom(MySeat)` 旋转成"我坐 0 号位"。**不造房间、不开官方联机界面。**  
   ⚠️ 同样**没实机验证过**（用户"没人陪我测试"，但明确要求代码必须先写上）。
2. **图标是借用** `assets/table/crown.png`（地主皇冠）。原版篝火图标是单色字形，皇冠是彩色的，  
   观感可能不统一 —— 要更好看就得给 mod 打 pck，那是另一件事。
3. **赌注平衡**：BaseStake=100 × 倍数最高可到 ×16 = 1600 金币一把。想调就改  
   `DouSpireConfig.BaseStake`，或后面给它加个独立的篝火底注设置项。
4. **没实机跑过**，一行都没跑过。

---

## 六、编译 / 部署（同以前）

```bash
cd "C:/Users/有花无实/Documents/Default Project/projects/dou-spire-rules"
export HTTP_PROXY=http://127.0.0.1:7892 HTTPS_PROXY=http://127.0.0.1:7892
bash tools/build-mod.sh ../dou-spire
```

游戏开着 → 拷 dll 会失败（文件被锁）。本次编译时游戏没开，**已自动部署**到  
`mods\DouSpire\DouSpire.dll`（23:43）。

---

## 七、参考实现（照抄对象）

- **`RestSiteExpansion`**（工坊 3802537170，Dixie）—— 专做休息处扩展，一次加 14 个选项。  
  它那个 `RestSiteExpansionSingleton : HookedSingletonModel` 就是注册范本；  
  还额外写了 `RestSiteChoicesLayoutPatch` 来容纳变多的按钮。
- **`Miyabists2`**（工坊 3747607263，星见雅）—— `ChoukaRestSiteOption` 是"抽卡"选项，返回 false。
- 原版：`Shovel` 遗物 + `HealRestSiteOption`（写法最干净的一个）。

