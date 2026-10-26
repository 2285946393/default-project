using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class ColorfulPhilosophersPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(ColorfulPhilosophers colorfulPhilosophers, EventOption option)
	{
		EventOption option2 = option;
		ColorfulPhilosophers colorfulPhilosophers2 = colorfulPhilosophers;
		if (!option2.TextKey.StartsWith("COLORFUL_PHILOSOPHERS.pages.INITIAL.options.", StringComparison.Ordinal))
		{
			return Array.Empty<IHoverTip>();
		}
		Player owner = ((EventModel)colorfulPhilosophers2).Owner;
		CardPoolModel pool2 = owner.UnlockState.CharacterCardPools.FirstOrDefault((Func<CardPoolModel, bool>)((CardPoolModel pool) => option2.TextKey.EndsWith(pool.EnergyColorName.ToUpperInvariant(), StringComparison.Ordinal)));
		if (pool2 == null)
		{
			return Array.Empty<IHoverTip>();
		}
		RunPredictionContext context = new RunPredictionContext(owner);
		CardRarity[] array = new CardRarity[3];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		List<IReadOnlyList<CardModel>> bundles = ((IEnumerable<CardRarity>)(object)array).Select((CardRarity rarity) => CardRewardPrediction.PredictCards(context, ((DynamicVar)((EventModel)colorfulPhilosophers2).DynamicVars.Cards).IntValue, new CardCreationOptions((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(pool2), (CardCreationSource)3, (CardRarityOddsType)5, (Func<CardModel, bool>)((CardModel card) => card.Rarity == rarity)).WithFlags((CardCreationFlags)145))).ToList();
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(bundles.ToPredictionCardBundleHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}
}
