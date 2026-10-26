using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MinionLib.Minion;
using MinionLib.Action;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;

namespace FrostSpire.Monsters;

/// <summary>
/// 凛冬尖塔的「单位」基类（M3.3）—— 会在场上**主动攻击**的我方生物。
///
/// 与 <see cref="FrostBuildingMonster"/> 的区别：
///   · 建筑：不动、不攻击，纯站桩（只挨打 / 只挂工事效果）
///   · 单位（本类）：有自己的怪 AI，会照 AI 自动协战（民兵走这条）
///
/// 同样继承 `MinionModel` —— 理由见 <see cref="FrostBuildingMonster"/> 的类注释。
/// </summary>
public abstract class FrostUnitMonster : MinionModel
{
    public override int MinInitialHp => MaxInitialHp;

    public override async Task OnSummon(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options)
    {
        await ApplyActionOnSummon(owner);
    }

    protected virtual Task ApplyActionOnSummon(Player owner) => Task.CompletedTask;

    protected async Task ApplyAction<TAction>(Player owner, int uses = 1) where TAction : ActionModel
    {
        var creature = Creature;
        if (creature == null) return;
        await PowerCmd.Apply<TAction>(
            new ThrowingPlayerChoiceContext(), creature, uses, owner.Creature, null, silent: false);
    }
}

/// <summary>
/// 民兵（M3.3）：6 血，站 Back 位。
/// ★ M3.4 行动「突击」：点民兵 → 选一个敌人 → 造成 8 点伤害（每回合 1 次）。
///   这是唯一**需要选目标**的行动，让玩家有"操作自己人"的手感（邦布同款）。
///
/// ★★ M3.8 重要修正：**民兵的「每回合自动攻击」原来是假的**。
///   老实现覆写了 `GenerateMoveStateMachine()`，声明一个
///   `MoveState("FROST_MILITIA_ATTACK", AttackMove, new SingleAttackIntent(5))`，
///   但 `AttackMove` 是 `Task.CompletedTask` —— **只有"5 点伤害"的意图图标，实际打 0 伤害**。
///   卡面写着「每回合自动攻击」，玩家看不出问题，但输出一直是零。
///
///   为什么不能直接补 `DamageCmd.Attack(...).FromMonster(this)`（**千万别写**）：
///     反编译 `AttackCommand.GetTargets()` 有硬编码分支
///     `if (_sourceType == SourceType.Monster) return _combatState.PlayerCreatures;`
///     —— 它**不看攻击者在哪一侧**。民兵在**玩家侧**，用 `FromMonster` 会**打自己人**。
///
///   所以现在**不覆写 move 状态机**（继承 `MinionModel` 的待机循环，与官方随从
///   `Osty` 的 `NOTHING_MOVE` 同理），自动攻击改由
///   <see cref="Powers.MilitiaAttackPower"/> 在玩家回合开始触发 ——
///   和箭塔/冰墙/篝火同一套已验证机制，且能随「重复部署 = 强化」叠层。
///
/// 外观借本体 `creature_visuals/leaf_slime_m`（已核实存在于 SlayTheSpire2.pck）。
/// </summary>
public sealed class MilitiaM : FrostUnitMonster
{
    public override int MaxInitialHp => 6;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/leaf_slime_m");

    /// <summary>只挂可点击行动「突击」；自动攻击的光环由卡（`SummonMilitiaCard`）挂。</summary>
    protected override Task ApplyActionOnSummon(Player owner) => ApplyAction<Actions.MilitiaChargeAction>(owner);
}
