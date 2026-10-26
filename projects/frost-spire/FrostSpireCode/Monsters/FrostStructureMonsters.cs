using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;

namespace FrostSpire.Monsters;

// ============================================================================
// 凛冬工事 · 扩充建筑（M3.6 —— 卡池扩充）
//
// 基类见 FrostBuildingMonster.cs（继承 MinionLib 的 MinionModel，
// 白拿站位 / 替伤 / 死亡清理 / 待机 AI / 可点击行动）。
//
// ★ 视觉一律「借本体已核实的场景」—— 挑之前必须用
//   `tools/check_creature_visuals.py` 确认该场景**根节点挂了 NCreatureVisuals**，
//   光看"文件存在"会踩 `battleworn_dummy` 那个坑（详见 FrostBuildingMonster.cs 的长注释）。
//
//   本文 6 座建筑全部**按上述方法核实过**（126 个生物场景，可用 123 / 不可用 3），
//   视觉路径可直接用：
//     gas_bomb / turret_operator / punch_construct / calcified_cultist / infested_prism / ceremonial_beast
//
// ★★ M3.7 重构：**光环不再在这里挂，改到卡上挂**。
//
//   原因 —— 「重复部署 = 强化」（用户要求「然后重复部署给强化吧」）的实现方式：
//     重复部署时**不召唤新建筑**，而是把已有那座练强，然后让**卡片的 `AfterSummonAsync` 照常跑**。
//     光环（篝火回血 / 箭塔伤害 / 蓄力…）都是**按层数叠加**的 → 重打一次 = 光环 +1 层。
//
//   所以分工是：
//     · 建筑自己的 `OnSummon`  → 只挂**可点击行动**（只会被挂一次，重打不该再挂）
//     · 卡片的 `AfterSummonAsync` → 挂**光环**（每次打出都跑，所以能叠层 = 强化）
//
//   ⚠️ 别把光环挪回 `OnSummon`：那样重复部署就只有 +3 血，没有光环成长，
//      "强化"会变得没手感。
// ============================================================================

/// <summary>
/// 篝火（0 费 / Size 1 / 后排）：5 血。每回合给主人回 2 血。
/// 视觉借 `gas_bomb`（有火焰感）。**最便宜的建筑**——免费铺场，但很脆。
/// </summary>
public sealed class CampfireM : FrostBuildingMonster
{
    public override int MaxInitialHp => 5;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/gas_bomb");

    // 无行动：光环（CampfireAuraPower）由 CampfireCard.AfterSummonAsync 挂。
    protected override Task ApplyActionOnSummon(Player owner) => Task.CompletedTask;
}

/// <summary>
/// 瞭望台（1 费 / Size 1 / 后排）：4 血。每回合让主人多抽 1 张牌。
/// 视觉借 `turret_operator`。**过牌引擎**——手牌越厚，部署牌越不容易卡。
/// </summary>
public sealed class WatchtowerM : FrostBuildingMonster
{
    public override int MaxInitialHp => 4;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/turret_operator");

    protected override Task ApplyActionOnSummon(Player owner) => Task.CompletedTask;
}

/// <summary>
/// 符文壁（2 费 / Size 1 / 前排）：8 血。**受普通攻击时伤害减半**。
/// 视觉借 `punch_construct`（方正的石墙感）。
/// ⇒ 8 血 + 减半 ≈ 等效 16 血，是**最耐揍的前排**（比冰垒的 6 血硬得多）。
/// </summary>
public sealed class RuneWallM : FrostBuildingMonster
{
    public override int MaxInitialHp => 8;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/punch_construct");

    protected override Task ApplyActionOnSummon(Player owner) => Task.CompletedTask;
}

/// <summary>
/// 寒霜法师（2 费 / Size 1 / 后排）：4 血。**控制位**。
///   · 被动：每回合给随机一个敌人挂 1 层虚弱（-25% 攻击）
///   · 主动「冰封」：点它 → 选一个敌人 → **让它跳过下一回合**
/// 视觉借 `calcified_cultist`（法师感）。
/// </summary>
public sealed class FrostMageM : FrostBuildingMonster
{
    public override int MaxInitialHp => 4;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/calcified_cultist");

    /// <summary>只挂行动（光环由卡挂）。行动不会被重复挂 —— 重打时建筑是"强化"而不是"重新召唤"。</summary>
    protected override Task ApplyActionOnSummon(Player owner)
        => ApplyAction<Actions.FrostMageFreezeAction>(owner);
}

/// <summary>
/// 霜爆塔（3 费 / Size 2 / 后排）：6 血。**蓄力爆发位**。
///   · 被动：每回合给自己 +4 层蓄力（层数 = 攒下的伤害）
///   · 主动「引爆」：点它 → 对全体敌人造成（蓄力值）伤害 → 蓄力清零
/// 视觉借 `infested_prism`（棱镜 = 冰晶感）。
/// ⇒ 攒 3 回合 = 12 点全体伤害。**越晚引爆越痛，但要赌它不被拆。**
/// </summary>
public sealed class FrostBurstTowerM : FrostBuildingMonster
{
    public override int MaxInitialHp => 6;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/infested_prism");

    protected override Task ApplyActionOnSummon(Player owner)
        => ApplyAction<Actions.FrostBurstDetonateAction>(owner);
}

/// <summary>
/// 寒冰王座（4 费 / Size 5 / 后排）：14 血。**巨兽级 —— 一张占满容量**。
///   · 部署时：全体友军 +3 力量（爆发式强化，见卡里实现）
///   · 每回合：给主人 +2 格挡
/// 视觉借 `ceremonial_beast`（仪式巨兽）。
/// ⇒ 「养大哥」的核心：把资源全押在它身上，用光环把整条战线拉起来。
/// </summary>
public sealed class FrostThroneM : FrostBuildingMonster
{
    public override int MaxInitialHp => 14;

    protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/ceremonial_beast");

    protected override Task ApplyActionOnSummon(Player owner) => Task.CompletedTask;
}
