using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Minion;
using FrostSpire.Deploy;
using FrostSpire.Monsters;
using FrostSpire.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Cards;

// ============================================================================
// 凛冬「精锐」部署卡 + 晋升引擎（M3.12）—— 4 张
//
// ★ 补的是《凛冬前线》五大类的最后一块：**精锐（可成长领袖）**。
//   之前只有霜爆塔 / 寒冰王座两个，而且官方特色词条**一个都没做**。
//
// | 卡 | 费 | 词条 | 效果 |
// |---|---|---|---|
// | 霜刃卫士 | 2 | **连击** | 8 血，每回合打最前方敌人 **2 次**，每次 5(+力量) |
// | 不冻者 | 3 | **不朽** | 10 血，每回合打 6(+力量)，**回合结束回满血** |
// | 凛冬守望者 | 2 | **固守** | 12 血，它存活时**全体友军受伤 -1×层数** |
// | 凛冬操演 | 1 | — | 每回合所有随从 +1×层数 历战，满 4 → **晋升**（+4 血上限 / +1 力量） |
//
// ★★ 词条是**直译会水土不服**的，必须翻译成尖塔语境（详见 FrostEpicMonsters.cs 注释）：
//   尖塔随从**不会自动离场**，所以「固守 / 不朽」按字面做「延长在场时间」是**空效果**。
//   ⇒ 固守 = 团队减伤光环；不朽 = 回合结束回满血。
//
// ★ 全部走 `FrostDeployCardBase` ⇒ 自动继承 M3.7 的「重复部署 = 强化」
//   （同类型 + 同站位已有 → 不召唤新的，改给已有那只 +3 血上限）。
//   ⇒ 精锐特别适合强化：霜刃卫士多一层连击 = 每回合多打 2 下。
// ============================================================================

/// <summary>
/// 霜刃卫士（2 费 / Size 2 / 前排）：8 血精锐，**连击** —— 每回合攻击 2 次，每次 5(+力量) 点。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostBladeCard : FrostDeployCardBase
{
    public FrostBladeCard() : base(2, CardRarity.Uncommon) { }

    public override int DeploySize => (int)FrostDeploySize.Normal;
    public override MinionPosition Position => MinionPosition.Front;
    protected override System.Type BuildingType => typeof(FrostBladeM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<FrostBladeM>(ctx, player, DeploySize, FrostDeploySize.Normal, Position, source);

    /// <summary>
    /// ★ 光环挂在**卡**上（M3.6 起的规矩）：重复部署时这段会再跑一次
    /// → 层数 +1 → **每回合多打 2 下**（`FrostBladeComboPower.Hits * Amount`）。
    /// </summary>
    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
        => await PowerCmd.Apply<FrostBladeComboPower>(ctx, building, 1m, player.Creature, this);
}

/// <summary>
/// 不冻者（3 费 / Size 2 / 前排）：10 血精锐，**不朽** —— 每回合攻击 6(+力量)，回合结束**回满血**。
///
/// ★ 前排 + 回满 = 一个会自我修复的肉盾。它的**强度与弱点同源**：
///   遇到持续消耗永远打不死，遇到单回合高爆发（GlobeHead 16 伤）照样倒。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class UndyingFrostCard : FrostDeployCardBase
{
    public UndyingFrostCard() : base(3, CardRarity.Rare) { }

    public override int DeploySize => (int)FrostDeploySize.Normal;
    public override MinionPosition Position => MinionPosition.Front;
    protected override System.Type BuildingType => typeof(UndyingFrostM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<UndyingFrostM>(ctx, player, DeploySize, FrostDeploySize.Normal, Position, source);

    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
        => await PowerCmd.Apply<UndyingFrostPower>(ctx, building, 1m, player.Creature, this);
}

/// <summary>
/// 凛冬守望者（2 费 / Size 2 / 后排）：12 血精锐，**固守** —— 它存活时全体友军受伤 -1×层数。
///
/// ★ 站后排是**故意**的：它自己不掉血才能一直提供光环。
///   （前排那两个——霜刃卫士 / 不冻者——才是该挨打的位置。）
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostWardenCard : FrostDeployCardBase
{
    public FrostWardenCard() : base(2, CardRarity.Uncommon) { }

    public override int DeploySize => (int)FrostDeploySize.Normal;
    public override MinionPosition Position => MinionPosition.Back;
    protected override System.Type BuildingType => typeof(FrostWardenM);

    protected override Task<Creature?> SummonAsync(PlayerChoiceContext ctx, Player player, CardModel source)
        => FrostDeployment.AddBuildingAsync<FrostWardenM>(ctx, player, DeploySize, FrostDeploySize.Normal, Position, source);

    protected override async Task AfterSummonAsync(PlayerChoiceContext ctx, Player player, Creature building)
        => await PowerCmd.Apply<FrostWardenPower>(ctx, building, 1m, player.Creature, this);
}

/// <summary>
/// 凛冬操演（1 费 · 能力）：每回合给你**每个**随从 +1×层数 历战；
/// 历战满 4 → 那只随从**晋升**（+4 血上限、+1 力量、回 4 血），历战清零后可继续晋升。
///
/// ★★ 这是「养大哥」真正的引擎，也是本批最有分量的一张：
///   在此之前，随从**活得久没有任何额外收益**（只有"还没死"本身）；
///   有了它，**每多站 4 个回合 = 一次永久变强** ⇒ 玩家会开始保护老兵、
///   会为"这只已经晋升两次了"做取舍 —— 战场终于有了**经营感**。
///
/// ★ 它和 M3.9 的殉爆线构成一对**相反的策略**：
///   遗霜 = 让随从**死掉**换即时收益；凛冬操演 = 让随从**活着**换长期收益。
///   ⇒ 一副卡组里两条线拉扯，才是真正的选择。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostDrillCard : FrostTriggerCardBase<FrostDrillPower>
{
    public FrostDrillCard() : base(1, CardRarity.Common) { }
}
