using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Random;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

internal static class DollRoomPrediction
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(DollRoom dollRoom, EventOption option)
	{
		int num = option.TextKey switch
		{
			"DOLL_ROOM.pages.INITIAL.options.RANDOM" => 1, 
			"DOLL_ROOM.pages.INITIAL.options.TAKE_SOME_TIME" => 2, 
			"DOLL_ROOM.pages.INITIAL.options.EXAMINE" => 3, 
			_ => 0, 
		};
		if (num <= 0)
		{
			return Array.Empty<IHoverTip>();
		}
		return OutOfCombatPredictionUtils.RelicTipsWithPickup(((EventModel)dollRoom).Owner, PredictDoll(((EventModel)dollRoom).Rng, num));
	}

	private static IReadOnlyList<RelicModel> PredictDoll(Rng realRng, int count)
	{
		Rng val = realRng.Clone();
		RelicModel[] array = DollRoom._dolls.Select((DollChoice doll) => doll.relic).ToArray();
		if (count != 1)
		{
			return ListExtensions.StableShuffle<RelicModel>(array.ToList(), val).Take(count).ToList();
		}
		int num = 1;
		List<RelicModel> list = new List<RelicModel>(num);
		CollectionsMarshal.SetCount(list, num);
		CollectionsMarshal.AsSpan(list)[0] = val.NextItem<RelicModel>((IEnumerable<RelicModel>)array);
		return list;
	}
}
