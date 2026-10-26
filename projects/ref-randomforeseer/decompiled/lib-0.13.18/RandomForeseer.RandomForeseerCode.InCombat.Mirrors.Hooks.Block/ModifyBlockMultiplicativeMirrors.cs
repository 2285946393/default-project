using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Block;

internal static class ModifyBlockMultiplicativeMirrors
{
	private static readonly MirrorMethodSpec ModifyBlockMultiplicative = MirrorMethodSpec.Hook("ModifyBlockMultiplicative", new Type[5]
	{
		typeof(Creature),
		typeof(decimal),
		typeof(ValueProp),
		typeof(CardModel),
		typeof(CardPlay)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyBlockMultiplicativeMirrorContext, decimal> Registry = CreateRegistry();

	public static decimal Invoke(AbstractModel listener, ModifyBlockMultiplicativeMirrorContext context)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (Registry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return dispatchResult.Value;
		}
		return listener.ModifyBlockMultiplicative(context.Target, context.Amount, context.Props, context.CardSource?.Preview, context.CardPlay);
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyBlockMultiplicativeMirrorContext, decimal> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyBlockMultiplicativeMirrorContext, decimal> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyBlockMultiplicativeMirrorContext, decimal>(ModifyBlockMultiplicative);
		methodMirrorRegistry.Register<PaelsLegion>((Func<PaelsLegion, ModifyBlockMultiplicativeMirrorContext, decimal>)HandlePaelsLegion);
		methodMirrorRegistry.Register<Vambrace>((Func<Vambrace, ModifyBlockMultiplicativeMirrorContext, decimal>)HandleVambrace);
		return methodMirrorRegistry;
	}

	private static decimal HandlePaelsLegion(PaelsLegion relic, ModifyBlockMultiplicativeMirrorContext context)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		PaelsLegion relic2 = relic;
		PaelsLegionPredictionState paelsLegionPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new PaelsLegionPredictionState(relic2));
		int num;
		if (ValuePropExtensions.IsCardOrMonsterMove(context.Props))
		{
			PredictedCard? cardSource = context.CardSource;
			if (((cardSource != null) ? cardSource.Preview.Owner : null) == ((RelicModel)relic2).Owner && paelsLegionPredictionState.Cooldown <= 0)
			{
				num = 2;
				goto IL_0069;
			}
		}
		num = 1;
		goto IL_0069;
		IL_0069:
		return num;
	}

	private static decimal HandleVambrace(Vambrace relic, ModifyBlockMultiplicativeMirrorContext context)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		Vambrace relic2 = relic;
		VambracePredictionState vambracePredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new VambracePredictionState(relic2));
		int num;
		if (ValuePropExtensions.IsCardOrMonsterMove(context.Props))
		{
			PredictedCard? cardSource = context.CardSource;
			if (((cardSource != null) ? cardSource.Preview.Owner : null) == ((RelicModel)relic2).Owner && !vambracePredictionState.BlockGainedThisCombat && (vambracePredictionState.TriggeringCard == null || context.CardSource.Original == vambracePredictionState.TriggeringCard))
			{
				num = 2;
				goto IL_0083;
			}
		}
		num = 1;
		goto IL_0083;
		IL_0083:
		return num;
	}
}
