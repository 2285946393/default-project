using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class CardPlayTimingPatch
{
	[HarmonyPatch(typeof(NPlayerHand), "StartCardPlay")]
	[HarmonyPostfix]
	public static void CardDragStartPatch(NHandCardHolder holder, bool startedViaShortcut)
	{
		CardModel val = ((holder != null) ? ((NCardHolder)holder).CardModel : null);
		if (val != null)
		{
			CardFX cardFX = CardFX.FromCard(val);
			if (cardFX != null)
			{
				Entry.StarEffectController?.OnCardHolding(val, cardFX);
			}
		}
		else
		{
			Entry.Logger.Warn("[CardPlayTiming]缺少CardModel", 1);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NCardPlay), "CancelPlayCard")]
	public static void CardCancel(NCardPlay __instance)
	{
		__instance._isTryingToPlayCard.ToString();
		if (!__instance._isTryingToPlayCard)
		{
			Entry.StarEffectController?.OnCancelCard();
		}
	}
}
