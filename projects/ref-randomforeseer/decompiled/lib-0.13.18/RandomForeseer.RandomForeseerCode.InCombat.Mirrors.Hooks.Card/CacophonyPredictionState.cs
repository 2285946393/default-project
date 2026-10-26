using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class CacophonyPredictionState
{
	public int CardsDrawn { get; set; } = ((DynamicVar)((PowerModel)power).DynamicVars.Cards).IntValue;


	public CacophonyPredictionState(CacophonyPower power)
	{
	}
}
