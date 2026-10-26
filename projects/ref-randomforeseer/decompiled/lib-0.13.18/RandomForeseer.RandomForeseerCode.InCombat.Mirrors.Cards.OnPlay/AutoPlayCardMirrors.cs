using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class AutoPlayCardMirrors
{
	public static void HavocOnPlay(Havoc card, CardOnPlayMirrorContext context)
	{
		context.Simulator.AutoPlayFromDrawPile(((CardModel)card).Owner, 1, (CardPilePosition)2, forceExhaust: true);
	}

	public static void CascadeOnPlay(Cascade card, CardOnPlayMirrorContext context)
	{
		int count = context.ResolveEnergyXValue() + (((CardModel)card).IsUpgraded ? 1 : 0);
		context.Simulator.AutoPlayFromDrawPile(((CardModel)card).Owner, count, (CardPilePosition)2);
	}
}
