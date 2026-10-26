using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Commands;
using MinionLib.Minion;
using FrostSpire.Monsters;

namespace FrostSpire.Deploy;

/// <summary>
/// 凛冬尖塔 · 部署系统（M3.3，已改走 MinionLib）—— 「单线上的有限部署位」。
///
/// 设计参照 Monster Train 2 的**真实数值与模型**（反编译 Assembly-CSharp.dll 取证，见
/// notes/凛冬尖塔-M3工事牌设计案-怪物火车方向.md 第七节）：
///   · 每层容量 `characterCapacityPerFloor` = **5 点**
///   · 单位 Size 范围 **1~6**，普通单位默认 **2**
///   · ★ 容量是「**权重和**」不是「格位数」：
///       GetRoomCapacity() = Σ 存活单位.Size
///       WillBeAtCapacity(s) = GetRoomCapacity() + s &gt; GetMaxCapacity()
///   · MT2 是 7 个点位配 5 点容量 → **永远填不满**，取舍感来自「容量贵」。
///
/// 凛冬尖塔的落地：**不引入点位概念**（《杀戮尖塔》战斗无位置），只用「权重和」这一个判定。
/// 场上所有「我方非玩家生物」（建筑 + 单位）都算占用；玩家本体不算。
///
/// ★ M3.3 关键改动：造建筑从 `CreateCreature + CreatureCmd.Add` 改为
///   **`MinionCmd.AddMinion&lt;T&gt;`**。原因见 FrostBuildingMonster 的类注释 ——
///   裸生物没有 PetOwner、不在 Pets 列表，`DieForYouPower` 这类替伤能力会静默失效。
/// </summary>
public static class FrostDeployment
{
    /// <summary>
    /// 每场战斗的基础容量上限（照抄 MT2 的 5，凛冬上调到 6）。
    ///
    /// ★ 为什么是 6 而不是 MT2 的 5：MT2 是**多层列车**，5 点是**单层**的容量；
    ///   而《杀戮尖塔》战斗没有"层"的概念，我们只有一层，等于把 MT2 的多层容量压成一层。
    ///   6 点 = **3 个标准单位（Size 2）** = 经典的「1 前排 + 2 后排」战线配置，
    ///   也刚好能放下 1 个重装（3）+ 1 个标准（2）+ 1 个小件（1）。
    ///   ⚠️ 这个值是**建议基准**，最终数值待用户实测 16 波难度后定
    ///   （见 `FrostSpireConfig.LimitCapacity`）。
    /// </summary>
    public const int BaseCapacity = 6;

    /// <summary>单个单位的 Size 下限 / 上限（照抄 MT2 的 Clamp(1,6)）。</summary>
    public const int MinSize = 1;
    public const int MaxSize = 6;

    /// <summary>单位 Size 规范化为 1~6。</summary>
    public static int ClampSize(int size) => System.Math.Clamp(size, MinSize, MaxSize);

    /// <summary>
    /// 当前战场占用 = Σ「我方随从/建筑」的 Size。
    ///
    /// ★ 用 `Player.PlayerCombatState.Pets`（而不是战斗全体 Allies）来数：
    ///   这才是「我方场上生物」的正确定义，且与 MinionLib 的站位/守护顺序一致。
    /// </summary>
    public static int GetUsedCapacity(Creature? playerCreature)
    {
        var player = playerCreature?.Player;
        var pcs = player?.PlayerCombatState;
        if (pcs == null) return 0;

        int used = 0;
        foreach (var pet in pcs.Pets)
        {
            if (pet == null || !pet.IsAlive) continue;
            used += GetSize(pet);
        }
        return used;
    }

    /// <summary>取一个生物占用的 Size。v1 用「牌自己声明的占位」或者默认 2。</summary>
    public static int GetSize(Creature creature)
    {
        // 挂 FrostDeployCostPower 可显式覆盖 Size（牌打出时声明）。
        var cost = creature.GetPower<FrostDeployCostPower>();
        if (cost != null) return ClampSize((int)cost.Amount);

        return 2;   // 默认 2（= MT2 的 CharacterData 默认值）
    }

    /// <summary>
    /// 还剩多少容量。★ 不限量模式（`LimitCapacity = false`）返回 99 —— 只是为了日志好看，
    /// 不返回 int.MaxValue 免得刷屏。
    /// </summary>
    public static int GetRemainingCapacity(Creature? playerCreature)
        => FrostSpireConfig.LimitCapacity
            ? System.Math.Max(BaseCapacity - GetUsedCapacity(playerCreature), 0)
            : 99;

    /// <summary>
    /// 能不能再放下一个 size 这么大的东西（= MT2 的 WillBeAtCapacity）。
    ///
    /// ★ 不限量模式直接 true —— 这是当前默认。原因见 `FrostSpireConfig.LimitCapacity`：
    ///   用户要先测难度，容量限制会让部署牌变灰卡手（打出 2 张后剩下的全打不出去），
    ///   干扰难度判断。等数值定了再打开。
    /// </summary>
    public static bool CanDeploy(Creature? playerCreature, int size)
    {
        if (!FrostSpireConfig.LimitCapacity) return true;
        return GetUsedCapacity(playerCreature) + ClampSize(size) <= BaseCapacity;
    }

    /// <summary>给牌用的便捷判定。</summary>
    public static bool CanDeploy(Creature? playerCreature, FrostDeploySize size)
        => CanDeploy(playerCreature, (int)size);

    // ========================================================================
    //  ★ 重复部署 = 强化（M3.7，2026-10-06 用户要求：「然后重复部署给强化吧」）
    //
    //  语义：**同类型 + 同站位**的建筑已经在场上 → 不再召唤第二座，
    //        而是把已有那座练强（+3 生命上限并回 3 血），然后**返回那一只**。
    //
    //  为什么返回"已有那只"很关键：卡片的 `AfterSummonAsync` 会照常执行，
    //  而各张卡的光环（箭塔 +4 AOE / 后排冰垒 +3 格挡 / 篝火 +2 回血 / …）
    //  都是**按层数叠加**的 → 重打一次 = 光环 +1 层。**"强化"这条链是白拿的。**
    //
    //  ⇒ 附带的好处：**每张部署牌任何时刻都打得出去**，彻底根治「卡手」。
    //    老版本容量满时牌会变灰、打不出去、还占着手牌位 —— 用户原话「然后就纯纯在手里也打不出
    //    去。卡自己？」。现在同类型重打永远是"强化"，不需要容量。
    //
    //  ★ 为什么要带「站位」一起判：`IceBulwarkCard`（前排）和 `IceBulwarkBackCard`（后排）
    //    召唤的是**同一个** `IceBulwarkM`。只按类型判的话，玩家打前排牌会去强化后排那座，
    //    语义就乱了。`MinionModel.Position` 的 getter 是 public（反编译确认），读得到。
    // ========================================================================

    /// <summary>一次「重复部署」给建筑加多少生命上限（并等量回血）。</summary>
    public const int StrengthenHp = 3;

    /// <summary>
    /// 找玩家场上「同类型 + 同站位」的活建筑（没有则 null）。
    /// 泛型版给 `AddBuildingAsync` 用；非泛型版给卡的 `IsPlayable` 用（那里只有 `System.Type`）。
    /// </summary>
    public static Creature? FindSameBuilding(Creature? playerCreature, System.Type buildingType, MinionPosition position)
    {
        var pets = playerCreature?.Player?.PlayerCombatState?.Pets;
        if (pets == null) return null;

        foreach (var c in pets)
        {
            if (c == null || !c.IsAlive) continue;
            if (c.Monster is not MinionModel m) continue;
            if (!buildingType.IsInstanceOfType(m)) continue;
            if (m.Position != position) continue;
            return c;
        }
        return null;
    }

    /// <summary>把一座已有建筑练强：+生命上限并回血。光环层数由卡片的 AfterSummonAsync 负责。</summary>
    public static async Task StrengthenAsync(Creature building, int amount = StrengthenHp)
    {
        await CreatureCmd.GainMaxHp(building, amount);
        await CreatureCmd.Heal(building, amount, false);
    }

    /// <summary>
    /// ★ 召唤一座建筑/单位到场上（冰垒 / 箭塔 / 民兵 / … 共用）。
    ///
    /// 做两件事：① 用 MinionLib 召唤（→ 自动挂 PetOwner、进 Pets、排站位）
    ///          ② 声明部署占位。容量不够时返回 null，调用方据此中止（不消耗牌）。
    ///
    /// 已核 API：
    ///   · `MinionCmd.AddMinion&lt;T&gt;(choiceContext, player, options)` → `Task&lt;Creature&gt;`
    ///   · `MinionSummonOptions(MaxHp, PrimaryStatAmount, ..., CardModel? Source, MinionPosition Position)`
    ///
    /// `T` 必须是 `MinionModel` 子类（见 FrostBuildingMonster / FrostUnitMonster 的类注释）。
    /// </summary>
    public static async Task<Creature?> AddBuildingAsync<T>(
        PlayerChoiceContext choiceContext,
        Player player,
        int deploySize,
        FrostDeploySize sizeClass = FrostDeploySize.Normal,
        MinionPosition position = MinionPosition.Front,
        CardModel? source = null)
        where T : MinionModel
    {
        if (player == null) return null;

        // ① ★ 已有同类建筑（同类型 + 同站位）→ 不新增，改成强化。
        //    放在容量校验**之前**：强化不需要新容量，容量满了照样该能重打（这正是"根治卡手"的关键）。
        if (FindSameBuilding(player.Creature, typeof(T), position) is { } existing)
        {
            await StrengthenAsync(existing);
            Entry.Logger.Info($"凛冬尖塔：已有同类建筑（{typeof(T).Name}@{position}），改为强化 +{StrengthenHp} 生命上限。");
            return existing;
        }

        // ② 容量校验：不够就中止（牌不消耗、不召唤）
        var playerCreature = player.Creature;
        if (playerCreature == null) return null;
        if (!CanDeploy(playerCreature, deploySize)) return null;

        // ③ MinionLib 召唤：内部 = PlayerCmd.AddPet<T> + 设 Position + TakeSnapshot + OnSummon + Rearrange
        var building = await MinionCmd.AddMinion<T>(choiceContext, player, new MinionSummonOptions(
            MaxHp: null,                 // null = 用模型自己的 MaxInitialHp
            PrimaryStatAmount: null,
            SecondaryStatAmount: null,
            TertiaryStatAmount: null,
            Source: source,
            Position: position));

        if (building == null) return null;

        // ④ 声明部署占位（Size 权重，用于容量判定）
        await PowerCmd.Apply<FrostDeployCostPower>(choiceContext, building, deploySize, null, null);

        return building;
    }
}

/// <summary>工事牌的 Size 档位（写在卡上，部署时读）。</summary>
public enum FrostDeploySize
{
    Small = 1,      // 小件：符文壁 / 冰法师
    Normal = 2,     // 标准：冰垒 / 箭塔 / 民兵 / 猎手
    Heavy = 3,      // 重装：熔炉 / 重装兵
    Colossus = 5,   // 巨兽：一张占满（预留，M3.4 出）
}

/// <summary>
/// 「部署占用」标记能力：挂在一个生物身上，显式声明它占几点容量。
/// 不挂则用 FrostDeployment 的默认值 2。
/// 层数 = Size（Single 不叠加）。
/// </summary>
[STS2RitsuLib.Interop.AutoRegistration.RegisterPower]
public sealed class FrostDeployCostPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;

    /// <summary>部署占位是纯内部数据，不显示成"增益"图标，避免污染状态栏。</summary>
    protected override bool IsVisibleInternal => false;
}
