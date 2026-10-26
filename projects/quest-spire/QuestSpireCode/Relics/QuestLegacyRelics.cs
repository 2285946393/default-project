using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace QuestSpire.Relics;

// ============================================================================
// 一代遗物的「自研版」——**不依赖任何别的 mod**。
//
// 为什么要有这一批：任务里原本是「运行期去别的 mod 里按 ID 找那件遗物，找到就发」。
// 但别人不可能装那么多 mod，找不到就只剩我们自己的「随身物」（只在面板里显示，不算遗物）。
// 现在把常用的几件直接做进本 mod，谁都能拿到。
//
// 文本：纯 dll 读不到 res:// 的本地化表，所以标题/描述由 LocInjector 写进
// 游戏的本地化覆盖目录（%APPDATA%\SlayTheSpire2\localization_override\<语言>\relics.json）。
// ============================================================================

// 鸟居：每场战斗开始时获得 8 点格挡。
[RegisterRelic(typeof(QuestRelicPool))]
public sealed class QuestToriiRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 图标：一代官方原画（抠自 STS1 的 jar，放进本 mod 的 pck）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://QuestSpire/images/relics/torii.png",
        IconOutlinePath: "res://QuestSpire/images/relics/torii_outline.png",
        BigIconPath: "res://QuestSpire/images/relics/torii_big.png");

    public override async Task BeforeCombatStart()
    {
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, 8m, ValueProp.Unpowered, null);
    }
}

// 咖啡滤杯：每场战斗的第一个回合 +1 能量。
[RegisterRelic(typeof(QuestRelicPool))]
public sealed class QuestDripperRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    // 图标：一代官方原画（抠自 STS1 的 jar）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://QuestSpire/images/relics/coffee_dripper.png",
        IconOutlinePath: "res://QuestSpire/images/relics/coffee_dripper_outline.png",
        BigIconPath: "res://QuestSpire/images/relics/coffee_dripper_big.png");

    private bool _firstTurnPending;

    public override Task BeforeCombatStart()
    {
        _firstTurnPending = true;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || !_firstTurnPending) return;
        _firstTurnPending = false;
        Flash();
        await PlayerCmd.GainEnergy(1m, Owner);
    }
}

// 玩具扑翼飞机：**每次使用药水时回复 5 点生命**。
// ⚠️ 一代原版就是这个效果（不是「战斗开始回血」）——玩家指出这和「药罐子」任务没有联动，已按原版改回。
[RegisterRelic(typeof(QuestRelicPool))]
public sealed class QuestOrnithopterRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 图标：一代官方原画（抠自 STS1 的 jar；这件一代没画大图，用规整后的小图放大补的）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://QuestSpire/images/relics/ornithopter.png",
        IconOutlinePath: "res://QuestSpire/images/relics/ornithopter_outline.png",
        BigIconPath: "res://QuestSpire/images/relics/ornithopter_big.png");

    // 一代原版是「每次使用药水时回复 5 点生命」——玩家指出「战斗开始回血」和
    // 「药罐子」这个任务毫无联动，已按原版改回。
    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        Flash();
        await CreatureCmd.Heal(Owner.Creature, 5m);
    }
}
