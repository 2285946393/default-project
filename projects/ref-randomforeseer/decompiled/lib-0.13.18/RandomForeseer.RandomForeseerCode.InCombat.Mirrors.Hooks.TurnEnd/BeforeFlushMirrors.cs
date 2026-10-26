using System;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;

internal static class BeforeFlushMirrors
{
	private static readonly MirrorMethodSpec BeforeFlush = MirrorMethodSpec.Hook("BeforeFlush", new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(Player)
	});

	private static readonly MirrorMethodSpec BeforeFlushLate = MirrorMethodSpec.Hook("BeforeFlushLate", new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(Player)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, BeforeFlushMirrorContext> BeforeFlushRegistry = CreateRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, BeforeFlushMirrorContext> BeforeFlushLateRegistry = new MethodMirrorRegistry<AbstractModel, BeforeFlushMirrorContext>(BeforeFlushLate);

	public static void Invoke(AbstractModel listener, BeforeFlushMirrorContext context)
	{
		BeforeFlushRegistry.Invoke(listener, context);
	}

	public static void InvokeLate(AbstractModel listener, BeforeFlushMirrorContext context)
	{
		BeforeFlushLateRegistry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, BeforeFlushMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, BeforeFlushMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, BeforeFlushMirrorContext>(BeforeFlush);
		methodMirrorRegistry.Register<SlumberingEssence>((Action<SlumberingEssence, BeforeFlushMirrorContext>)HandleSlumberingEssence);
		return methodMirrorRegistry;
	}

	private static void HandleSlumberingEssence(SlumberingEssence enchantment, BeforeFlushMirrorContext context)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		if (context.Player != ((EnchantmentModel)enchantment).Card.Owner)
		{
			return;
		}
		PredictedCard predictedCard = context.State.FindCard(((EnchantmentModel)enchantment).Card);
		if (predictedCard != null)
		{
			SimCardPile pile = predictedCard.GetPile(context.State);
			if (pile != null && (int)pile.Type == 2)
			{
				predictedCard.MutablePreview.EnergyCost.AddUntilPlayed(-1, false);
			}
		}
	}
}
