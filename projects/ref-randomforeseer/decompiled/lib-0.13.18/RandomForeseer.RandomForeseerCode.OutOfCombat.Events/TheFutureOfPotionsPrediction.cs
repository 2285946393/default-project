using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class TheFutureOfPotionsPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(TheFutureOfPotions future, EventOption option)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Invalid comparison between Unknown and I4
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Invalid comparison between Unknown and I4
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		if (option.TextKey != "THE_FUTURE_OF_POTIONS.pages.INITIAL.options.POTION")
		{
			return Array.Empty<IHoverTip>();
		}
		Player owner = ((EventModel)future).Owner;
		int index = ((EventModel)future).CurrentOptions.ToList().IndexOf(option);
		PotionModel val = owner.Potions.ElementAtOrDefault(index);
		Dictionary<PotionModel, CardType> cardTypes = future._cardTypes;
		if (val == null || cardTypes == null || !cardTypes.TryGetValue(val, out var cardType))
		{
			return Array.Empty<IHoverTip>();
		}
		PotionRarity rarity = val.Rarity;
		CardRarity val2 = (((int)rarity == 2) ? ((CardRarity)3) : ((rarity - 3 > 1) ? ((CardRarity)2) : ((CardRarity)4)));
		CardRarity targetRarity = val2;
		CardCreationOptions options = CardCreationOptions.ForNonCombatWithUniformOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(owner.Character.CardPool), (Func<CardModel, bool>)((CardModel card) => card.Rarity == targetRarity && card.Type == cardType)).WithFlags((CardCreationFlags)145);
		IReadOnlyList<CardModel> readOnlyList = CardRewardPrediction.PredictCards(owner, 3, options);
		foreach (CardModel item in readOnlyList)
		{
			PredictionUtils.UpgradeCard(item);
		}
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(readOnlyList.ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}
}
