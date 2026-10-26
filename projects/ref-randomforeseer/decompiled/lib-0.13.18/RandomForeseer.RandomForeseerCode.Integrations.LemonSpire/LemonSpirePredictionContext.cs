using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat;
using RandomForeseer.RandomForeseerCode.OutOfCombat;

namespace RandomForeseer.RandomForeseerCode.Integrations.LemonSpire;

internal sealed class LemonSpirePredictionContext
{
	[CompilerGenerated]
	private Player _003Cplayer_003EP;

	[CompilerGenerated]
	private AbstractModel _003Cmodel_003EP;

	[CompilerGenerated]
	private LemonSpirePredictionKind _003Ckind_003EP;

	public LemonSpirePredictionContext(Player player, AbstractModel model, LemonSpirePredictionKind kind)
	{
		_003Cplayer_003EP = player;
		_003Cmodel_003EP = model;
		_003Ckind_003EP = kind;
		base._002Ector();
	}

	public IReadOnlyList<IHoverTip> GetHoverTips()
	{
		LemonSpirePredictionKind lemonSpirePredictionKind = _003Ckind_003EP;
		AbstractModel val = _003Cmodel_003EP;
		switch (lemonSpirePredictionKind)
		{
		case LemonSpirePredictionKind.HandCard:
		{
			CardModel val4 = (CardModel)(object)((val is CardModel) ? val : null);
			if (val4 == null)
			{
				break;
			}
			return CombatCardPrediction.GetHoverTips(val4);
		}
		case LemonSpirePredictionKind.AncientRelicChoice:
		{
			RelicModel val3 = (RelicModel)(object)((val is RelicModel) ? val : null);
			if (val3 == null)
			{
				break;
			}
			IReadOnlyList<IHoverTip> result;
			if (!IsAncientRelicChoiceStale(_003Cplayer_003EP))
			{
				result = RelicPickupPrediction.GetHoverTips(_003Cplayer_003EP, val3);
			}
			else
			{
				IReadOnlyList<IHoverTip> readOnlyList = Array.Empty<IHoverTip>();
				result = readOnlyList;
			}
			return result;
		}
		case LemonSpirePredictionKind.ShopRelic:
		{
			RelicModel val3 = (RelicModel)(object)((val is RelicModel) ? val : null);
			if (val3 == null)
			{
				break;
			}
			RelicModel relic = val3;
			return RelicPickupPrediction.GetHoverTips(_003Cplayer_003EP, relic);
		}
		case LemonSpirePredictionKind.ShopPotion:
		{
			PotionModel val2 = (PotionModel)(object)((val is PotionModel) ? val : null);
			if (val2 == null)
			{
				break;
			}
			return PotionPrediction.GetHoverTips(_003Cplayer_003EP, val2);
		}
		}
		return Array.Empty<IHoverTip>();
	}

	private static bool IsAncientRelicChoiceStale(Player player)
	{
		try
		{
			EventModel eventForPlayer = RunManager.Instance.EventSynchronizer.GetEventForPlayer(player);
			AncientEventModel val = (AncientEventModel)(object)((eventForPlayer is AncientEventModel) ? eventForPlayer : null);
			if (val == null)
			{
				return false;
			}
			return ((EventModel)val).IsFinished || ((EventModel)val).CurrentOptions.Any((EventOption option) => option != null && option.Relic != null && option.WasChosen);
		}
		catch
		{
			return false;
		}
	}
}
