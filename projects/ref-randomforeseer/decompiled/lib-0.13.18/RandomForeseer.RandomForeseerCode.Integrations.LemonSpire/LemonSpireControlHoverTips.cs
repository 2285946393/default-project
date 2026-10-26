using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Integrations.LemonSpire;

internal static class LemonSpireControlHoverTips
{
	private static readonly ConditionalWeakTable<Control, LemonSpirePredictionContext> Contexts = new ConditionalWeakTable<Control, LemonSpirePredictionContext>();

	public static void Register(Control control, Player player, AbstractModel model, LemonSpirePredictionKind kind)
	{
		Contexts.AddOrUpdate(control, new LemonSpirePredictionContext(player, model, kind));
	}

	public static IReadOnlyList<IHoverTip> GetHoverTips(Control control)
	{
		if (!Contexts.TryGetValue(control, out LemonSpirePredictionContext value))
		{
			return Array.Empty<IHoverTip>();
		}
		return value.GetHoverTips();
	}
}
