using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MinionLib.Minion;

namespace FrostSpire.Monsters;

/// <summary>
/// 凛冬工事的「建筑生物」基类（M3.3，已改走 MinionLib）。
///
/// ★ 为什么必须继承 `MinionModel` 而不是裸 `MonsterModel`（M3.2 的真实教训）：
///   本体 `DieForYouPower` 的判定是 `target != Owner.PetOwner?.Creature`，
///   而 `Creature.Pets => Player?.PlayerCombatState?.Pets`。
///   → 用 `CreatureCmd.Add` 直接加进战斗的「裸生物」**既没有 PetOwner 也不在 Pets 列表里**，
///     替伤逻辑全部静默失效（不报错、不生效 —— 最难查的那种）。
///   → 必须用 `PlayerCmd.AddPet`（MinionLib 的 `MinionCmd.AddMinion` 内部就是调它），
///     生物才会挂上 PetOwner、进入 `PlayerCombatState.Pets`，替伤/站位/排布才成立。
///
/// 继承 `MinionModel` 顺带白拿（全部来自 MinionLib，零自研）：
///   · 固定站位 `Position`（Front 才挡刀，见 `MinionGuardianPower`）
///   · `NCombatRoom.AddCreature` 的 Patch：自动把建筑节点挪到站位、随主人移动
///   · 死亡后的战斗清理 patch（`MinionKillPatch`）
///   · 默认死亡音效（`DeathSfx` / `HasDeathSfx`）
///   · 待机行为（`GenerateMoveStateMachine` 已实现：单一 MINION_IDLE 状态自循环 → 永远不动、不攻击）
///   · ★ M3.4 新增：**可点击行动**（`MinionLib.Action.ActionModel`，见 Actions/FrostActions.cs）
///
/// 我们只需覆写血量、外观、名字，以及在 OnSummon 里挂上行动。
/// </summary>
public abstract class FrostBuildingMonster : MinionModel
{
    /// <summary>建筑血量（子类覆写 MaxInitialHp 即可，Min 自动跟随）。</summary>
    public override int MinInitialHp => MaxInitialHp;

    // v1 借训练假人的本地化名（零新素材）；FrostLoc 会覆盖成真名。
    public override LocString Title => MonsterModel.L10NMonsterLookup("BIG_DUMMY.name");

    /// <summary>
    /// 召唤时挂「可点击行动」。子类覆写。
    /// ★ 用 `ThrowingPlayerChoiceContext`（sts2 提供）—— OnSummon 阶段没有玩家选择上下文。
    /// </summary>
    public override async Task OnSummon(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options)
    {
        await ApplyActionOnSummon(owner);
    }

    /// <summary>给自己挂上这个建筑的行动（子类实现）。</summary>
    protected virtual Task ApplyActionOnSummon(Player owner) => Task.CompletedTask;

    /// <summary>便捷：把行动挂到自己身上。层数 = 每回合可用次数。</summary>
    protected async Task ApplyAction<TAction>(Player owner, int uses = 1) where TAction : MinionLib.Action.ActionModel
    {
        var creature = Creature;
        if (creature == null) return;
        await PowerCmd.Apply<TAction>(
            new ThrowingPlayerChoiceContext(), creature, uses, owner.Creature, null, silent: false);
    }

    /// <summary>
    /// 便捷：把**普通能力**挂到自己身上（M3.6 新增，和 <see cref="ApplyAction"/> 对称）。
    ///
    /// 用途：建筑自带的光环类能力（篝火回血、瞭望台抽牌、霜爆塔蓄力…）。
    /// ★ 和 ApplyAction 的区别只是**泛型约束**：ActionModel 要求"可点击"，
    ///   PowerModel 只要是能力就行（自动触发的那些走这里）。
    /// </summary>
    protected async Task ApplyPower<TPower>(Player owner, int amount = 1) where TPower : PowerModel
    {
        var creature = Creature;
        if (creature == null) return;
        await PowerCmd.Apply<TPower>(
            new ThrowingPlayerChoiceContext(), creature, amount, owner.Creature, null, silent: false);
    }
}

/// <summary>
/// 冰垒：6 血，替玩家挡普通攻击（Front 位）。
/// ★ M3.4 行动「举盾」：点冰垒 → 全体我方随从 +3 格挡（每回合 1 次）。
///
/// ★★ 外观路径的坑（**两轮才查清，务必别再试错**）：
///
///   ① 原写 `creature_visuals/battleworn_dummy` —— 这个场景**在 pck 里是存在的**（3194 字节），
///      但它的根节点**没挂 `NCreatureVisuals` 脚本**（它的第一个 ext_resource 是 Spine 骨骼数据）。
///      ⇒ `PreloadManager.Cache.GetScene(path).Instantiate&lt;NCreatureVisuals&gt;()` 抛
///        `InvalidCastException: Unable to cast 'Godot.Node2D' to 'NCreatureVisuals'` → 回退 error 场景。
///      ★ 教训：「场景存在」≠「能当生物视觉」——**路径对不代表类型对**。
///
///   ② 二改 `creature_visuals/big_dummy` —— 名字是从本体 `BigDummy` 类推的，但**该场景根本不存在**：
///      `BigDummy` 自己覆写了 `VisualsPath => SceneHelper.GetScenePath("creature_visuals/defect")`。
///      ⇒ 「默认路径 = `creature_visuals/&lt;怪物ID小写&gt;`」这条规则**只对没覆写 VisualsPath 的怪成立**，
///        拿类名直接拼路径会踩空。（这是上一版注释里的错误推断，已纠正。）
///
///   ③ 正确做法：挑一个**根节点确实挂了 `NCreatureVisuals`** 的场景。
///      **核实方法（两条工具，本仓库自带）**：
///        · `tools/list_pck_scenes.py &lt;pck&gt; creature_visuals/` —— 列出本体所有生物视觉场景（127 个）；
///        · `tools/extract_pck_file.py &lt;pck&gt; &lt;资源路径&gt; out.tscn` —— 把场景抠出来，
///          看首行 ext_resource 里有没有 `path="res://src/Core/Nodes/Combat/NCreatureVisuals.cs"`。
///      冰垒选 `cubex_construct`（方块构造体，堡垒感），**已按上述方法核实 ✓**。
/// </summary>
public sealed class IceBulwarkM : FrostBuildingMonster
{
    public override int MaxInitialHp => 6;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/cubex_construct");

    protected override Task ApplyActionOnSummon(Player owner) => ApplyAction<Actions.IceBulwarkGuardAction>(owner);
}

/// <summary>
/// 箭塔：4 血，每回合自动对全部敌人开火（Back 位）。
/// ★ M3.4 行动「齐射」：点箭塔 → 全体敌人 -6（每回合 1 次）。
/// 外观借本体 `creature_visuals/guardbot`（守卫机器人，炮台感）——
/// 已按 `IceBulwarkM` 上方的核实方法确认：场景存在 + 根节点挂 `NCreatureVisuals` ✓
/// （游戏日志里对它只有 `Asset not cached` 的 WARN —— 那只是「没预加载」，即时加载即可，不是错误）。
/// </summary>
public sealed class ArrowTowerM : FrostBuildingMonster
{
    public override int MaxInitialHp => 4;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/guardbot");

    protected override Task ApplyActionOnSummon(Player owner) => ApplyAction<Actions.ArrowTowerVolleyAction>(owner);
}
