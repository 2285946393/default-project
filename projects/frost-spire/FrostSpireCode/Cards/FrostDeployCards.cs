using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using MinionLib.Powers;
using FrostSpire.Deploy;
using FrostSpire.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Cards;

/// <summary>
/// 凛冬工事牌的**部署基类**（M3.4）—— 「打出一座建筑/单位」的公共骨架。
///
/// ★ 关于「打出时选前/后排」的实现取舍（重要，写给未来的自己）：
///   本引擎**没有通用的「二选一弹窗」API**（反编译确认：全局只有
///   `CardPlay.Target`（一个 `Creature?`）、卡组选择屏、遗物选择屏，没有任意选项列表）。
///   而且 MinionLib 的 `MinionModel.Position` 是 **`internal set`**，
///   只在 `MinionCmd.AddMinion` 那一刻能设，**战斗中无法改站位**。
///
///   → 所以「前/后排」做成**卡牌变体**（和本体的 Strike / Strike+、以及星见雅的
///     「不同邦布站不同位」是同一套设计语言）：你想放前排就带前排的牌，
///     想放后排就带后排的牌 —— 变成**构筑决策**，而不是每次打牌都弹窗。
///     这比"每张牌都弹一次窗"更符合尖塔的设计习惯，也省掉一整块 UI 代码。
///
/// 子类只需声明：建筑模型、血量档位、站位、部署容量、召唤后挂什么。
/// </summary>
public abstract class FrostDeployCardBase : CardModel, IFrostCardArt
{
    protected FrostDeployCardBase(int cost, CardRarity rarity)
        : base(cost, CardType.Skill, rarity, TargetType.None)
    {
    }

    /// <summary>部署容量占几点（= MT2 的 Size 权重）。</summary>
    public abstract int DeploySize { get; }

    /// <summary>这座建筑站哪个位（Front = 挡刀；Back = 纯输出）。</summary>
    public abstract MinionPosition Position { get; }

    /// <summary>
    /// 这张牌召唤的建筑类型 —— 给「重复部署 = 强化」的判定用
    /// （同类型 + 同站位已有 → 强化它，不新增，也不占容量）。
    ///
    /// ★ 必须和 <see cref="SummonAsync"/> 里的泛型参数保持一致，
    ///   否则会出现「这里说没有、召唤时又说有」的判定错位。
    /// </summary>
    protected abstract System.Type BuildingType { get; }

    /// <summary>★ 战斗中且（容量够 或 已有同类建筑可强化）→ 才打得出去（否则牌变灰）。</summary>
    protected override bool IsPlayable
    {
        get
        {
            try
            {
                var creature = Owner?.Creature;
                if (creature == null) return false;
                if (!CombatManager.Instance.IsInProgress) return false;

                // ★ 已有同类建筑 → 重打只是强化，不需要新容量 → 任何时候都打得出去。
                //   这是「卡手」的根治：老版本容量满时牌会变灰、打不出、还占着手牌位。
                if (FrostDeployment.FindSameBuilding(creature, BuildingType, Position) != null) return true;

                return FrostDeployment.CanDeploy(creature, DeploySize);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>召唤具体建筑（子类实现，因为泛型需要编译期类型）。</summary>
    protected abstract Task<Creature?> SummonAsync(PlayerChoiceContext choiceContext, Player player, CardModel source);

    /// <summary>召唤成功后挂效果（子类实现，可空）。</summary>
    protected virtual Task AfterSummonAsync(PlayerChoiceContext choiceContext, Player player, Creature building)
        => Task.CompletedTask;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.Creature == null) return;

        var building = await SummonAsync(choiceContext, player, this);
        if (building == null) return;

        await AfterSummonAsync(choiceContext, player, building);

        Entry.Logger.Info(
            $"凛冬尖塔：{GetType().Name} 已部署（{Position} 位，占 {DeploySize}/{FrostDeployment.BaseCapacity} 容量，" +
            $"剩余 {FrostDeployment.GetRemainingCapacity(player.Creature)}）。");
    }

    protected override void OnUpgrade()
    {
        // v1：部署类牌不升级（升级语义 M3.5 统一设计：血 +3 / 费用 -1）。
    }
}

/// <summary>
/// 冰垒·前排（M3.4）—— 默认形态，替玩家挡普通攻击。
/// 1 费，6 血，Front 位，带 `MinionGuardianPower`。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class IceBulwarkCard : FrostDeployCardBase
{
    public IceBulwarkCard() : base(1, CardRarity.Common) { }

    public override int DeploySize => (int)FrostDeploySize.Normal;
    public override MinionPosition Position => MinionPosition.Front;
    protected override System.Type BuildingType => typeof(IceBulwarkM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<IceBulwarkM>(ctx, player, DeploySize, FrostDeploySize.Normal, Position, source);

    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
    {
        // ★ 冰垒的核心：替玩家挡普通攻击（前排守卫，碎一个下一个顶上）
        await PowerCmd.Apply<MinionGuardianPower>(ctx, building, 1m, null, null);
    }
}

/// <summary>
/// 冰垒·后排（M3.4）—— 变体：不挡刀，改成「每回合给玩家 3 格挡」的支援位。
///
/// 设计意图：给「前/后排」这个选择一个**真实的战术分歧**，而不是同一个东西放两个位置。
///   前排冰垒 = 硬吃伤害（换血）；后排冰垒 = 稳定叠甲（保守）。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class IceBulwarkBackCard : FrostDeployCardBase
{
    public IceBulwarkBackCard() : base(1, CardRarity.Common) { }

    public override int DeploySize => (int)FrostDeploySize.Normal;
    public override MinionPosition Position => MinionPosition.Back;
    protected override System.Type BuildingType => typeof(IceBulwarkM);   // ★ 和前排冰垒同类型，靠 Position 区分

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<IceBulwarkM>(ctx, player, DeploySize, FrostDeploySize.Normal, Position, source);

    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
    {
        // 后排冰垒：不替伤，改成每回合给玩家叠甲（挂 IceWallPower 到建筑身上，
        // IceWallPower 的 IsMyTurnStart 用的就是 Owner.Side → 建筑和玩家同阵营，照样每回合触发）。
        // ★ 重复部署时这里会再跑一次 → IceWallPower 层数 +1 → 每回合多 3 格挡。这就是"强化"。
        await PowerCmd.Apply<Powers.IceWallPower>(ctx, building, 1m, null, null);
    }
}

/// <summary>
/// 箭塔·后排（M3.4）—— 2 费，4 血，Back 位。
/// 每回合被动 AOE 4 点（`ArrowTowerPower`），外加主动技「齐射」（点箭塔触发 6 点 AOE）。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class ArrowTowerCard : FrostDeployCardBase
{
    public ArrowTowerCard() : base(2, CardRarity.Common) { }

    public override int DeploySize => (int)FrostDeploySize.Normal;
    public override MinionPosition Position => MinionPosition.Back;
    protected override System.Type BuildingType => typeof(ArrowTowerM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<ArrowTowerM>(ctx, player, DeploySize, FrostDeploySize.Normal, Position, source);

    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
    {
        // 每回合被动 AOE（层数=1 → 4 伤害）。★ 重复部署时这里再跑 → 层数 +1 → 每回合多 4 点 AOE。
        await PowerCmd.Apply<Powers.ArrowTowerPower>(ctx, building, 1m, null, null);
    }
}
