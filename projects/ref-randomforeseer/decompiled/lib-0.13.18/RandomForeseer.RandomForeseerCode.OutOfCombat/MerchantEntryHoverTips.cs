using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class MerchantEntryHoverTips
{
	public static IReadOnlyList<IHoverTip> GetHoverTips(Control owner)
	{
		NMerchantCard val = (NMerchantCard)(object)((owner is NMerchantCard) ? owner : null);
		if (val != null)
		{
			MerchantEntry entry = ((NMerchantSlot)val).Entry;
			MerchantCardEntry val2 = (MerchantCardEntry)(object)((entry is MerchantCardEntry) ? entry : null);
			if (val2 != null)
			{
				return MerchantRestockPrediction.GetHoverTips((MerchantEntry)(object)val2);
			}
		}
		else
		{
			NMerchantRelic val3 = (NMerchantRelic)(object)((owner is NMerchantRelic) ? owner : null);
			if (val3 != null)
			{
				MerchantEntry entry2 = ((NMerchantSlot)val3).Entry;
				MerchantRelicEntry val4 = (MerchantRelicEntry)(object)((entry2 is MerchantRelicEntry) ? entry2 : null);
				if (val4 != null)
				{
					RelicModel model = val4.Model;
					if (model != null)
					{
						IReadOnlyList<IHoverTip> hoverTips = RelicPickupPrediction.GetHoverTips(((MerchantEntry)val4)._player, model);
						IReadOnlyList<IHoverTip> hoverTips2 = MerchantRestockPrediction.GetHoverTips((MerchantEntry)(object)val4);
						int num = 0;
						IHoverTip[] array = (IHoverTip[])(object)new IHoverTip[hoverTips.Count + hoverTips2.Count];
						foreach (IHoverTip item in hoverTips)
						{
							array[num] = item;
							num++;
						}
						foreach (IHoverTip item2 in hoverTips2)
						{
							array[num] = item2;
							num++;
						}
						return new _003C_003Ez__ReadOnlyArray<IHoverTip>(array);
					}
				}
			}
			else
			{
				NMerchantPotion val5 = (NMerchantPotion)(object)((owner is NMerchantPotion) ? owner : null);
				if (val5 != null)
				{
					MerchantEntry entry3 = ((NMerchantSlot)val5).Entry;
					MerchantPotionEntry val6 = (MerchantPotionEntry)(object)((entry3 is MerchantPotionEntry) ? entry3 : null);
					if (val6 != null)
					{
						PotionModel model2 = val6.Model;
						if (model2 != null)
						{
							IReadOnlyList<IHoverTip> hoverTips3 = PotionPrediction.GetHoverTips(((MerchantEntry)val6)._player, model2);
							IReadOnlyList<IHoverTip> hoverTips2 = MerchantRestockPrediction.GetHoverTips((MerchantEntry)(object)val6);
							int num = 0;
							IHoverTip[] array = (IHoverTip[])(object)new IHoverTip[hoverTips3.Count + hoverTips2.Count];
							foreach (IHoverTip item3 in hoverTips3)
							{
								array[num] = item3;
								num++;
							}
							foreach (IHoverTip item4 in hoverTips2)
							{
								array[num] = item4;
								num++;
							}
							return new _003C_003Ez__ReadOnlyArray<IHoverTip>(array);
						}
					}
				}
			}
		}
		return Array.Empty<IHoverTip>();
	}
}
