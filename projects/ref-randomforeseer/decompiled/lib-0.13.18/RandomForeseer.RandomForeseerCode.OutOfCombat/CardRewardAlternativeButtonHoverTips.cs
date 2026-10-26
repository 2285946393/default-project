using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Rewards;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class CardRewardAlternativeButtonHoverTips
{
	private sealed record PredictionContext(CardReward Reward, CardRewardAlternative Alternative);

	private const string RerollKey = "REROLL";

	private const string SacrificeKey = "SACRIFICE";

	private static readonly ConditionalWeakTable<CardRewardAlternative, CardReward> Sources = new ConditionalWeakTable<CardRewardAlternative, CardReward>();

	private static readonly ConditionalWeakTable<Control, PredictionContext> Contexts = new ConditionalWeakTable<Control, PredictionContext>();

	public static void RegisterReward(CardReward reward, IEnumerable<CardRewardAlternative> alternatives)
	{
		foreach (CardRewardAlternative alternative in alternatives)
		{
			string optionId = alternative.OptionId;
			if ((optionId == "REROLL" || optionId == "SACRIFICE") ? true : false)
			{
				Sources.AddOrUpdate(alternative, reward);
			}
		}
	}

	public static void RegisterButton(Control button, CardRewardAlternative alternative)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		if (Sources.TryGetValue(alternative, out CardReward value) && Contexts.TryAdd(button, new PredictionContext(value, alternative)))
		{
			((GodotObject)button).Connect(SignalName.Focused, Callable.From<NClickableControl>((Action<NClickableControl>)ShowPrediction), 0u);
			((GodotObject)button).Connect(SignalName.Unfocused, Callable.From<NClickableControl>((Action<NClickableControl>)HidePrediction), 0u);
		}
	}

	public static IReadOnlyList<IHoverTip> GetHoverTips(Control owner)
	{
		if (!Contexts.TryGetValue(owner, out PredictionContext value))
		{
			return Array.Empty<IHoverTip>();
		}
		string optionId = value.Alternative.OptionId;
		if (!(optionId == "REROLL"))
		{
			if (optionId == "SACRIFICE")
			{
				return PaelsWingSacrificePrediction.GetHoverTips(value.Reward, value.Alternative);
			}
			return Array.Empty<IHoverTip>();
		}
		return CardRewardRerollPrediction.GetHoverTips(value.Reward);
	}

	private static void ShowPrediction(Control button)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		PredictionHoverTipSetHelper.EnsureHoverTipSet(button, HoverTip.GetHoverTipAlignment(button, 0.75f));
	}

	private static void HidePrediction(Control button)
	{
		PredictionHoverTipSetHelper.RemoveOwnedHoverTipSet(button);
	}
}
