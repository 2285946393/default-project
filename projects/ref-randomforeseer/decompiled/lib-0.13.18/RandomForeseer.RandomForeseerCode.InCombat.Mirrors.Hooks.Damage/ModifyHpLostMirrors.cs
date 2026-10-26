using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal static class ModifyHpLostMirrors
{
	private static readonly Type[] ModifyHpLostParamTypes = new Type[5]
	{
		typeof(Creature),
		typeof(decimal),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel)
	};

	private static readonly MirrorMethodSpec ModifyHpLostBeforeOsty = MirrorMethodSpec.Hook("ModifyHpLostBeforeOsty", ModifyHpLostParamTypes);

	private static readonly MirrorMethodSpec ModifyHpLostBeforeOstyLate = MirrorMethodSpec.Hook("ModifyHpLostBeforeOstyLate", ModifyHpLostParamTypes);

	private static readonly MirrorMethodSpec ModifyHpLostAfterOsty = MirrorMethodSpec.Hook("ModifyHpLostAfterOsty", ModifyHpLostParamTypes);

	private static readonly MirrorMethodSpec ModifyHpLostAfterOstyLate = MirrorMethodSpec.Hook("ModifyHpLostAfterOstyLate", ModifyHpLostParamTypes);

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal> BeforeOstyRegistry = new MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal>(ModifyHpLostBeforeOsty);

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal> BeforeOstyLateRegistry = CreateBeforeOstyLateRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal> AfterOstyRegistry = CreateAfterOstyRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal> AfterOstyLateRegistry = CreateAfterOstyLateRegistry();

	public static decimal InvokeBeforeOsty(AbstractModel listener, ModifyHpLostMirrorContext context)
	{
		if (!BeforeOstyRegistry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return InvokeOriginalBeforeOsty(listener, context);
		}
		return dispatchResult.Value;
	}

	public static decimal InvokeBeforeOstyLate(AbstractModel listener, ModifyHpLostMirrorContext context)
	{
		if (!BeforeOstyLateRegistry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return InvokeOriginalBeforeOstyLate(listener, context);
		}
		return dispatchResult.Value;
	}

	public static decimal InvokeAfterOsty(AbstractModel listener, ModifyHpLostMirrorContext context)
	{
		if (!AfterOstyRegistry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return InvokeOriginalAfterOsty(listener, context);
		}
		return dispatchResult.Value;
	}

	public static decimal InvokeAfterOstyLate(AbstractModel listener, ModifyHpLostMirrorContext context)
	{
		if (!AfterOstyLateRegistry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return InvokeOriginalAfterOstyLate(listener, context);
		}
		return dispatchResult.Value;
	}

	private static decimal InvokeOriginalBeforeOsty(AbstractModel listener, ModifyHpLostMirrorContext context)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return listener.ModifyHpLostBeforeOsty(context.Target, context.Amount, context.Props, context.Dealer, context.CardSource?.Preview);
	}

	private static decimal InvokeOriginalBeforeOstyLate(AbstractModel listener, ModifyHpLostMirrorContext context)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return listener.ModifyHpLostBeforeOstyLate(context.Target, context.Amount, context.Props, context.Dealer, context.CardSource?.Preview);
	}

	private static decimal InvokeOriginalAfterOsty(AbstractModel listener, ModifyHpLostMirrorContext context)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return listener.ModifyHpLostAfterOsty(context.Target, context.Amount, context.Props, context.Dealer, context.CardSource?.Preview);
	}

	private static decimal InvokeOriginalAfterOstyLate(AbstractModel listener, ModifyHpLostMirrorContext context)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return listener.ModifyHpLostAfterOstyLate(context.Target, context.Amount, context.Props, context.Dealer, context.CardSource?.Preview);
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal> CreateBeforeOstyLateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal>(ModifyHpLostBeforeOstyLate);
		methodMirrorRegistry.Register<HardenedShellPower>((Func<HardenedShellPower, ModifyHpLostMirrorContext, decimal>)HandleHardenedShellPower);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal> CreateAfterOstyRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal>(ModifyHpLostAfterOsty);
		methodMirrorRegistry.Register<BeatingRemnant>((Func<BeatingRemnant, ModifyHpLostMirrorContext, decimal>)HandleBeatingRemnant);
		methodMirrorRegistry.Register<IntangiblePower>((Func<IntangiblePower, ModifyHpLostMirrorContext, decimal>)HandleIntangiblePower);
		methodMirrorRegistry.Register<SlipperyPower>((Func<SlipperyPower, ModifyHpLostMirrorContext, decimal>)HandleSlipperyPower);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal> CreateAfterOstyLateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyHpLostMirrorContext, decimal>(ModifyHpLostAfterOstyLate);
		methodMirrorRegistry.Register<BufferPower>((Func<BufferPower, ModifyHpLostMirrorContext, decimal>)HandleBufferPower);
		return methodMirrorRegistry;
	}

	private static decimal HandleHardenedShellPower(HardenedShellPower power, ModifyHpLostMirrorContext context)
	{
		HardenedShellPower power2 = power;
		if (context.Target != ((PowerModel)power2).Owner || context.Amount == 0m)
		{
			return context.Amount;
		}
		HardenedShellPredictionState hardenedShellPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new HardenedShellPredictionState(power2));
		return Math.Min(context.Amount, (decimal)((PowerModel)power2).Amount - hardenedShellPredictionState.DamageReceivedThisTurn);
	}

	private static decimal HandleBeatingRemnant(BeatingRemnant relic, ModifyHpLostMirrorContext context)
	{
		BeatingRemnant relic2 = relic;
		if (!context.Simulator.IsInProgress || context.Target != ((RelicModel)relic2).Owner.Creature)
		{
			return context.Amount;
		}
		BeatingRemnantPredictionState beatingRemnantPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new BeatingRemnantPredictionState(relic2));
		decimal baseValue = ((RelicModel)relic2).DynamicVars["MaxHpLoss"].BaseValue;
		return Math.Min(context.Amount, baseValue - beatingRemnantPredictionState.DamageReceivedThisTurn);
	}

	private static decimal HandleIntangiblePower(IntangiblePower power, ModifyHpLostMirrorContext context)
	{
		if (!context.Simulator.IsInProgress || context.Target != ((PowerModel)power).Owner)
		{
			return context.Amount;
		}
		return Math.Min(context.Amount, 1m);
	}

	private static decimal HandleSlipperyPower(SlipperyPower power, ModifyHpLostMirrorContext context)
	{
		if (!context.StateStore.GetPowerAmount((PowerModel)(object)power).IsActive)
		{
			return context.Amount;
		}
		return InvokeOriginalAfterOsty((AbstractModel)(object)power, context);
	}

	private static decimal HandleBufferPower(BufferPower power, ModifyHpLostMirrorContext context)
	{
		if (!context.StateStore.GetPowerAmount((PowerModel)(object)power).IsActive)
		{
			return context.Amount;
		}
		return InvokeOriginalAfterOstyLate((AbstractModel)(object)power, context);
	}
}
