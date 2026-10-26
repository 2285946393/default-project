using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;
using FrostSpire.Deploy;
using FrostSpire.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Cards;

// ============================================================================
// 凛冬工事 · 扩充卡池（M3.6）
//
// ★ 为什么加这一批（用户原话）：
//   「凛冬前线的玩法应该是各种有意思卡。我们就这么几个基础塔有什么用呢。
//     连人家邦布的效果都比我们多，而他甚至都是角色mod附带的一个小玩意」
//
//   《凛冬前线》的卡分**五大类**：攻击 / 防御 / **技能** / **建筑**（数量最多）/ **精锐**（可成长）。
//   我们原来只有 4 张「建筑」，且**全是被动站桩** —— 既没有技能卡，也没有"精锐"那种能养的。
//   这一批按那个框架补：
//     · 建筑：篝火 / 瞭望台 / 符文壁（三种不同职能的场地支援）
//     · 精锐：霜爆塔（蓄力爆发）/ 寒冰王座（巨兽光环）
//     · 控制：寒霜法师（虚弱 + 冰封）
//     · 技能：暴风雪 / 冰封术（我们**第一张非部署卡**）
//
// 全部走已验证的 `FrostDeployCardBase` 骨架（容量判定 / 前中后排 / 召唤后挂效果）。
//
// ★★ M3.7 分工（配合「重复部署 = 强化」）：
//   **光环挂在卡片的 `AfterSummonAsync` 里**，不挂在建筑的 `OnSummon` 里。
//   因为重复部署时**不会召唤新建筑**，`OnSummon` 不跑；但 `AfterSummonAsync` 每次都跑
//   → 重打一次 = 光环层数 +1 = 建筑变强。这就是「强化」的手感来源。
//   （建筑的 `OnSummon` 只负责挂**可点击行动**，那个只能挂一次。）
// ============================================================================

/// <summary>篝火（0 费 / Size 1 / 后排）：5 血建筑，每回合回 2 血。免费铺场。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class CampfireCard : FrostDeployCardBase
{
    public CampfireCard() : base(0, CardRarity.Common) { }

    public override int DeploySize => (int)FrostDeploySize.Small;
    public override MinionPosition Position => MinionPosition.Back;
    protected override System.Type BuildingType => typeof(CampfireM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<CampfireM>(ctx, player, DeploySize, FrostDeploySize.Small, Position, source);

    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
        => await PowerCmd.Apply<Powers.CampfireAuraPower>(ctx, building, 1m, player.Creature, this);
}

/// <summary>瞭望台（1 费 / Size 1 / 后排）：4 血建筑，每回合多抽 1 张牌。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class WatchtowerCard : FrostDeployCardBase
{
    public WatchtowerCard() : base(1, CardRarity.Uncommon) { }

    public override int DeploySize => (int)FrostDeploySize.Small;
    public override MinionPosition Position => MinionPosition.Back;
    protected override System.Type BuildingType => typeof(WatchtowerM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<WatchtowerM>(ctx, player, DeploySize, FrostDeploySize.Small, Position, source);

    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
        => await PowerCmd.Apply<Powers.WatchtowerAuraPower>(ctx, building, 1m, player.Creature, this);
}

/// <summary>
/// 符文壁（2 费 / Size 1 / 前排）：8 血建筑，**受普通攻击伤害减半**。
/// ⇒ 等效 16 血，比冰垒（6 血）硬得多，但**不替玩家挡伤**（它是自己扛，不是转移）。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class RuneWallCard : FrostDeployCardBase
{
    public RuneWallCard() : base(2, CardRarity.Uncommon) { }

    public override int DeploySize => (int)FrostDeploySize.Small;
    public override MinionPosition Position => MinionPosition.Front;
    protected override System.Type BuildingType => typeof(RuneWallM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<RuneWallM>(ctx, player, DeploySize, FrostDeploySize.Small, Position, source);

    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
        => await PowerCmd.Apply<Powers.RuneWallPower>(ctx, building, 1m, player.Creature, this);
}

/// <summary>寒霜法师（2 费 / Size 1 / 后排）：4 血。被动挂虚弱，主动可冰封（让敌人跳回合）。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostMageCard : FrostDeployCardBase
{
    public FrostMageCard() : base(2, CardRarity.Uncommon) { }

    public override int DeploySize => (int)FrostDeploySize.Small;
    public override MinionPosition Position => MinionPosition.Back;
    protected override System.Type BuildingType => typeof(FrostMageM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<FrostMageM>(ctx, player, DeploySize, FrostDeploySize.Small, Position, source);

    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
        => await PowerCmd.Apply<Powers.FrostMageAuraPower>(ctx, building, 1m, player.Creature, this);
}

/// <summary>霜爆塔（3 费 / Size 2 / 后排）：6 血。每回合蓄力 +4，点它引爆打全体。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostBurstTowerCard : FrostDeployCardBase
{
    public FrostBurstTowerCard() : base(3, CardRarity.Rare) { }

    public override int DeploySize => (int)FrostDeploySize.Normal;
    public override MinionPosition Position => MinionPosition.Back;
    protected override System.Type BuildingType => typeof(FrostBurstTowerM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<FrostBurstTowerM>(ctx, player, DeploySize, FrostDeploySize.Normal, Position, source);

    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
        => await PowerCmd.Apply<Powers.FrostBurstChargePower>(ctx, building, 1m, player.Creature, this);
}

/// <summary>
/// 寒冰王座（4 费 / Size 5 / 后排）：14 血**巨兽**，一张占满容量。
///   · 部署时：**全体友军 +3 力量**（一次性爆发强化）
///   · 每回合：给玩家 +2 格挡
/// ⇒ 「养大哥」的核心 —— 把容量全押给它，用光环把整条战线拉起来。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostThroneCard : FrostDeployCardBase
{
    /// <summary>部署时给全体友军的力量。</summary>
    public const int StrengthOnDeploy = 3;

    public FrostThroneCard() : base(4, CardRarity.Rare) { }

    public override int DeploySize => (int)FrostDeploySize.Colossus;
    public override MinionPosition Position => MinionPosition.Back;
    protected override System.Type BuildingType => typeof(FrostThroneM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<FrostThroneM>(ctx, player, DeploySize, FrostDeploySize.Colossus, Position, source);

    /// <summary>
    /// 部署瞬间：全体友军（含它自己）+3 力量；并给王座挂上每回合 +2 格挡的光环。
    /// ★ 重复部署时**这段会再跑一遍** → 全体再 +3 力量、光环再 +1 层。
    ///   4 费换"全体 +3 力量"，是「养大哥」的正当回报（也是唯一会重复给全体加力量的牌）。
    /// </summary>
    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
    {
        await PowerCmd.Apply<Powers.FrostThroneAuraPower>(ctx, building, 1m, player.Creature, this);

        var pets = player.PlayerCombatState?.Pets;
        if (pets == null) return;

        foreach (var pet in pets)
        {
            if (pet == null || !pet.IsAlive) continue;
            await PowerCmd.Apply<StrengthPower>(ctx, pet, StrengthOnDeploy, player.Creature, this);
        }

        Entry.Logger.Info($"凛冬尖塔：寒冰王座降临，全体友军 +{StrengthOnDeploy} 力量。");
    }
}

// ============================================================================
//  技能卡（我们第一批**不部署任何东西**的牌）
// ============================================================================

/// <summary>
/// 暴风雪（2 费技能）：对**全体敌人**造成 8 点伤害，并各挂 1 层虚弱。
///
/// 定位：清场 / 补刀 / 削弱。对标《凛冬前线》的「攻击卡」——
///   它说攻击卡"往往附带各种特殊效果，如削弱敌人护甲、造成范围伤害"。
///
/// ★ 附带好处：技能卡**永远打得出去**（不占容量、不看场地），
///   是对「部署牌被容量卡住 = 卡手」的结构性补充。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class BlizzardCard : CardModel, IFrostCardArt
{
    public const int Damage = 8;
    public const int WeakAmount = 1;

    public BlizzardCard() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.None) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.Creature == null) return;

        var enemies = player.Creature.CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        await CreatureCmd.Damage(choiceContext, enemies, Damage, ValueProp.Unpowered, player.Creature);

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, WeakAmount, player.Creature, this);
        }

        Entry.Logger.Info($"凛冬尖塔：暴风雪，全体敌人 -{Damage} 并各挂 {WeakAmount} 层虚弱。");
    }

    protected override void OnUpgrade() { /* v1：技能卡不升级 */ }
}

/// <summary>
/// 冰封术（1 费技能）：**让一个敌人跳过下一个回合**（单体 `Stun`）。
///
/// 定位：打断 Boss 大招 / 争一回合喘息。
/// 和寒霜法师的主动技同源，但**不依赖场上有没有法师** —— 给控制流一张随时能用的牌。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FreezeCard : CardModel, IFrostCardArt
{
    public FreezeCard() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null || !target.IsAlive) return;

        await CreatureCmd.Stun(target);

        Entry.Logger.Info($"凛冬尖塔：冰封术冻住了 {target.Name}（跳过下一回合）。");
        Entry.Toast($"冰封：{target.Name} 将跳过下一回合。", "凛冬尖塔");
    }

    protected override void OnUpgrade() { /* v1：技能卡不升级 */ }
}
