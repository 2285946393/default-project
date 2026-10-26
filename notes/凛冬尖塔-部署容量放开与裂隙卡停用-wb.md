# 凛冬尖塔 · 部署容量放开 + 「开启凛冬裂隙」停用（2026-10-06 22:5x）

> 用户实测反馈：「这次没有卡死。但是现在还不用设置召唤和容量上限。首先我得测难度。多少召唤上限合适。
> 其次这牌有什么用。地图上是不能打出牌。只有战斗中才能打出牌。然后这牌又不能在战斗中打出，
> 然后你给我这些召唤物有什么用。第一次打出后然后因为容量或者格子问题打不了。然后就纯纯在手里也打出不了。
> 卡自己？发了五张诅咒？」

**卡死修复已确认生效** ✓（上一轮的僵尸怪修复）。本轮处理用户指出的三个设计问题。

---

## 一、★ 「开启凛冬裂隙」是死牌 —— 已停用发放

### 为什么它打不出去

```csharp
protected override bool IsPlayable {
    get {
        if (CombatManager.Instance.IsInProgress) return false;      // 战斗里不行
        return Owner?.RunState is RunState state && state.CurrentRoom is MapRoom;   // 只许地图房
    }
}
```

条件本身没错，错的是**假设**：它假设「站在地图房时手牌是可见可点的」。
实际上**地图房没有手牌 UI** —— 玩家在地图上根本看不到手牌。
⇒ 战斗里被 `IsPlayable` 挡、地图上没入口 → **永远打不出去**。

### 为什么会有这张牌

它是「阶段 1」的最低成本入口（见 `OpenFrostRiftCard` 原注释）：
当时**地图上放点**要动 `ActMap.Grid` + 地图屏图标/可达性补丁（一大坨 UI 活儿），
所以先用「遗物发一张牌当门把手」过渡。

而**阶段 2 已经做完**：地图上会注入「凛冬裂隙」问号节点（`FrostRiftMapNode`），
点它就能进裂隙 —— **这才是玩家实际在用的入口**（用户上一轮报的"点传送黑屏"就是它）。
⇒ 卡牌入口彻底冗余。

### 处理

- `FrostPactRelic.AfterObtained` **不再调** `GrantRiftCardAsync()`；
- **类保留不删** —— 老存档里可能有这张卡的实例，删类会让存档读不出来；
  它在战斗里依旧变灰，不干扰战斗；
- `GrantRiftCardAsync` 方法保留（以后要接「地图热键开门」可以复用）。

> ★ 通用教训：**「某个房间类型下能出牌」要先确认那个房间有没有手牌 UI。**
> 地图房、事件房这些"非战斗房间"都没有手牌界面。

---

## 二、★ 容量 5 点只够放 2 个 → 打不出的牌卡手

每张部署牌 `DeploySize = 2`，`BaseCapacity = 5`：

| 打出第几张 | 已占 | 剩余 | 能否再打（需 2 点） |
|---|---|---|---|
| 1 | 2 | 3 | ✓ |
| 2 | 4 | 1 | ✗ |

而**打不出的牌不会消耗**，就一直在手里占位置 —— 用户说的「卡自己」。

### 处理：加开关，默认不限量

```csharp
// FrostSpireConfig
public static bool LimitCapacity { get; set; } = false;   // 默认 false = 不限量

// FrostDeployment
public static bool CanDeploy(Creature? playerCreature, int size)
{
    if (!FrostSpireConfig.LimitCapacity) return true;      // ★ 不限量模式
    return GetUsedCapacity(playerCreature) + ClampSize(size) <= BaseCapacity;
}

public static int GetRemainingCapacity(Creature? playerCreature)
    => FrostSpireConfig.LimitCapacity
        ? System.Math.Max(BaseCapacity - GetUsedCapacity(playerCreature), 0)
        : 99;                                              // 只是日志好看，不用 int.MaxValue 免得刷屏
```

依据用户原话：「现在不用设置召唤和容量上限，首先我得测难度」。

---

## 三、★ 容量基准值：5 → 6（建议值，待实测确认）

**依据**：抄的 Monster Train 2 是 **5 点/层**，但 **MT2 是多层列车**（每层独立容量）；
而《杀戮尖塔》战斗**没有"层"的概念**，我们只有一层
⇒ 等于把 MT2 的多层容量压成了一层，5 点偏紧。

**6 点**的好处：

- = **3 个标准单位**（每张 2 点）→ 经典的「1 前排 + 2 后排」战线；
- 也刚好能放 1 重装(3) + 1 标准(2) + 1 小件(1)。

**给用户的实测对照表**：

| 实测体感 | 建议上限 |
|---|---|
| 2 个建筑就够打 16 波 | 4~5 |
| 3 个刚好，4 个就很稳 | 6 |
| 3 个扛不住，得 4 个以上 | 8~10 |

---

## 四、★ 给用户的设计建议：卡手可以根治（待确认）

现在放开限制不会卡手了。但**以后加回限制时，别让牌变灰卡手**。

正确做法照「征召民兵」：

```csharp
// SummonMilitiaCard 现在就是这套 —— 已有民兵时打出 = 强化，不占新容量
if (FindMilitia(player) is { } militia) {
    await CreatureCmd.GainMaxHp(militia, 4m);
    await CreatureCmd.Heal(militia, 4m, false);
    return;   // 不召唤新的，也不吃容量
}
```

⇒ 建议给**冰垒 / 箭塔**也加「重复部署 = 强化已有同类建筑」，
这样每张牌在任何时候都打得出去，永远不会卡手。**待用户点头**。

---

## 五、当前发牌清单（用户说「发了五张」）

| 来源 | 卡 | 数量 |
|---|---|---|
| 遗物 `AfterObtained` | 开启凛冬裂隙 | ~~1~~ → **已停发** |
| 进裂隙 `GrantStarterCardsAsync` | 征召民兵 / 冰垒(前排) / 冰垒(后排) / 箭塔 | 4 |

⇒ 停发后，进裂隙只发 **4 张部署牌**。

> ⚠️ 停发只对**新开局**生效。当前局牌库里那张旧卡会留着（打不出，但不影响玩）——
> 想干净就开新局。

---

## 六、改动清单

| 文件 | 改动 |
|---|---|
| `FrostSpireConfig.cs` | 新增 `LimitCapacity`（bool，默认 false）+ 设置页标签 |
| `FrostDeployment.cs` | `BaseCapacity` 5→6；`CanDeploy`/`GetRemainingCapacity` 支持不限量 |
| `FrostPactRelic.Rift.cs` | 停用 `GrantRiftCardAsync()` 调用；类注释重写 |
| `OpenFrostRiftCard.cs` | 类注释重写，标注「已废弃」并写清原因 |

构建 **0 错 0 警**，已部署（MD5 `8f0541245b079c0c05e72117d202ed2e`）。

---

## 七、待用户确认

1. **容量上限最终定多少**（需实测 16 波难度后反馈）；
2. **要不要给冰垒/箭塔加「重复部署 = 强化」**（根治卡手）；
3. 「幕」是否要真切 `state.Act`（上一轮遗留）；
4. 走完最后一场大战斗是回原地图还是直接结束整局（上一轮遗留）。
