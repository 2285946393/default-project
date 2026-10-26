using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Models;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace FrostSpire;

// ============================================================================
// 让「纯 dll 的 mod」也能给自己造的内容起名字（quest-spire LocInjector 的凛冬版）。
//
// 机制（详见 quest-spire/QuestSpireCode/LocInjector.cs 的完整注释）：
//   user://localization_override/<语言>/<表名>.json 会在启动时**合并**进本地化表，
//   不要求 key 已存在；mod 初始化早于本地化加载 → 当次启动就生效。
//
// ★ key 形态（由 RitsuLib 的注册规则决定，已从 RitsuLib 官方 XML 文档核实）：
//     默认条目规则 = <MOD>_<CATEGORY>_<CLR类型名>
//     例：FrostSpire 的 IceBulwarkCard → 条目 FROST_SPIRE_CARD_ICE_BULWARK_CARD
//   表名 = 类别名小写（卡 = "cards" / 能力 = "powers" / 遗物 = "relics"）
//   最终 key = "<条目>.title" / "<条目>.description" / "<条目>.flavor"
//   游戏报错时显示的 `cards.FROST_SPIRE_CARD_XXX.description` 就是这个 key。
//
// 安全规矩（继承自 quest-spire 踩坑史）：
//   先读再合并、绝不覆盖玩家自己的文件；解析不了整个跳过；文本里不出现裸 { }。
// ============================================================================
public static class FrostLoc
{
    /// <summary>
    /// 卡牌文本。字段顺序：CLR 类型名 → 中文(名/描述/风味) → 英文(名/描述/风味)。
    /// ★ 新增卡牌时**必须**在这里补一条，否则卡面会显示
    ///   `cards.FROST_SPIRE_CARD_XXX.description` 这种原始 key（M3.3 实测踩过）。
    /// </summary>
    private static readonly (System.Type type, string zhTitle, string zhDesc, string zhFlavor,
        string enTitle, string enDesc, string enFlavor)[] Cards =
    {
        (typeof(Cards.SummonMilitiaCard),
            "征召民兵", "部署一名 6 血民兵到【后排】。每回合自动攻击最前方的敌人 5 点；点它可【突击】一名敌人 8 点。已有民兵时改为强化：+3 生命上限，且每回合多打 5 点。",
            "凛冬的墙，是用人心砌的。",
            "Conscript Militia", "Deploy a 6 HP Militia to your [Back] row. Each turn it attacks the frontmost enemy for 5; click it to [Charge] an enemy for 8. If you already have one: strengthen it (+3 Max HP, +5 damage per turn).",
            "Walls of winter are built from hearts."),

        // ★ M3.8 基础卡（攻击 / 防御）—— 纯凛冬起始卡组的骨架，对标本体 Strike / Defend。
        (typeof(Cards.FrostStrikeCard),
            "凛冬打击", "造成 6 点伤害。",
            "第一刀不华丽，但决定谁站着。",
            "Frost Strike", "Deal 6 damage.",
            "The first blow is not pretty. It decides who stands."),

        (typeof(Cards.FrostDefendCard),
            "凛冬壁垒", "获得 5 点格挡。",
            "雪落下来，就是墙。",
            "Frost Guard", "Gain 5 Block.",
            "When the snow falls, it becomes a wall."),

        (typeof(Cards.IceBulwarkCard),
            "冰垒（前排）", "部署一座 6 血冰垒到【前排】，替你承受普通攻击的伤害。",
            "冰墙不为好看，只为挡下第一刀。",
            "Ice Bulwark (Front)", "Deploy a 6 HP Bulwark to your [Front] row; it takes ordinary attacks in your place.",
            "Ice walls exist to stop the first blow."),

        (typeof(Cards.IceBulwarkBackCard),
            "冰垒（后排）", "部署一座 6 血冰垒到【后排】，每回合为你补充 3 点格挡（不替你挡伤）。",
            "站在后面，它砌的是墙，不是盾。",
            "Ice Bulwark (Back)", "Deploy a 6 HP Bulwark to your [Back] row; each turn it grants you 3 Block (does not body-block).",
            "From the back, it builds walls, not shields."),

        (typeof(Cards.ArrowTowerCard),
            "箭塔", "部署一座 4 血箭塔到【后排】，每回合对全部敌人造成 4(+力量) 点伤害。",
            "它不说话，只放箭。",
            "Arrow Tower", "Deploy a 4 HP Arrow Tower to your [Back] row; each turn it deals 4 (+Strength) damage to ALL enemies.",
            "It does not speak. It looses."),

        (typeof(Portal.OpenFrostRiftCard),
            "开启凛冬裂隙", "在地图上使用：进入凛冬裂隙 —— 一张独立的小地图，打完即可返回原路线。",
            "门后不是别处，是另一个冬天。",
            "Open the Frost Rift", "Use on the map: enter the Frost Rift, a separate map. Clear it and return to your original path.",
            "Behind the door is not elsewhere. It is another winter."),

        (typeof(Cards.IceWallBuildingCard),
            "冰墙工事", "获得能力：每回合开始获得 3 点格挡。可重复获得以叠加。",
            "一块砖挡不住什么，一万块可以。",
            "Ice Wall Works", "Gain: at the start of each turn, gain 3 Block. Stackable.",
            "One brick stops nothing. Ten thousand stop everything."),

        (typeof(Cards.ArrowTowerBuildingCard),
            "箭塔工事", "获得能力：每回合对全部敌人造成 4 点伤害。可重复获得以叠加。",
            "塔上的弓手从不休息。",
            "Arrow Tower Works", "Gain: each turn, deal 4 damage to ALL enemies. Stackable.",
            "The archer on the tower never rests."),

        (typeof(Cards.CampfireBuildingCard),
            "篝火工事", "获得能力：每回合回复 2 点生命。可重复获得以叠加。",
            "火不灭，人就能撑到天亮。",
            "Campfire Works", "Gain: each turn, heal 2 HP. Stackable.",
            "As long as the fire burns, you last till dawn."),

        (typeof(Cards.WatchtowerBuildingCard),
            "瞭望台工事", "获得能力：每回合多抽 1 张牌。可重复获得以叠加。",
            "站得高，才看得见敌人从哪来。",
            "Watchtower Works", "Gain: draw 1 additional card each turn. Stackable.",
            "Only from up high can you see where they come from."),

        // ====================================================================
        // ★ M3.6 扩充卡池（8 张）—— 对应 FrostStructureCards.cs
        //
        // 注意别和上面四张「工事牌」混淆（IceWallBuildingCard 等）：
        //   工事牌 = 挂**能力**在玩家身上（不召唤任何东西）；
        //   下面这批 = 召唤**一座建筑**到场上（占容量、会被打掉）。
        // ====================================================================

        (typeof(Cards.CampfireCard),
            "篝火", "部署一座 5 血篝火到【后排】。每回合回复 2 点生命。",
            "最便宜的火，往往救最贵的命。",
            "Campfire", "Deploy a 5 HP Campfire to your [Back] row. Each turn, heal 2 HP.",
            "The cheapest fire saves the dearest life."),

        (typeof(Cards.WatchtowerCard),
            "瞭望台", "部署一座 4 血瞭望台到【后排】。每回合多抽 1 张牌。",
            "先看见的人，先出牌。",
            "Watchtower", "Deploy a 4 HP Watchtower to your [Back] row. Draw 1 additional card each turn.",
            "Whoever sees first, plays first."),

        (typeof(Cards.RuneWallCard),
            "符文壁", "部署一座 8 血符文壁到【前排】。它受到的普通攻击伤害减半。",
            "刻在石头上的字，比刀更硬。",
            "Rune Wall", "Deploy an 8 HP Rune Wall to your [Front] row. Ordinary attacks against it are halved.",
            "Words carved in stone outlast the blade."),

        (typeof(Cards.FrostMageCard),
            "寒霜法师", "部署一名 4 血寒霜法师到【后排】。每回合给随机一名敌人挂 1 层虚弱；点它可【冰封】一名敌人（跳过其下一回合）。",
            "她念的不是咒，是天气。",
            "Frost Mage", "Deploy a 4 HP Frost Mage to your [Back] row. Each turn, apply 1 Weak to a random enemy; click it to [Freeze] an enemy, making them skip their next turn.",
            "She does not chant spells. She chants weather."),

        (typeof(Cards.FrostBurstTowerCard),
            "霜爆塔", "部署一座 6 血霜爆塔到【后排】。每回合蓄力 +4；点它【引爆】——对全体敌人造成等同于蓄力的伤害，随后清空蓄力。",
            "它把所有冬天都攒在里面。",
            "Frostburst Tower", "Deploy a 6 HP Frostburst Tower to your [Back] row. It gains 4 Charge each turn; click it to [Detonate] — deal its Charge as damage to ALL enemies, then reset.",
            "It keeps every winter inside."),

        (typeof(Cards.FrostThroneCard),
            "寒冰王座", "部署一座 14 血寒冰王座到【后排】。部署时全体友军 +3 力量（力量会加在他们的攻击与主动技上）；每回合给你 +2 格挡。",
            "坐上它的人，整个冬天都会替他作战。",
            "Frost Throne", "Deploy a 14 HP Frost Throne to your [Back] row. On deploy: all your minions gain +3 Strength (it boosts their attacks and active abilities). Each turn, gain 2 Block.",
            "Seat yourself, and the whole winter fights for you."),

        (typeof(Cards.BlizzardCard),
            "暴风雪", "对全体敌人造成 8 点伤害，并各挂 1 层虚弱。",
            "没有敌人，只有天气。",
            "Blizzard", "Deal 8 damage to ALL enemies and apply 1 Weak to each.",
            "There are no enemies. Only weather."),

        (typeof(Cards.FreezeCard),
            "冰封术", "让一名敌人跳过它的下一个回合。",
            "一秒钟的静止，够改写一场战斗。",
            "Freeze", "An enemy skips its next turn.",
            "A second of stillness rewrites a battle."),

        // ====================================================================
        // ★ M3.9 凛冬「回响」· 触发式效果牌（11 张）—— 对应 FrostTriggerCards.cs
        //
        // ★★ 这批和上面所有牌的根本区别：**它们不是"打出即生效"**，
        //   而是在「某件事发生时」才生效。描述里必须把**触发条件**写清楚，
        //   否则玩家看到一张不立刻做任何事的牌会以为它是坏的。
        // ====================================================================

        (typeof(Cards.LastFrostCard),
            "遗霜", "获得能力：每当你的一个随从被摧毁时，对全体敌人造成 6×层数 点伤害。可重复获得以叠加。",
            "它们倒下的地方，会留下一层霜。",
            "Last Frost", "Gain: whenever one of your minions is destroyed, deal 6x damage to ALL enemies. Stackable.",
            "Where they fall, a layer of frost remains."),

        (typeof(Cards.FrostLegacyCard),
            "霜之遗赠", "获得能力：每当你的一个随从被摧毁时，获得 8×层数 点格挡。可重复获得以叠加。",
            "倒下的人把墙留给了后面的人。",
            "Frost Legacy", "Gain: whenever one of your minions is destroyed, gain 8x Block. Stackable.",
            "The fallen leave their wall to those behind."),

        (typeof(Cards.FrostBurialCard),
            "霜葬", "摧毁你生命最低的一个随从，对全体敌人造成 14 点伤害。消耗。",
            "墙不是用来站的，是用来炸的。",
            "Frost Burial", "Destroy your lowest-HP minion, then deal 14 damage to ALL enemies. Exhaust.",
            "Walls are not for standing on. They are for blowing up."),

        (typeof(Cards.AvalancheCard),
            "雪崩", "获得能力：每当一张你的牌被消耗时，对全体敌人造成 5×层数 点伤害。可重复获得以叠加。",
            "一张牌烧掉，山就塌一寸。",
            "Avalanche", "Gain: whenever one of your cards is Exhausted, deal 5x damage to ALL enemies. Stackable.",
            "Burn a card, and the mountain loses an inch."),

        (typeof(Cards.WinterEmberCard),
            "余烬", "获得能力：每当一张你的牌被消耗时，抽 1×层数 张牌。可重复获得以叠加。",
            "牌烧完了，火还没灭。",
            "Winter Ember", "Gain: whenever one of your cards is Exhausted, draw 1x card. Stackable.",
            "The card is gone. The fire is not."),

        (typeof(Cards.FrostBrandCard),
            "霜燃", "抽 2 张牌。消耗。",
            "烧掉一张，换回一整个冬天。",
            "Frost Brand", "Draw 2 cards. Exhaust.",
            "Burn one card. Buy back a whole winter."),

        (typeof(Cards.IceShardCard),
            "冰棱", "造成 7 点伤害。消耗。",
            "最便宜的武器，往往最先用完。",
            "Ice Shard", "Deal 7 damage. Exhaust.",
            "The cheapest weapon is always spent first."),

        (typeof(Cards.WinterWrathCard),
            "凛冬之怒", "获得能力：每当你的一个随从受到伤害时，对全体敌人造成 3×层数 点伤害。可重复获得以叠加。",
            "打我的墙，就是打冬天本身。",
            "Winter Wrath", "Gain: whenever one of your minions takes damage, deal 3x damage to ALL enemies. Stackable.",
            "Strike my wall, and you strike the winter itself."),

        (typeof(Cards.IceShatterCard),
            "碎冰", "获得能力：每当一名敌人的格挡被打破时，对它造成 8×层数 点伤害。可重复获得以叠加。",
            "壳碎的那一秒，才是真正的开始。",
            "Ice Shatter", "Gain: whenever an enemy's Block is broken, deal 8x damage to that enemy. Stackable.",
            "The second the shell cracks is the real beginning."),

        (typeof(Cards.ColdWindCard),
            "寒风", "获得能力：每当你弃一张牌时，对全体敌人造成 2×层数 点伤害。可重复获得以叠加。",
            "你松手的每一张牌，都变成一阵风。",
            "Cold Wind", "Gain: whenever you discard a card, deal 2x damage to ALL enemies. Stackable.",
            "Every card you let go becomes a gust of wind."),

        (typeof(Cards.FrostEchoCard),
            "霜之回响", "获得能力：每当你打出一张攻击牌时，对全体敌人造成 3×层数 点伤害。可重复获得以叠加。",
            "每一刀落下，雪原都会跟着响一次。",
            "Frost Echo", "Gain: whenever you play an Attack, deal 3x damage to ALL enemies. Stackable.",
            "For every blade that falls, the snowfield answers once."),

        // ====================================================================
        // ★ M3.10 凛冬「裂霜」· 状态与养成牌（5 张）—— 对应 FrostStateCards.cs
        //
        // ★ 这批补的是两个**结构性缺口**，不是凑数：
        //   ① 易伤（Vulnerable）：之前整池 0 张 —— 没有伤害放大器，中后期必然"打不动"。
        //   ② 力量（Strength）：之前只有寒冰王座一张 4 费 Rare —— 成长线等于不存在。
        // ====================================================================

        (typeof(Cards.FrostCrackCard),
            "裂霜", "造成 5 点伤害，并给 2 层易伤（易伤：受到的攻击伤害提高 50%）。",
            "先裂开，再冻上。",
            "Frost Crack", "Deal 5 damage and apply 2 Vulnerable (Vulnerable: takes 50% more attack damage).",
            "Crack it first. Then freeze it."),

        (typeof(Cards.FrostSpreadCard),
            "霜蚀蔓延", "获得能力：每当一名敌人死亡时，把它身上的易伤层数转给其它敌人（×层数 倍）。可重复获得以叠加。",
            "一个人裂开的地方，会顺着风裂到所有人身上。",
            "Frost Spread", "Gain: whenever an enemy dies, transfer its Vulnerable stacks to other enemies (x multiplier). Stackable.",
            "Where one cracks, the wind carries the crack to everyone."),

        (typeof(Cards.FrostForgeCard),
            "霜锻", "获得能力：每回合开始时获得 1×层数 点力量。可重复获得以叠加。",
            "不是每一次挥砍变强，是每一次呼吸变强。",
            "Frost Forge", "Gain: at the start of each turn, gain 1x Strength. Stackable.",
            "Not every swing makes you stronger. Every breath does."),

        (typeof(Cards.DeepFreezeCoreCard),
            "极寒核心", "获得能力：每当你的一个随从被摧毁时，获得 1×层数 点力量。可重复获得以叠加。",
            "他们倒下的时候，有一部分留在了你身上。",
            "Deep Freeze Core", "Gain: whenever one of your minions is destroyed, gain 1x Strength. Stackable.",
            "When they fall, part of them stays on you."),

        (typeof(Cards.IceShellCard),
            "冰壳", "获得能力：每当你获得格挡时，对全体敌人造成 1×层数 点伤害。可重复获得以叠加。",
            "壳不是为了躲，是为了撞。",
            "Ice Shell", "Gain: whenever you gain Block, deal 1x damage to ALL enemies. Stackable.",
            "The shell is not for hiding. It is for ramming."),

        // ====================================================================
        // ★ M3.12 凛冬「精锐」+ 晋升引擎（4 张）—— 对应 FrostEpicCards.cs
        //   《凛冬前线》五大类的最后一块：精锐（可成长领袖）。
        //   三个官方特色词条（连击 / 固守 / 不朽）——**按尖塔语境重新翻译过的版本**。
        // ====================================================================

        (typeof(Cards.FrostBladeCard),
            "霜刃卫士", "部署一名 8 血霜刃卫士到【前排】。【连击】每回合对最前方的敌人攻击 2 次，每次 5(+力量) 点。已有同名单位时改为强化：+3 生命上限，且每回合多打 2 次。",
            "第一刀破防，第二刀收命。",
            "Frostblade Guard", "Deploy an 8 HP Frostblade Guard to your [Front] row. [Combo] Each turn it strikes the frontmost enemy TWICE for 5 (+Strength) each. If you have one: strengthen it (+3 Max HP, +2 more hits per turn).",
            "The first blow breaks the guard. The second takes the life."),

        (typeof(Cards.UndyingFrostCard),
            "不冻者", "部署一名 10 血不冻者到【前排】。【不朽】每回合结束回复全部生命；每回合攻击最前方的敌人 6(+力量) 点。",
            "冻不住，也杀不死。",
            "Undying Frost", "Deploy a 10 HP Undying Frost to your [Front] row. [Undying] At the end of each turn it heals to full; each turn it strikes the frontmost enemy for 6 (+Strength).",
            "It cannot freeze. It cannot die."),

        (typeof(Cards.FrostWardenCard),
            "凛冬守望者", "部署一名 12 血凛冬守望者到【后排】。【固守】只要它还活着，全体友军受到的伤害 -1×层数。",
            "它站在后面，前面的人就敢往前一步。",
            "Frost Warden", "Deploy a 12 HP Frost Warden to your [Back] row. [Hold] While it lives, all your units take 1x less damage.",
            "Because it stands behind them, those in front dare a step forward."),

        (typeof(Cards.FrostDrillCard),
            "凛冬操演", "获得能力：每回合给你每个随从 +1×层数 层【历战】；历战满 4 层时该随从【晋升】——+4 生命上限、+1 力量，并回复 4 点生命，随后历战清零（可反复晋升）。可重复获得以叠加（历战攒得更快）。",
            "活下来的，才配叫老兵。",
            "Frost Drill", "Gain: each turn, each of your minions gains 1x [Veterancy]. At 4 Veterancy it [Promotes]: +4 Max HP, +1 Strength, heal 4, then Veterancy resets (can promote repeatedly). Stackable (faster Veterancy).",
            "Only those who live earn the name veteran."),
    };

    /// <summary>
    /// 能力（Power）文本。key = `<条目>.title` / `.description`。
    /// 挂在生物身上的内部能力（FrostDeployCostPower）不写 —— 它 IsVisibleInternal = false。
    /// </summary>
    private static readonly (System.Type type, string zhTitle, string zhDesc,
        string enTitle, string enDesc)[] Powers =
    {
        (typeof(Powers.IceWallPower),
            "冰墙", "每回合开始获得 3×层数 点格挡。",
            "Ice Wall", "At the start of each turn, gain 3x Block."),

        (typeof(Powers.ArrowTowerPower),
            "箭塔", "每回合对全部敌人造成 4×层数(+力量) 点伤害。",
            "Arrow Tower", "Each turn, deal 4x (+Strength) damage to ALL enemies."),

        (typeof(Powers.CampfirePower),
            "篝火", "每回合回复 2×层数 点生命。",
            "Campfire", "Each turn, heal 2x HP."),

        (typeof(Powers.WatchtowerPower),
            "瞭望台", "每回合多抽 层数 张牌。",
            "Watchtower", "Draw 1x additional card each turn."),

        // ★ M3.4 随从「可点击行动」——点随从（或它的图标）触发，就是用户要的「像邦布那样」。
        (typeof(Actions.IceBulwarkGuardAction),
            "举盾", "点冰垒触发：全体我方随从 +3 格挡。每回合 1 次。",
            "Raise Shields", "Click the Bulwark: all your minions gain 3 Block. Once per turn."),

        (typeof(Actions.ArrowTowerVolleyAction),
            "齐射", "点箭塔触发：对全体敌人造成 6(+力量) 点伤害。每回合 1 次。",
            "Volley", "Click the Arrow Tower: deal 6 (+Strength) damage to ALL enemies. Once per turn."),

        (typeof(Actions.MilitiaChargeAction),
            "突击", "点民兵触发：选择一个敌人，造成 8(+力量) 点伤害。每回合 1 次。",
            "Charge", "Click the Militia: choose an enemy and deal 8 (+Strength) damage. Once per turn."),

        // ★ M3.8 单位光环（民兵自动攻击）—— 对应 FrostUnitPowers.cs
        (typeof(Powers.MilitiaAttackPower),
            "民兵", "每回合对最前方的敌人造成 5×层数(+力量) 点伤害。",
            "Militia", "Each turn, deal 5x (+Strength) damage to the frontmost enemy."),

        // ★ M3.6 建筑光环（挂在建筑身上，作用到主人/友军）—— 对应 FrostStructurePowers.cs
        (typeof(Powers.CampfireAuraPower),
            "篝火", "每回合回复 2×层数 点生命。",
            "Campfire", "Each turn, heal 2x HP."),

        (typeof(Powers.WatchtowerAuraPower),
            "瞭望台", "每回合多抽 层数 张牌。",
            "Watchtower", "Draw 1x additional card each turn."),

        (typeof(Powers.RuneWallPower),
            "符文壁", "受到的普通攻击伤害减半。",
            "Rune Wall", "Ordinary attacks against this are halved."),

        (typeof(Powers.FrostMageAuraPower),
            "霜蚀", "每回合给随机一名敌人挂 层数 层虚弱。",
            "Frostbite", "Each turn, apply 1x Weak to a random enemy."),

        (typeof(Powers.FrostBurstChargePower),
            "蓄力", "每回合 +4 层蓄力。点霜爆塔可引爆，造成等同于蓄力的全体伤害。",
            "Charge", "Gain 4 Charge each turn. Click the tower to Detonate for damage equal to Charge."),

        (typeof(Powers.FrostThroneAuraPower),
            "王座威仪", "每回合给主人 +2×层数 点格挡。",
            "Throne Aura", "Each turn, grant its owner 2x Block."),

        // ★ M3.6 建筑的可点击行动
        (typeof(Actions.FrostMageFreezeAction),
            "冰封", "点寒霜法师触发：选择一名敌人，让它跳过下一回合。每回合 1 次。",
            "Freeze", "Click the Frost Mage: choose an enemy, it skips its next turn. Once per turn."),

        (typeof(Actions.FrostBurstDetonateAction),
            "引爆", "点霜爆塔触发：对全体敌人造成【蓄力 + 力量】点伤害，然后清空蓄力。",
            "Detonate", "Click the Frostburst Tower: deal [Charge + Strength] damage to ALL enemies, then reset Charge."),

        // ★ M3.9 凛冬「回响」· 触发式能力 —— 对应 FrostTriggerPowers.cs
        //   文本里必须写清**触发条件**，因为这些能力平时什么都不做。
        (typeof(Powers.LastFrostPower),
            "遗霜", "每当你的一个随从被摧毁时，对全体敌人造成 6×层数 点伤害。",
            "Last Frost", "Whenever one of your minions is destroyed, deal 6x damage to ALL enemies."),

        (typeof(Powers.FrostLegacyPower),
            "霜之遗赠", "每当你的一个随从被摧毁时，获得 8×层数 点格挡。",
            "Frost Legacy", "Whenever one of your minions is destroyed, gain 8x Block."),

        (typeof(Powers.AvalanchePower),
            "雪崩", "每当一张你的牌被消耗时，对全体敌人造成 5×层数 点伤害。",
            "Avalanche", "Whenever one of your cards is Exhausted, deal 5x damage to ALL enemies."),

        (typeof(Powers.WinterEmberPower),
            "余烬", "每当一张你的牌被消耗时，抽 1×层数 张牌。",
            "Winter Ember", "Whenever one of your cards is Exhausted, draw 1x card."),

        (typeof(Powers.WinterWrathPower),
            "凛冬之怒", "每当你的一个随从受到伤害时，对全体敌人造成 3×层数 点伤害。",
            "Winter Wrath", "Whenever one of your minions takes damage, deal 3x damage to ALL enemies."),

        (typeof(Powers.IceShatterPower),
            "碎冰", "每当一名敌人的格挡被打破时，对它造成 8×层数 点伤害。",
            "Ice Shatter", "Whenever an enemy's Block is broken, deal 8x damage to that enemy."),

        (typeof(Powers.ColdWindPower),
            "寒风", "每当你弃一张牌时，对全体敌人造成 2×层数 点伤害。",
            "Cold Wind", "Whenever you discard a card, deal 2x damage to ALL enemies."),

        (typeof(Powers.FrostEchoPower),
            "霜之回响", "每当你打出一张攻击牌时，对全体敌人造成 3×层数 点伤害。",
            "Frost Echo", "Whenever you play an Attack, deal 3x damage to ALL enemies."),

        // ★ M3.10 凛冬「裂霜」· 状态与养成能力 —— 对应 FrostStatePowers.cs
        (typeof(Powers.FrostSpreadPower),
            "霜蚀蔓延", "每当一名敌人死亡时，把它身上的易伤层数转给其它敌人（×层数 倍）。",
            "Frost Spread", "Whenever an enemy dies, transfer its Vulnerable stacks to other enemies (x multiplier)."),

        (typeof(Powers.FrostForgePower),
            "霜锻", "每回合开始时获得 1×层数 点力量。",
            "Frost Forge", "At the start of each turn, gain 1x Strength."),

        (typeof(Powers.DeepFreezeCorePower),
            "极寒核心", "每当你的一个随从被摧毁时，获得 1×层数 点力量。",
            "Deep Freeze Core", "Whenever one of your minions is destroyed, gain 1x Strength."),

        (typeof(Powers.IceShellPower),
            "冰壳", "每当你获得格挡时，对全体敌人造成 1×层数 点伤害。",
            "Ice Shell", "Whenever you gain Block, deal 1x damage to ALL enemies."),

        // ★ M3.12 精锐词条 + 历战晋升 —— 对应 FrostEpicPowers.cs
        (typeof(Powers.FrostBladeComboPower),
            "连击", "每回合对最前方的敌人攻击 2×层数 次，每次 5(+力量) 点。",
            "Combo", "Each turn, strike the frontmost enemy 2x times for 5 (+Strength) each."),

        (typeof(Powers.UndyingFrostPower),
            "不朽", "每回合结束回复全部生命；每回合攻击最前方的敌人 6×层数(+力量) 点。",
            "Undying", "Heal to full at the end of each turn; strike the frontmost enemy for 6x (+Strength) each turn."),

        (typeof(Powers.FrostWardenPower),
            "固守", "只要它还活着，全体友军受到的伤害 -1×层数。",
            "Hold", "While it lives, all your units take 1x less damage."),

        (typeof(Powers.FrostVeterancyPower),
            "历战", "满 4 层时晋升：+4 生命上限、+1 力量，并回复 4 点生命，随后清零。",
            "Veterancy", "At 4 stacks: promote (+4 Max HP, +1 Strength, heal 4), then reset."),

        (typeof(Powers.FrostDrillPower),
            "凛冬操演", "每回合给你每个随从 +1×层数 层历战。",
            "Frost Drill", "Each turn, each of your minions gains 1x Veterancy."),
    };

    /// <summary>生物（Monster）文本：名字。</summary>
    private static readonly (System.Type type, string zhTitle, string enTitle)[] Monsters =
    {
        (typeof(Monsters.IceBulwarkM), "冰垒", "Ice Bulwark"),
        (typeof(Monsters.ArrowTowerM), "箭塔", "Arrow Tower"),
        (typeof(Monsters.MilitiaM), "民兵", "Militia"),

        // ★ M3.12 精锐单位 —— 对应 FrostEpicMonsters.cs
        (typeof(Monsters.FrostBladeM), "霜刃卫士", "Frostblade Guard"),
        (typeof(Monsters.UndyingFrostM), "不冻者", "Undying Frost"),
        (typeof(Monsters.FrostWardenM), "凛冬守望者", "Frost Warden"),

        // ★ M3.6 扩充建筑 —— 对应 FrostStructureMonsters.cs
        (typeof(Monsters.CampfireM), "篝火", "Campfire"),
        (typeof(Monsters.WatchtowerM), "瞭望台", "Watchtower"),
        (typeof(Monsters.RuneWallM), "符文壁", "Rune Wall"),
        (typeof(Monsters.FrostMageM), "寒霜法师", "Frost Mage"),
        (typeof(Monsters.FrostBurstTowerM), "霜爆塔", "Frostburst Tower"),
        (typeof(Monsters.FrostThroneM), "寒冰王座", "Frost Throne"),
    };

    /// <summary>遗物（Relic）文本。</summary>
    private static readonly (System.Type type, string zhTitle, string zhDesc, string zhFlavor,
        string enTitle, string enDesc, string enFlavor)[] Relics =
    {
        (typeof(Relics.FrostPactRelic),
            "凛冬契约", "本局改为守塔战：清空场地会引来下一波，共 16 波。",
            "你与冬天签了字。",
            "Frost Pact", "This run is a defense: clearing the field summons the next wave. 16 waves total.",
            "You signed with the winter."),
    };

    public static void Inject(Logger logger)
    {
        try
        {
            WriteTable("zhs", "cards", BuildCards(zh: true), logger);
            WriteTable("eng", "cards", BuildCards(zh: false), logger);
            WriteTable("zhs", "powers", BuildPowers(zh: true), logger);
            WriteTable("eng", "powers", BuildPowers(zh: false), logger);
            WriteTable("zhs", "monsters", BuildMonsters(zh: true), logger);
            WriteTable("eng", "monsters", BuildMonsters(zh: false), logger);
            WriteTable("zhs", "relics", BuildRelics(zh: true), logger);
            WriteTable("eng", "relics", BuildRelics(zh: false), logger);

            // ★ 自检：把"我们写的 key vs 运行时真名"打进日志。
            //   上次排查"卡牌还英文"绕了很久，就是因为不知道 key 有没有对上。
            SelfCheck(logger);
        }
        catch (System.Exception e)
        {
            logger.Warn($"FrostLoc failed (不影响游戏，只是少了文本): {e}");
        }
    }

    // =====================================================================
    //  各表构建
    // =====================================================================

    private static Dictionary<string, string> BuildCards(bool zh)
    {
        var map = new Dictionary<string, string>();
        foreach (var (type, zhT, zhD, zhF, enT, enD, enF) in Cards)
        {
            foreach (string entry in CandidateEntries(type, "CARD"))
            {
                map[$"{entry}.title"] = zh ? zhT : enT;
                map[$"{entry}.description"] = zh ? zhD : enD;
                map[$"{entry}.flavor"] = zh ? zhF : enF;
            }
        }
        return map;
    }

    private static Dictionary<string, string> BuildPowers(bool zh)
    {
        var map = new Dictionary<string, string>();
        foreach (var (type, zhT, zhD, enT, enD) in Powers)
        {
            foreach (string entry in CandidateEntries(type, "POWER"))
            {
                map[$"{entry}.title"] = zh ? zhT : enT;
                map[$"{entry}.description"] = zh ? zhD : enD;
            }
        }
        return map;
    }

    private static Dictionary<string, string> BuildMonsters(bool zh)
    {
        var map = new Dictionary<string, string>();
        foreach (var (type, zhT, enT) in Monsters)
        {
            foreach (string entry in CandidateEntries(type, "MONSTER"))
            {
                map[$"{entry}.name"] = zh ? zhT : enT;
                map[$"{entry}.title"] = zh ? zhT : enT;
            }
        }
        return map;
    }

    private static Dictionary<string, string> BuildRelics(bool zh)
    {
        var map = new Dictionary<string, string>();
        foreach (var (type, zhT, zhD, zhF, enT, enD, enF) in Relics)
        {
            foreach (string entry in CandidateEntries(type, "RELIC"))
            {
                map[$"{entry}.title"] = zh ? zhT : enT;
                map[$"{entry}.description"] = zh ? zhD : enD;
                map[$"{entry}.flavor"] = zh ? zhF : enF;
            }
        }
        return map;
    }

    // =====================================================================
    //  条目名候选（多写无害：游戏只会命中其中一条）
    // =====================================================================

    /// <summary>
    /// 一个类型可能对应多个条目名。全部写上——命中的那条生效，多余的没人读。
    ///   ① `<MOD>_<类别>_<SCREAMING_SNAKE>` —— RitsuLib 默认规则（最可能命中的）
    ///   ② 运行时真名（ModelDb.GetId，注册完成前可能拿不到）
    ///   ③ 裸 SCREAMING_SNAKE（有些版本不加前缀）
    /// </summary>
    private static System.Collections.Generic.IEnumerable<string> CandidateEntries(System.Type type, string category)
    {
        var seen = new HashSet<string>();

        string prefixed = $"{ToScreamingSnake(Entry.ModId)}_{category}_{ToScreamingSnake(type.Name)}";
        if (seen.Add(prefixed)) yield return prefixed;

        string runtime = EntryOf(type);
        if (!string.IsNullOrEmpty(runtime) && seen.Add(runtime))
            yield return runtime;

        string bare = ToScreamingSnake(type.Name);
        if (seen.Add(bare)) yield return bare;
    }

    /// <summary>
    /// 自检：把我们写进覆盖表的内容，跟**运行时真名**对一遍。
    ///
    /// ★ 为什么需要它（M3.5 教训）：
    ///   覆盖表只有在 key **命中本体已有条目**时才生效。key 猜错时游戏**不报错**，
    ///   卡面只会显示原始 key（`cards.FROST_SPIRE_CARD_XXX.description`）——
    ///   排查时无从下手，只能靠肉眼看日志有没有 `[LocValidation]` 警告。
    ///   这个自检把"我写了什么 key / 运行时真名是什么 / 有没有对上"直接打进日志。
    ///
    /// 判定"对上"的口径：候选里**至少有一个**等于运行时真名，就算命中。
    /// （候选写得比真名多是有意的兜底，不算错。）
    /// </summary>
    private static void SelfCheck(Logger logger)
    {
        try
        {
            var problems = new List<string>();

            foreach (var (type, _, _, _, _, _, _) in Cards)
                Compare(logger, problems, type, "CARD");
            foreach (var (type, _, _, _, _) in Powers)
                Compare(logger, problems, type, "POWER");
            foreach (var (type, _, _) in Monsters)
                Compare(logger, problems, type, "MONSTER");
            foreach (var (type, _, _, _, _, _, _) in Relics)
                Compare(logger, problems, type, "RELIC");

            if (problems.Count == 0)
                logger.Info("FrostLoc 自检通过：所有条目名都能对上运行时真名。");
            else
                logger.Warn($"FrostLoc 自检发现 {problems.Count} 个条目名对不上（卡面会显示原始 key）："
                            + string.Join(" | ", problems));
        }
        catch (System.Exception e)
        {
            logger.Warn($"FrostLoc 自检本身失败（不影响注入）: {e.Message}");
        }
    }

    private static void Compare(Logger logger, List<string> problems, System.Type type, string category)
    {
        string runtime = EntryOf(type);
        if (string.IsNullOrEmpty(runtime))
        {
            // ★ 这不是错误：RitsuLib 的自动注册可能晚于我们的 Inject。
            //   这种情况我们靠"猜前缀"那条候选兜底，没法验证，如实记一条信息。
            logger.Info($"FrostLoc 自检：{type.Name} 运行时真名暂不可得（注册未完成？），跳过比对。");
            return;
        }

        foreach (string entry in CandidateEntries(type, category))
            if (entry == runtime) return;   // 有一条对上即命中

        problems.Add($"{type.Name} 真名={runtime} 候选={string.Join("/", CandidateEntries(type, category))}");
    }

    private static string EntryOf(System.Type type)
    {
        try { return ModelDb.GetId(type).Entry; }
        catch (System.Exception) { return ""; }
    }

    // IceBulwarkCard → ICE_BULWARK_CARD；FrostSpire → FROST_SPIRE
    private static string ToScreamingSnake(string name)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && i > 0 && name[i - 1] != '_') sb.Append('_');
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    // =====================================================================
    //  落盘（先读再合并，绝不覆盖玩家自己的文件）
    // =====================================================================

    private static void WriteTable(string language, string table, Dictionary<string, string> ours, Logger logger)
    {
        string root = ProjectSettings.GlobalizePath("user://localization_override");
        string dir = System.IO.Path.Combine(root, language);
        string file = System.IO.Path.Combine(dir, table + ".json");

        if (!DirAccess.DirExistsAbsolute(dir))
        {
            Error err = DirAccess.MakeDirRecursiveAbsolute(dir);
            if (err != Error.Ok)
            {
                logger.Warn($"FrostLoc: 建不了目录 {dir}（{err}）");
                return;
            }
        }

        var merged = new Dictionary<string, string>();
        if (Godot.FileAccess.FileExists(file))
        {
            try
            {
                using var read = Godot.FileAccess.Open(file, Godot.FileAccess.ModeFlags.Read);
                var existing = read == null
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(read.GetAsText());
                if (existing != null)
                    foreach (var kv in existing)
                        merged[kv.Key] = kv.Value;
            }
            catch (System.Exception e)
            {
                logger.Warn($"FrostLoc: {file} 解析不了，整个跳过（不碰玩家自己的文件）：{e.Message}");
                return;
            }
        }

        foreach (var kv in ours) merged[kv.Key] = kv.Value;

        using var write = Godot.FileAccess.Open(file, Godot.FileAccess.ModeFlags.Write);
        if (write == null)
        {
            logger.Warn($"FrostLoc: 写不进 {file}");
            return;
        }
        write.StoreString(JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }));
        logger.Info($"FrostLoc: 注入 {merged.Count} 条 [{table}] 文本到 {file}");
    }
}
