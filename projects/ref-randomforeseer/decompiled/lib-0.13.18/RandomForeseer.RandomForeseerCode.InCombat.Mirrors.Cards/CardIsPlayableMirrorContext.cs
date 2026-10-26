using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards;

internal sealed class CardIsPlayableMirrorContext : CombatCardMirrorContext<CardModel>
{
	protected override AbstractModel GetDispatchSource(CardModel _)
	{
		return (AbstractModel)(object)base.OriginalCard;
	}
}
