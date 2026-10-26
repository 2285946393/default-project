# 凛冬尖塔 · ★ 重大发现：MinionLib（随从框架）wb

> 2026-10-06 17:45。**毛烨提示**：「那个 mod 角色……有很多邦布。绝区零的 mod 角色」
> **找到**：创意工坊 `Miyabi The Spire`（星见雅，作者 月下樱）+ 依赖库 `MinionLib`。
> **结论：MinionLib 是凛冬尖塔「部署系统」的现成工业级方案，比我们手搓的强得多。建议改用。**

---

## 一、找到的东西

| 项 | 内容 |
|---|---|
| **Mod** | `Miyabi The Spire`（星见雅 / 绝区零），工坊 ID `3747607263`，作者「月下樱」 |
| **文件** | `Miyabists2.dll`(967KB) + `Miyabists2.pck`(71MB 美术) |
| **依赖库** | **`MinionLib`**（工坊 ID `3747595039`，作者 FuYnAloft，v0.6.3） |
| **邦布** | `AgentBangboo` / `EousBangboo` / `ExcalibooBangboo` / `Exploreboo` / `Luckyboo` / `Magnetiboo` / `Sharkboo` / `Sumoboo` / `Paperboo` / `Overtimeboo` … |
| **邦布卡** | `BangbooSummonOne`(召唤) / `BangbooActiveOne`(激活) / `BangbooChargeAll`(充能) / `BangbooHelpmeOne`(求助) / `BangbooUseOnemore`(再用一次) |

**mod 里还有仪玄（Yixuan）** —— 那一大堆 `_Yixuan.*` 都是。

---

## 二、★★ MinionLib 为什么对凛冬尖塔是重大发现

我们 M3.2 手搓了一套"建筑生物 + 容量"，而 **MinionLib 提供的是完整工业级随从系统**：

| 模块 | 类 | 我们现状 | 差距 |
|---|---|---|---|
| **随从基类** | `Minion.MinionModel` | 手搓 `FrostBuildingMonster` | ★ 思路一致，但人家有站位 |
| **召唤命令** | `Commands.MinionCmd.AddMinion<T>` | 手搓 `DeployBuildingAsync` | 人家自动排布 + 快照 |
| **★ 站位** | `Minion.MinionPosition { Front, Back, FrontUpper, BackUpper, Upper }` | **完全没有** | ★★★ 核心缺失 |
| **★ 守护替伤** | `Powers.MinionGuardianPower` | 用 `DieForYouPower` | ★★★ 人家按站位、支持链式 |
| **自动排布** | `Layout.MinionLayoutManager` / `DefaultMinionLayout` | 无 | 视觉 |
| **随从目标池** | `Targeting.Pets.AllMinionsTargetType` 等 | 手动筛 | 卡牌目标 |
| **组件化卡牌** | `Component.CardComponent` / `ComponentsCardModel` | 无 | 高级特性 |
| **右键交互** | `RightClick.RightClickDispatcher` | 无 | 高级特性 |

---

## 三、★★★ 最关键的三个发现（源码实证）

### 3.1 随从底层**也是 `PlayerCmd.AddPet`**（验证了我们的判断）

```csharp
// MinionLib.Commands.MinionCmd（反编译）
public static async Task<Creature> AddMinion<T>(PlayerChoiceContext ctx, Player player, MinionSummonOptions options)
{
    Creature pet = await PlayerCmd.AddPet<T>(player);          // ★ 底层就是 AddPet
    if (pet.Monster is MinionModel m) m.Position = options.Position;   // ★ 额外设站位
    PetOrderSnapshotManager.TakeSnapshot(player);
    if (pet.Monster is MinionModel m2) await m2.OnSummon(ctx, player, options);
    MinionAnimCmd.Rearrange();                                  // ★ 重排布局动画
    return pet;
}
```

**含义**：MinionLib 的「随从」= **宠物 + 站位 + 排布**。我们 M3.2 走的是"非宠物生物"路线（可以，但少了站位）。

### 3.2 ★★★ `MinionGuardianPower` = 「建筑挡伤害」的**工业级正确实现**

比 `DieForYouPower` 强得多（反编译全貌）：

```csharp
public sealed class MinionGuardianPower : PowerModel
{
    public override Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature? dealer)
    {
        // ★ ① 只有**前排**（Front）的随从才挡 —— 这就是"站位"的意义
        if (Owner.Monster is MinionModel { Position: not MinionPosition.Front })
            return target;

        Player? petOwner = Owner.PetOwner;
        if (target != petOwner?.Creature)
        {
            bool flag = true;
            // ★ ② 链式守护：如果目标也是带 Guardian 的随从，
            //    且我在它"前面"（Index 更小）→ 我替它挡
            if (target.PetOwner == petOwner && petOwner != null
                && target.GetPower<MinionGuardianPower>() != null)
            {
                var pets = target.PetOwner.PlayerCombatState.Pets;
                if (pets.IndexOf(Owner) < pets.IndexOf(target)) flag = false;
            }
            if (flag) return target;
        }

        if (Owner.IsDead) return target;

        // ★ ③ 只挡普通攻击（ValueProp 位判定，注释说 Powered 且非 Unpowered）
        if (!props.HasFlag(ValueProp.Powered) || props.HasFlag(ValueProp.Unpowered))
            return target;

        return Owner;    // 转移伤害 → 打到守护者身上
    }
}
```

**三个设计亮点**（都是我们没有的）：
1. **只有前排挡** → 玩家可以把建筑放**前排**（挡伤）或**后排**（纯输出）
2. **链式守护** → 前面的一排建筑依次替后面的挡，形成真正的"阵线"
3. **只挡普通攻击** → 毒/能力伤害穿透（和 `DieForYouPower` 一致的好设计）

配套三个 Patch：`BlockToHpPatch` / `OverkillPatch` / `OwnerDamageSuppressPatch`。

### 3.3 `MinionModel` 的静态状态机（和我们写的一致）

```csharp
public abstract class MinionModel : MonsterModel
{
    public MinionPosition Position { get; internal set; }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var idle = new MoveState("MINION_IDLE", _ => Task.CompletedTask, Array.Empty<AbstractIntent>())
        { FollowUpState = null };
        idle.FollowUpState = idle;                 // 自己循环
        return new MonsterMoveStateMachine(new[] { idle }, idle);
    }

    public virtual Task OnSummon(PlayerChoiceContext ctx, Player owner, MinionSummonOptions options)
        => Task.CompletedTask;
}
```

**→ 我们的 `FrostBuildingMonster` 思路正确**（同样是"单 idle 状态自己循环"），只是没站位。

---

## 四、MinionSummonOptions（调用极简）

```csharp
public readonly record struct MinionSummonOptions(
    decimal? MaxHp = null,
    decimal? PrimaryStatAmount = null,
    decimal? SecondaryStatAmount = null,
    decimal? TertiaryStatAmount = null,
    CardModel? Source = null,
    MinionPosition Position = MinionPosition.Front);    // 默认前排
```

**用法**（照 `MiyabiBangbooService`）：
```csharp
await MinionCmd.AddMinion<IceBulwarkM>(ctx, player,
    new MinionSummonOptions(MaxHp: 6m, PrimaryStatAmount: 1m, Source: thisCard,
                           Position: MinionPosition.Front));
```

---

## 五、★ 给凛冬尖塔的建议（三条路，等毛烨定）

### 路线 A：**改用 MinionLib**（推荐）
- **做法**：凛冬的建筑/单位 `: MinionModel`，召唤走 `MinionCmd.AddMinion`，挡伤用 `MinionGuardianPower`
- **好处**：白拿**站位系统**（前排挡 / 后排输出）+ 自动排布 + 目标池 + 右键交互
- **代价**：多一个依赖（`MinionLib`，已是工坊现成库）；我们的 `FrostDeployment` 容量系统改成"MinionLib 之上的一层"
- **对应用户意象**：★★★ **「建筑放在场上挡伤害」直接变成"放前排的建筑挡伤害"**，比现在更完整

### 路线 B：**保持现状，只借鉴思路**
- **做法**：继续手搓，但**抄它的站位概念**（给 `FrostDeployment` 加"前排/后排"概念）
- **好处**：零新依赖
- **代价**：站位/排布/链式守护都得自己写（工作量大，且容易出 bug）

### 路线 C：**混合**（折中）
- 建筑用 MinionLib（拿站位 + 守护），容量系统仍用我们的
- 依赖 MinionLib，但不用它的组件化卡牌（那部分是 Miyabi 那种复杂角色才需要）

---

## 六、一句话总结

**「很多邦布」的 mod = `Miyabi The Spire`，它的依赖库 `MinionLib` 就是一套现成的随从框架。**
**我们的「建筑挡伤害」手搓对了方向，但 MinionLib 的 `MinionGuardianPower` 是工业级版本** ——
**带站位（前排才挡）、带链式守护（真正的阵线）、带自动排布**。
**建议改用 MinionLib（路线 A）**，这样"建筑放前排挡伤害"就从"能用"变成"像个正经游戏机制"。

**⚠️ 待毛烨决定要不要引入这个依赖。**
