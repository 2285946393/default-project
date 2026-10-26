using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace QuestSpire.Relics;

// ============================================================================
// 第二批自研的一代遗物（2026-09-22 深夜十一批）。
//
// 背景：用户指出「官方公开说过一代的素材可以用在二代里」，所以这批图标直接抠自
// 杀戮尖塔1 的官方 jar（tools/extract_sts1_relic_art.py）。于是这几件也不必再
// 依赖「更多遗物」(moreRelics) —— 谁都能拿到。
//
// 只挑了**接口干净、确认能做**的三件；剩下三件先留着（原因写在下面）。
// ============================================================================

// 金偶像：敌人掉落的金币 +25%。
// 用的是 ModifyGoldGained —— 基类返回原值 amount，所以乘 1.25 就是标准的加成写法。
[RegisterRelic(typeof(QuestRelicPool))]
public sealed class QuestGoldenIdolRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://QuestSpire/images/relics/golden_idol.png",
        IconOutlinePath: "res://QuestSpire/images/relics/golden_idol_outline.png",
        BigIconPath: "res://QuestSpire/images/relics/golden_idol_big.png");

    public override decimal ModifyGoldGained(Player player, decimal amount)
        => player == Owner ? amount * 1.25m : amount;
}

// 御守：抵消你接下来获得的 2 张诅咒。
// 用的是 ShouldAddToDeck（基类返回 true，返回 false 就是不让这张牌进卡组）。
[RegisterRelic(typeof(QuestRelicPool))]
public sealed class QuestOmamoriRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://QuestSpire/images/relics/omamori.png",
        IconOutlinePath: "res://QuestSpire/images/relics/omamori_outline.png",
        BigIconPath: "res://QuestSpire/images/relics/omamori_big.png");

    // 剩几次护佑。默认 2 —— 遗物实体的初始化器会跑到，读档时由存档覆盖。
    [SavedProperty]
    public int ChargesLeft { get; set; } = 2;

    public override bool ShouldAddToDeck(CardModel card)
    {
        if (ChargesLeft <= 0) return true;
        if (card.Type != CardType.Curse) return true;

        ChargesLeft--;
        Flash();
        Entry.Logger.Info($"QuestOmamoriRelic: 挡掉一张诅咒（还剩 {ChargesLeft} 次）");
        return false;
    }
}

// 齿轮工艺品：每场战斗开始时获得 1 层人工制品。
// ⚠️ PowerCmd.Apply 需要一个 PlayerChoiceContext，而 BeforeCombatStart() 没有，
//    所以改成「战斗的第一个回合开始时」给 —— 体感几乎一样，而且那边有 context。
[RegisterRelic(typeof(QuestRelicPool))]
public sealed class QuestClockworkRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://QuestSpire/images/relics/clockwork.png",
        IconOutlinePath: "res://QuestSpire/images/relics/clockwork_outline.png",
        BigIconPath: "res://QuestSpire/images/relics/clockwork_big.png");

    private bool _pending;

    public override Task BeforeCombatStart()
    {
        _pending = true;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || !_pending) return;
        _pending = false;
        Flash();
        await PowerCmd.Apply<ArtifactPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null);
    }
}

// ============================================================================
// 还没做进来的三件（写清楚原因，免得下次又去翻一遍）：
//
//   机械臂 Inserter（每 2 回合 +1 充能球栏位）
//     → 没找到充能球栏位的接口（PlayerCmd / CreatureCmd 里都没有，也没有 OrbSlot* 类型）。
//   套娃 Matryoshka（接下来 2 个宝箱内会有 2 件遗物）
//     → 要改宝箱房的奖励生成。RewardsCmd 只有 OfferForRoomEnd/OfferCustom/Generate*，
//       构造 RelicReward 的参数没确认，风险大。
//   突变酵素 MutagenicStrength（战斗开始 +3 力量，第一回合结束失去）
//     → 「失去」这一半要精确移除自己给的那 3 点（PowerCmd.Remove<T> 会把来源不明的力量
//       一起清掉），得先确认力量是不是带时长/来源。而且一代 jar 里没找到这件的大图。
//
// 这三件目前仍走旧路：装了「更多遗物」就发那个 mod 的，没装就折现金币。
// ============================================================================
