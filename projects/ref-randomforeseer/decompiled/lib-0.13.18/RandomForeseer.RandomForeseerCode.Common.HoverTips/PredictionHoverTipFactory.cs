using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Localization;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal static class PredictionHoverTipFactory
{
	public const string HoverTipIdPrefix = "RandomForeseer:Prediction";

	public static IHoverTip Card(CardModel card)
	{
		return Card(card, isDimmed: false);
	}

	public static IHoverTip Card(CardModel card, bool isDimmed)
	{
		return (IHoverTip)(object)new PredictionCardHoverTip(card, isDimmed);
	}

	public static IHoverTip CardBundle(IReadOnlyList<CardModel> cards, PredictionCardBundleKind kind = PredictionCardBundleKind.Regular)
	{
		ArgumentOutOfRangeException.ThrowIfZero(cards.Count, "cards.Count");
		return (IHoverTip)(object)new PredictionCardBundleHoverTip(cards, kind);
	}

	public static IHoverTip Potion(PotionModel potion)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return (IHoverTip)(object)FromModelHoverTip(potion.HoverTip);
	}

	public static IHoverTip Relic(RelicModel relic)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return (IHoverTip)(object)FromModelHoverTip(relic.HoverTip);
	}

	public static IHoverTip Orb(OrbModel orb)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return (IHoverTip)(object)FromModelHoverTip(orb.DumbHoverTip);
	}

	public static HoverTip Text(string key, Action<LocString>? configureDescription = null)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		LocString val = ModLocalization.Text(key + ".title");
		LocString val2 = ModLocalization.Text(key + ".description");
		configureDescription?.Invoke(val2);
		HoverTip result = default(HoverTip);
		((HoverTip)(ref result))._002Ector(val, val2, (Texture2D)null);
		((HoverTip)(ref result)).Id = "RandomForeseer:Prediction:" + key;
		((HoverTip)(ref result)).IsInstanced = true;
		return result;
	}

	private static HoverTip FromModelHoverTip(HoverTip tip)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		((HoverTip)(ref tip)).Id = "RandomForeseer:Prediction:" + ((HoverTip)(ref tip)).Id;
		((HoverTip)(ref tip)).IsInstanced = true;
		((HoverTip)(ref tip)).CanonicalModel = null;
		return tip;
	}
}
