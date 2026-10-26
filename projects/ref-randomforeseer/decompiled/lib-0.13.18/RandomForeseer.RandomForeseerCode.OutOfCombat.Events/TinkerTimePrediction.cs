using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class TinkerTimePrediction
{
	private static readonly Dictionary<string, RiderEffect[]> RidersByOptionKey;

	public static IReadOnlyList<IHoverTip> GetHoverTips(TinkerTime tinkerTime, EventOption option)
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		TinkerTime tinkerTime2 = tinkerTime;
		if (!RidersByOptionKey.TryGetValue(option.TextKey, out RiderEffect[] value))
		{
			return Array.Empty<IHoverTip>();
		}
		Rng val = ((EventModel)tinkerTime2).Rng.Clone();
		IEnumerable<RiderEffect> source = ListExtensions.UnstableShuffle<RiderEffect>(value.ToList(), val).Take(2);
		string textKey = option.TextKey;
		CardType val2 = ((textKey == "TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.ATTACK") ? ((CardType)1) : ((!(textKey == "TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.SKILL")) ? ((CardType)3) : ((CardType)2)));
		CardType cardType = val2;
		IEnumerable<CardModel> cards = source.Select(delegate(RiderEffect rider)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Expected O, but got Unknown
			MadScience val3 = (MadScience)PredictionUtils.CreateCard((CardModel)(object)ModelDb.Card<MadScience>(), ((EventModel)tinkerTime2).Owner);
			val3.TinkerTimeType = cardType;
			val3.TinkerTimeRider = rider;
			return (CardModel)val3;
		});
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(cards.ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}

	static TinkerTimePrediction()
	{
		Dictionary<string, RiderEffect[]> dictionary = new Dictionary<string, RiderEffect[]>();
		RiderEffect[] array = new RiderEffect[3];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		dictionary["TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.ATTACK"] = (RiderEffect[])(object)array;
		RiderEffect[] array2 = new RiderEffect[3];
		RuntimeHelpers.InitializeArray(array2, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		dictionary["TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.SKILL"] = (RiderEffect[])(object)array2;
		RiderEffect[] array3 = new RiderEffect[3];
		RuntimeHelpers.InitializeArray(array3, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		dictionary["TINKER_TIME.pages.CHOOSE_CARD_TYPE.options.POWER"] = (RiderEffect[])(object)array3;
		RidersByOptionKey = dictionary;
	}
}
