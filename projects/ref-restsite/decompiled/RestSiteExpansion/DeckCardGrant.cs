using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace RestSiteExpansion;

internal static class DeckCardGrant
{
	internal static async Task AddToDeck<T>(Player player, float previewSeconds = 1.2f) where T : CardModel
	{
		CardModel card = ((ICardScope)player.RunState).CreateCard((CardModel)(object)ModelDb.Card<T>(), player);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), previewSeconds, (CardPreviewStyle)1);
		MainFile.Logger.Info($"[DeckGrant] 已把 {((AbstractModel)card).Id.Entry} 加入牌库：player={player.NetId}", 1);
	}
}
