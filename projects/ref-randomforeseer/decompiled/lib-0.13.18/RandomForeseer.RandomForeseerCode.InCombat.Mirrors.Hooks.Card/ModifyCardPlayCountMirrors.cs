using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class ModifyCardPlayCountMirrors
{
	private static readonly MirrorMethodSpec ModifyCardPlayCount = MirrorMethodSpec.Hook("ModifyCardPlayCount", new Type[3]
	{
		typeof(CardModel),
		typeof(Creature),
		typeof(int)
	});

	private static readonly MirrorMethodSpec AfterModifyingCardPlayCount = MirrorMethodSpec.Hook("AfterModifyingCardPlayCount", new Type[1] { typeof(CardModel) });

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyCardPlayCountMirrorContext, int> Registry = CreateRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, AfterModifyingCardPlayCountMirrorContext> AfterRegistry = CreateAfterRegistry();

	public static int Invoke(AbstractModel listener, ModifyCardPlayCountMirrorContext context)
	{
		if (!Registry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return InvokeOriginal(listener, context);
		}
		return dispatchResult.Value;
	}

	public static void InvokeAfter(AbstractModel listener, AfterModifyingCardPlayCountMirrorContext context)
	{
		AfterRegistry.Invoke(listener, context);
	}

	private static int InvokeOriginal(AbstractModel listener, ModifyCardPlayCountMirrorContext context)
	{
		return listener.ModifyCardPlayCount(context.Card.Preview, context.Target, context.PlayCount);
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyCardPlayCountMirrorContext, int> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyCardPlayCountMirrorContext, int> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyCardPlayCountMirrorContext, int>(ModifyCardPlayCount);
		methodMirrorRegistry.Register<BurstPower>((Func<BurstPower, ModifyCardPlayCountMirrorContext, int>)HandleConsumablePower);
		methodMirrorRegistry.Register<DuplicationPower>((Func<DuplicationPower, ModifyCardPlayCountMirrorContext, int>)HandleConsumablePower);
		methodMirrorRegistry.Register<EchoFormPower>((Func<EchoFormPower, ModifyCardPlayCountMirrorContext, int>)HandleEchoFormPower);
		methodMirrorRegistry.Register<OneTwoPunchPower>((Func<OneTwoPunchPower, ModifyCardPlayCountMirrorContext, int>)HandleConsumablePower);
		methodMirrorRegistry.Register<SignalBoostPower>((Func<SignalBoostPower, ModifyCardPlayCountMirrorContext, int>)HandleConsumablePower);
		methodMirrorRegistry.Register<TagTeamPower>((Func<TagTeamPower, ModifyCardPlayCountMirrorContext, int>)HandleConsumablePower);
		methodMirrorRegistry.Register<ThrowingAxe>((Func<ThrowingAxe, ModifyCardPlayCountMirrorContext, int>)HandleThrowingAxe);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, AfterModifyingCardPlayCountMirrorContext> CreateAfterRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterModifyingCardPlayCountMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterModifyingCardPlayCountMirrorContext>(AfterModifyingCardPlayCount);
		methodMirrorRegistry.Register<BurstPower>((Action<BurstPower, AfterModifyingCardPlayCountMirrorContext>)HandleDecrementPower);
		methodMirrorRegistry.Register<DuplicationPower>((Action<DuplicationPower, AfterModifyingCardPlayCountMirrorContext>)HandleDecrementPower);
		methodMirrorRegistry.RegisterIgnored<EchoFormPower>();
		methodMirrorRegistry.Register<OneTwoPunchPower>((Action<OneTwoPunchPower, AfterModifyingCardPlayCountMirrorContext>)HandleDecrementPower);
		methodMirrorRegistry.Register<SignalBoostPower>((Action<SignalBoostPower, AfterModifyingCardPlayCountMirrorContext>)HandleDecrementPower);
		methodMirrorRegistry.Register<TagTeamPower>((Action<TagTeamPower, AfterModifyingCardPlayCountMirrorContext>)HandleConsumePower);
		methodMirrorRegistry.Register<ThrowingAxe>((Action<ThrowingAxe, AfterModifyingCardPlayCountMirrorContext>)HandleThrowingAxeAfter);
		return methodMirrorRegistry;
	}

	private static int HandleConsumablePower(PowerModel power, ModifyCardPlayCountMirrorContext context)
	{
		if (!context.StateStore.GetPowerAmount(power).IsActive)
		{
			return context.PlayCount;
		}
		return InvokeOriginal((AbstractModel)(object)power, context);
	}

	private static int HandleEchoFormPower(EchoFormPower power, ModifyCardPlayCountMirrorContext context)
	{
		EchoFormPower power2 = power;
		if (context.Card.Preview.Owner.Creature != ((PowerModel)power2).Owner)
		{
			return context.PlayCount;
		}
		if (CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry entry) => ((CombatHistoryEntry)entry).Actor == ((PowerModel)power2).Owner && entry.CardPlay.IsFirstInSeries && ((CombatHistoryEntry)entry).HappenedThisTurn(((PowerModel)power2).CombatState)) + context.History.OfType<CombatPredictionCardPlayStartedEntry>().Count((CombatPredictionCardPlayStartedEntry entry) => entry.CardPlay.Player.Creature == ((PowerModel)power2).Owner && entry.CardPlay.IsFirstInSeries) >= ((PowerModel)power2).Amount)
		{
			return context.PlayCount;
		}
		return context.PlayCount + 1;
	}

	private static int HandleThrowingAxe(ThrowingAxe relic, ModifyCardPlayCountMirrorContext context)
	{
		ThrowingAxe relic2 = relic;
		if (context.StateStore.Get((AbstractModel)(object)relic2, () => new ThrowingAxePredictionState(relic2)).UsedThisCombat || context.Card.Preview.Owner != ((RelicModel)relic2).Owner)
		{
			return context.PlayCount;
		}
		return context.PlayCount + 1;
	}

	private static void HandleDecrementPower(PowerModel power, AfterModifyingCardPlayCountMirrorContext context)
	{
		context.StateStore.GetPowerAmount(power).Decrement();
	}

	private static void HandleConsumePower(TagTeamPower power, AfterModifyingCardPlayCountMirrorContext context)
	{
		context.StateStore.GetPowerAmount((PowerModel)(object)power).Consume();
	}

	private static void HandleThrowingAxeAfter(ThrowingAxe relic, AfterModifyingCardPlayCountMirrorContext context)
	{
		ThrowingAxe relic2 = relic;
		context.StateStore.Get((AbstractModel)(object)relic2, () => new ThrowingAxePredictionState(relic2)).UsedThisCombat = true;
	}
}
