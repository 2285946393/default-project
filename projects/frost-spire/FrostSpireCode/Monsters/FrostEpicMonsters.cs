using MinionLib.Minion;
using MegaCrit.Sts2.Core.Helpers;

namespace FrostSpire.Monsters;

// ============================================================================
// 凛冬「精锐」单位（M3.12）
//
// ★ 为什么做这一批 —— 补《凛冬前线》五大类的最后一块：
//   攻击 ✓ / 防御 ✓ / 技能 ✓ / 建筑 ✓ / **精锐（可成长领袖）** ← 之前基本是空的
//
//   《凛冬前线》里精锐的特色是**词条**，而我们一个都没做：
//     · **连击** —— 前进时攻击 2 次
//     · **固守** —— +1 在场时间
//     · **不朽** —— 回合结束不离场
//
// ★★ 直译会水土不服，必须**翻译成尖塔的语境**：
//   尖塔的随从**不会自动离场**，所以「固守 / 不朽」按字面做（延长在场时间）**毫无意义**。
//   ⇒ 重新解释这三个词条在尖塔里"应该是什么意思"：
//     · **连击** = 每回合**真的打两下**（两段独立结算，力量每段都加）
//     · **固守** = 它站着的时候，**全体友军受伤 -1**（把"在场时间"换成"在场价值"）
//     · **不朽** = 每回合结束**回满血**（它打不死，除非一回合内被灌满伤害）
//
// ★ 视觉全部走 `tools/check_creature_visuals.py` 核查过的可用场景
//   （根节点挂 `NCreatureVisuals` 才算可用，否则会回退 error 占位）。
//   已占用：cubex_construct / guardbot / gas_bomb / turret_operator /
//           punch_construct / calcified_cultist / infested_prism /
//           ceremonial_beast / leaf_slime_m —— 本批三个都不冲突。
// ============================================================================

/// <summary>
/// 霜刃卫士（8 血 · 精锐）：**连击** —— 每回合对最前方的敌人**连续攻击 2 次**，每次 5(+力量) 点。
///
/// ★ 为什么是"真打两下"而不是"一次打 10"：
///   连击的爽感来自**两段独立结算** —— 力量每段都加、挨打的目标会掉两次血条。
///   一次打 10 在数值上等价，但手感完全不同（而且易伤/破甲类效果也会只算一次）。
/// </summary>
public sealed class FrostBladeM : FrostUnitMonster
{
    public override int MaxInitialHp => 8;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/spectral_knight");
}

/// <summary>
/// 不冻者（10 血 · 精锐）：**不朽** —— 每回合结束**回满生命**；每回合攻击 1 次 6(+力量) 点。
///
/// ★ 「不朽」在尖塔里最贴切的译法就是"打不死"：
///   它不免疫伤害，但每个回合末都会满血站起来。
///   ⇒ 敌人想杀它，必须**在一个回合内**打穿 10 点 —— 这就是它的强度也是它的弱点：
///     遇到高爆发（GlobeHead 的 16 伤）照样会倒，遇到持续消耗则永远打不死。
///   ⇒ 10 血这个数字就是它的平衡点：太低则随便死，太高则真无敌。
/// </summary>
public sealed class UndyingFrostM : FrostUnitMonster
{
    public override int MaxInitialHp => 10;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/the_lost");
}

/// <summary>
/// 凛冬守望者（12 血 · 精锐）：**固守** —— 它存活期间，**全体友军受到的伤害 -1×层数**。
///
/// ★ 「固守」原意是"+1 在场时间"，但尖塔随从本来就不会离场，直译是**空效果**。
///   ⇒ 改成「它站着就有价值」：一个**团队减伤光环**。
///     这既保留了"固守 = 守住整条战线"的原意，又在尖塔里真的有用。
///
/// ★ -1 为什么够：本体怪的单次伤害普遍是 3~16，
///   -1 对多段攻击（WhipSlap 3×2、Whirlwind 2×3）收益最大 —— 正好克制杂鱼海。
/// </summary>
public sealed class FrostWardenM : FrostBuildingMonster
{
    public override int MaxInitialHp => 12;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/waterfall_giant");
}
