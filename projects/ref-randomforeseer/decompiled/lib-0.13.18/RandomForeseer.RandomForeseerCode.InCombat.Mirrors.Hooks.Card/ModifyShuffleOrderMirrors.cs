using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class ModifyShuffleOrderMirrors
{
	private static readonly MirrorMethodSpec ModifyShuffleOrder = MirrorMethodSpec.Hook("ModifyShuffleOrder", new Type[3]
	{
		typeof(Player),
		typeof(List<CardModel>),
		typeof(bool)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyShuffleOrderMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, ModifyShuffleOrderMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyShuffleOrderMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyShuffleOrderMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyShuffleOrderMirrorContext>(ModifyShuffleOrder);
		methodMirrorRegistry.Register<PerfectFit>((Action<PerfectFit, ModifyShuffleOrderMirrorContext>)HandlePerfectFit);
		return methodMirrorRegistry;
	}

	private static void HandlePerfectFit(PerfectFit enchantment, ModifyShuffleOrderMirrorContext context)
	{
		PerfectFit enchantment2 = enchantment;
		if (!context.IsInitialShuffle)
		{
			PredictedCard predictedCard = context.Cards.FirstOrDefault((PredictedCard card) => card.References(((EnchantmentModel)enchantment2).Card));
			if (predictedCard != null)
			{
				context.Cards.Remove(predictedCard);
				context.Cards.Insert(0, predictedCard);
			}
		}
	}
}
