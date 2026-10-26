using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using QuestSpire.Quests;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace QuestSpire.Relics;

// 枯木树枝 —— **我们自己造的**，不依赖「更多遗物」(moreRelics) 之类的别的 mod。
// 效果：每当你消耗一张牌，将一张随机牌加入手牌。
//
// 关于文本：纯 dll 的 mod 读不到 res:// 下的本地化表（那要 pck），
// 所以标题/描述由 LocInjector 写进游戏的本地化覆盖目录，见那个文件。
[RegisterRelic(typeof(QuestRelicPool))]
public sealed class QuestDeadBranchRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    // 图标：用**杀戮尖塔1 的官方原画**（从本体的 jar 里抠出来，放进本 mod 的 pck）。
    // 注意路径要写全 —— RelicAssetProfile 收的是原始路径，不像 ContentAssetProfiles 那样会自动补前缀。
    //
    // ⚠️ 实测教训：三张图都要给。
    //   - 只给小图标 → 遗物栏能看见图，但**详情页仍然是锁头**（缺 big 就回退成 locked_model）。
    //   - 一代的图是 128x128 带透明留白，二代是 85x85 贴边，不做规整会比正式遗物「小一节」。
    //   规整脚本：tools/ 之外那次用的 normalize_icons.py（裁透明边 → 85x85，大图 256x256）。
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://QuestSpire/images/relics/dead_branch.png",
        IconOutlinePath: "res://QuestSpire/images/relics/dead_branch_outline.png",
        BigIconPath: "res://QuestSpire/images/relics/dead_branch_big.png");

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner != Owner) return;
        if (await CardGift.GiveRandomCardToHand(Owner)) Flash();
    }
}
