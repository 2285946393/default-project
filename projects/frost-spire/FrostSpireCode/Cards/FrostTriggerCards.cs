using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using FrostSpire.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Cards;

// ============================================================================
// 凛冬「回响」· 触发式效果牌（M3.9）—— 11 张
//
// ★ 为什么是这一批（用户原话）：
//   「自己多加些凛冬牌。要有创意。比如那我方场上被摧毁，对所有敌人造成一次伤害。
//     这种效果牌。……把凛冬里面要触发效果的牌换成我们这边要触发效果的牌。诸如此类还要我多说吗」
//
// ⇒ 之前 16 张凛冬牌**全是"打出即生效"**，一张触发式都没有。
//   这一批把「触发点」全部挪到**我方侧**，一次补齐 6 个不同的触发时机：
//
//   | 卡         | 费 | 触发时机                | 效果                  |
//   |------------|----|-------------------------|-----------------------|
//   | 遗霜       | 1  | 我方随从被摧毁          | 全体敌人 6×层数       |
//   | 霜之遗赠   | 1  | 我方随从被摧毁          | 获得 8×层数 格挡      |
//   | 雪崩       | 1  | 我的牌被消耗            | 全体敌人 5×层数       |
//   | 余烬       | 1  | 我的牌被消耗            | 抽 1×层数 张牌        |
//   | 凛冬之怒   | 2  | 我方随从受到伤害        | 全体敌人 3×层数       |
//   | 碎冰       | 1  | 敌人的格挡被打破        | 对该敌人 8×层数       |
//   | 寒风       | 1  | 我弃一张牌              | 全体敌人 2×层数       |
//   | 霜之回响   | 2  | 我打出一张攻击牌        | 全体敌人 3×层数       |
//   | 霜燃       | 1  | （消耗源）抽 2 张牌     | 消耗                  |
//   | 冰棱       | 0  | （消耗源）造成 7 点伤害 | 消耗                  |
//   | 霜葬       | 0  | （主动殉爆）摧毁最弱随从 | 全体敌人 14，消耗     |
//
// ★★ 这批里最重要的一条自我约束：**「雪崩 / 余烬」必须有消耗源才不是死牌**。
//   开工前盘过整个凛冬卡池 —— **一张带 `Exhaust` 的牌都没有**。
//   如果不补「霜燃 / 冰棱」，玩家打出雪崩后会看到一句永远不兑现的描述。
//   ⇒ 所以「霜燃 / 冰棱」不是装饰，是消耗流的**发动机**。
//
// ★ 数值平衡口径（承接 M3.7 用户实测「第三波就死」的教训：先算再写）：
//   · 低频高价值触发（随从死亡、破甲）→ 单次给足（6 / 8）
//   · 高频触发（挨打、弃牌、打攻击牌）→ 单次压低（3 / 2 / 3）
//   · 消耗线（雪崩 5）介于两者之间：消耗本身要付出资源，值得中等回报
// ============================================================================

/// <summary>
/// 触发式能力牌的公共骨架：打出 = 给自己挂一层对应能力（`StackType.Counter` → 可叠加）。
///
/// ★ `CardType.Power`：打出后能力常驻本场战斗，符合尖塔"能力牌"的既有语义。
/// </summary>
public abstract class FrostTriggerCardBase<TPower> : CardModel, IFrostCardArt
    where TPower : PowerModel
{
    protected FrostTriggerCardBase(int cost, CardRarity rarity)
        : base(cost, CardType.Power, rarity, TargetType.None)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;
        await PowerCmd.Apply<TPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级语义留空（M3.5 起全项目统一：升级 = 数值成长，触发式牌的成长走层数叠加）。
    }
}

// ============================================================================
//  一、殉爆线 —— 我方随从被摧毁时触发
// ============================================================================

/// <summary>遗霜（1 费 · 能力）：我方随从被摧毁时，对全体敌人造成 6×层数 伤害。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class LastFrostCard : FrostTriggerCardBase<LastFrostPower>
{
    public LastFrostCard() : base(1, CardRarity.Common) { }
}

/// <summary>霜之遗赠（1 费 · 能力）：我方随从被摧毁时，获得 8×层数 格挡。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostLegacyCard : FrostTriggerCardBase<FrostLegacyPower>
{
    public FrostLegacyCard() : base(1, CardRarity.Common) { }
}

/// <summary>
/// 霜葬（0 费 · 技能 · 消耗）：**主动**摧毁我方生命最低的随从，对全体敌人造成 14 点伤害。
///
/// ★ 它是「遗霜」的**主动扳机** —— 不用等敌人打掉你的墙，你自己拆墙换输出。
///   这就是《凛冬前线》"防御单位只活一回合"在尖塔里的正确译法：
///   随从不是资产，是**可以主动引爆的弹药**。
///
/// ★ 为什么挑「生命最低的」而不是随机的或最强的：
///   · 无随机 → 联机各端结果一致（不用处理 `Rng` 同步）
///   · 玩家**可预期** → 能规划"先把残血墙炸掉"
///   · 语义上"先让快倒的倒下"最符合直觉
///
/// ★ 顺序：先 `Kill`（会正常走死亡流程 → 遗霜/遗赠都会触发），再结算本卡的 14 点。
/// ⇒ 有遗霜时这一张牌 = 6 + 14 = 20 点全体爆发，0 费。这是殉爆流的终端 combo。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostBurialCard : CardModel, IFrostCardArt
{
    public const int Damage = 14;

    public FrostBurialCard() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.None) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords
        => new[] { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player?.Creature == null) return;

        var pets = player.PlayerCombatState?.Pets;
        Creature? victim = null;
        if (pets != null)
        {
            foreach (var pet in pets)
            {
                if (pet == null || !pet.IsAlive) continue;
                if (victim == null || pet.CurrentHp < victim.CurrentHp) victim = pet;
            }
        }

        if (victim != null)
        {
            await CreatureCmd.Kill(victim);
            Entry.Logger.Info($"凛冬尖塔：霜葬 —— 引爆 {victim.Name}。");
        }

        var enemies = player.Creature.CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        var alive = new List<Creature>();
        foreach (var e in enemies)
            if (e != null && e.IsAlive) alive.Add(e);
        if (alive.Count == 0) return;

        await CreatureCmd.Damage(choiceContext, alive, Damage, ValueProp.Unpowered, player.Creature);

        Entry.Toast(victim == null
            ? $"霜葬：全场 -{Damage}（场上无随从可引爆）。"
            : $"霜葬：{victim.Name} 炸裂，全场 -{Damage}。", "凛冬尖塔");
    }

    protected override void OnUpgrade() { }
}

// ============================================================================
//  二、消耗线 —— 我的牌被消耗时触发（含两张发动机）
// ============================================================================

/// <summary>雪崩（1 费 · 能力）：每当一张我的牌被消耗时，对全体敌人造成 5×层数 伤害。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class AvalancheCard : FrostTriggerCardBase<AvalanchePower>
{
    public AvalancheCard() : base(1, CardRarity.Common) { }
}

/// <summary>余烬（1 费 · 能力）：每当一张我的牌被消耗时，抽 1×层数 张牌。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class WinterEmberCard : FrostTriggerCardBase<WinterEmberPower>
{
    public WinterEmberCard() : base(1, CardRarity.Uncommon) { }
}

/// <summary>
/// 霜燃（1 费 · 技能 · 消耗）：抽 2 张牌。
///
/// ★ 这是消耗流的**发动机**，不是一张普通过牌：
///   挂上「余烬」后 → 打出即消耗 → 余烬抽 1 → 净抽 1 张（1 费换 1 张牌 + 触发雪崩）；
///   挂上「雪崩」后 → 顺带全体 -5。
///   ⇒ 单卡看很平庸，配齐了就是一个能自循环的引擎 —— 这正是"触发式卡组"该有的样子。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostBrandCard : CardModel, IFrostCardArt
{
    public const int DrawCount = 2;

    public FrostBrandCard() : base(1, CardType.Skill, CardRarity.Common, TargetType.None) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords
        => new[] { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player == null) return;
        await CardPileCmd.Draw(choiceContext, DrawCount, player);
    }

    protected override void OnUpgrade() { }
}

/// <summary>
/// 冰棱（0 费 · 攻击 · 消耗）：造成 7 点伤害。
///
/// ★ 第二台发动机，且是**攻击牌** → 顺带触发「霜之回响」的全体 3 点。
///   0 费 + 消耗 = 触发式卡组里最便宜的"点火器"：
///   一次点火同时喂给「雪崩（消耗）」和「霜之回响（攻击牌）」两条线。
/// </summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class IceShardCard : CardModel, IFrostCardArt
{
    public IceShardCard() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars
        => new DynamicVar[] { new DamageVar(7m, ValueProp.Move) };

    public override IEnumerable<CardKeyword> CanonicalKeywords
        => new[] { CardKeyword.Exhaust };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        System.ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
            .Targeting(cardPlay.Target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

// ============================================================================
//  三、受击 / 破甲线
// ============================================================================

/// <summary>凛冬之怒（2 费 · 能力）：我方随从受到伤害时，对全体敌人造成 3×层数 伤害。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class WinterWrathCard : FrostTriggerCardBase<WinterWrathPower>
{
    public WinterWrathCard() : base(2, CardRarity.Rare) { }
}

/// <summary>碎冰（1 费 · 能力）：每当一名敌人的格挡被打破时，对它造成 8×层数 伤害。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class IceShatterCard : FrostTriggerCardBase<IceShatterPower>
{
    public IceShatterCard() : base(1, CardRarity.Uncommon) { }
}

// ============================================================================
//  四、牌流动线
// ============================================================================

/// <summary>寒风（1 费 · 能力）：每当你弃一张牌时，对全体敌人造成 2×层数 伤害。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class ColdWindCard : FrostTriggerCardBase<ColdWindPower>
{
    public ColdWindCard() : base(1, CardRarity.Common) { }
}

/// <summary>霜之回响（2 费 · 能力）：每当你打出一张攻击牌时，对全体敌人造成 3×层数 伤害。</summary>
[RegisterCard(typeof(FrostCardPool))]
public sealed class FrostEchoCard : FrostTriggerCardBase<FrostEchoPower>
{
    public FrostEchoCard() : base(2, CardRarity.Rare) { }
}
