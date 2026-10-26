using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using BloomlessSpire.Characters;
using BloomlessSpire.Packs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace BloomlessSpire.Cards;

// 核心卡：撕包。从当前激活卡包中随机撕开一张卡加入手牌。
// 如果没有激活卡包，回退到随机卡包。
[RegisterCard(typeof(BloomlessSpireCardPool))]
public sealed class PackRip : ModCardTemplate
{
    private const int BaseEnergyCost = 1;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Common;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public PackRip() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;

        // 优先从当前激活卡包开卡，没有则回退到随机
        var card = PackSelectionManager.RipFromActivePack(owner)
                   ?? CardPackDatabase.RandomPack().CreateRandomCard(owner);

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
    }
}
