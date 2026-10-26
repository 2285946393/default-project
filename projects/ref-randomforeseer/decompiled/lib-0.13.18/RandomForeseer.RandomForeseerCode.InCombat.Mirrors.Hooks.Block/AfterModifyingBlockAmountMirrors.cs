using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Block;

internal static class AfterModifyingBlockAmountMirrors
{
	private static readonly MirrorMethodSpec AfterModifyingBlockAmount = MirrorMethodSpec.Hook("AfterModifyingBlockAmount", new Type[3]
	{
		typeof(decimal),
		typeof(CardModel),
		typeof(CardPlay)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterModifyingBlockAmountMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterModifyingBlockAmountMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterModifyingBlockAmountMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterModifyingBlockAmountMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterModifyingBlockAmountMirrorContext>(AfterModifyingBlockAmount);
		methodMirrorRegistry.RegisterIgnored<FastenPower>();
		methodMirrorRegistry.Register<PaelsLegion>((Action<PaelsLegion, AfterModifyingBlockAmountMirrorContext>)HandlePaelsLegion);
		methodMirrorRegistry.Register<Vambrace>((Action<Vambrace, AfterModifyingBlockAmountMirrorContext>)HandleVambrace);
		return methodMirrorRegistry;
	}

	private static void HandlePaelsLegion(PaelsLegion relic, AfterModifyingBlockAmountMirrorContext context)
	{
		PaelsLegion relic2 = relic;
		if (!(context.ModifiedBlock <= 0m) && context.CardPlay != null)
		{
			PaelsLegionPredictionState paelsLegionPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new PaelsLegionPredictionState(relic2));
			if (paelsLegionPredictionState.AffectedCardPlay == null)
			{
				CardPlay val = (paelsLegionPredictionState.AffectedCardPlay = context.CardPlay);
			}
		}
	}

	private static void HandleVambrace(Vambrace relic, AfterModifyingBlockAmountMirrorContext context)
	{
		Vambrace relic2 = relic;
		if (!(context.ModifiedBlock <= 0m) && context.CardSource != null)
		{
			context.StateStore.Get((AbstractModel)(object)relic2, () => new VambracePredictionState(relic2)).TriggeringCard = context.CardSource.Original;
		}
	}
}
