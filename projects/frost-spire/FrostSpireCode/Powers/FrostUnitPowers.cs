using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace FrostSpire.Powers;

// ============================================================================
// 凛冬单位 · 「单位自动攻击」光环（M3.8）
//
// ★ 为什么另起一个文件、而不是继续用怪物的 move 状态机：
//
//   民兵原来在 `MilitiaM.GenerateMoveStateMachine()` 里声明了
//   `new MoveState("FROST_MILITIA_ATTACK", AttackMove, new SingleAttackIntent(5))`，
//   但 `AttackMove` 的实现是 `Task.CompletedTask` —— **只有意图图标，没有任何伤害**。
//   也就是说卡面写着「每回合自动攻击」，实际民兵每回合打 0。
//
//   ★★ 为什么不能简单补上 `DamageCmd.Attack(...).FromMonster(this)`（这是**坑，千万别写**）：
//     反编译 `AttackCommand.GetTargets()` 原文：
//         if (_sourceType == SourceType.Monster)
//             return _combatState.PlayerCreatures;      // ← 硬编码：怪物打「玩家侧」
//     这个分支**不看攻击者在哪一侧**。民兵是**玩家侧**的随从，
//     用 `FromMonster` 会让它**打自己人**（玩家本体 + 自己所有建筑）。
//
//   ★ 官方自己怎么做的：本体缚灵师的随从 `Osty` 的 move 状态机是
//     `NOTHING_MOVE = Task.CompletedTask` —— **官方随从根本不靠状态机攻击**，
//     它靠卡牌触发（`AttackCommand.FromOsty(osty, card, cardPlay)`）。
//
//   ⇒ 我们的结论：**别碰 move 状态机，改用已经在本项目跑通的光环机制**
//     （`BeforeSideTurnStart`，箭塔 `ArrowTowerPower` / 冰墙 `IceWallPower` / 篝火
//     `CampfirePower` 全是这条路，M3.1 起实测可用）。
//     顺带白拿一个好处：**光环按层数叠加** → 「重复部署 = 强化」时民兵每回合多打 5 点。
// ============================================================================

/// <summary>
/// 民兵 · 自动攻击：每回合开始时，对**最前面的敌人**造成 `5 × 层数` 点伤害。
///
/// ★ 为什么打"最前面那个"而不是全体：箭塔（`ArrowTowerPower`）已经是全体 AOE 了，
///   民兵作为「单位」该有区分度 —— 它是**单点输出**，配合它自己的可点击行动「突击」（单体 8）
///   走同一个定位。用 `enemies[0]` 而不是随机，是为了**无随机、可预期**
///   （也省掉 `Rng` 的联机一致性处理）。
/// </summary>
[RegisterPower]
public sealed class MilitiaAttackPower : FrostBuildingPowerBase
{
    public const int DamagePerTurn = 5;

    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!IsMyTurnStart(this, side) || Amount <= 0) return;

        var enemies = combatState.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        var target = enemies[0];
        if (target == null || !target.IsAlive) return;

        // ★ M3.11：显式加上民兵自己的力量（寒冰王座给的 +3 力量在这里才真的兑现）。
        await CreatureCmd.Damage(choiceContext, target, AddStrength(this, Amount * DamagePerTurn), ValueProp.Unpowered, Owner);
    }
}
