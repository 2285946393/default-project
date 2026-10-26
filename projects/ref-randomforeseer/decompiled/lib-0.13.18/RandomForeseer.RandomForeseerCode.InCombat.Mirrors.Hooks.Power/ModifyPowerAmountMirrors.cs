using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Power;

internal static class ModifyPowerAmountMirrors
{
	private static readonly MirrorMethodSpec ModifyGivenAdditive = MirrorMethodSpec.Hook("ModifyPowerAmountGivenAdditive", new Type[5]
	{
		typeof(PowerModel),
		typeof(Creature),
		typeof(decimal),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly MirrorMethodSpec ModifyGivenMultiplicative = MirrorMethodSpec.Hook("ModifyPowerAmountGivenMultiplicative", new Type[5]
	{
		typeof(PowerModel),
		typeof(Creature),
		typeof(decimal),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly MirrorMethodSpec TryModifyReceived = MirrorMethodSpec.Hook("TryModifyPowerAmountReceived", new Type[5]
	{
		typeof(PowerModel),
		typeof(Creature),
		typeof(decimal),
		typeof(Creature),
		typeof(decimal).MakeByRefType()
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyPowerAmountMirrorContext, decimal> AdditiveRegistry = new MethodMirrorRegistry<AbstractModel, ModifyPowerAmountMirrorContext, decimal>(ModifyGivenAdditive);

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyPowerAmountMirrorContext, decimal> MultiplicativeRegistry = new MethodMirrorRegistry<AbstractModel, ModifyPowerAmountMirrorContext, decimal>(ModifyGivenMultiplicative);

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyPowerAmountMirrorContext, PowerAmountModification> ReceivedRegistry = CreateReceivedRegistry();

	public static decimal InvokeGivenAdditive(AbstractModel listener, ModifyPowerAmountMirrorContext context)
	{
		if (!AdditiveRegistry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return listener.ModifyPowerAmountGivenAdditive(context.Power, context.Applier, context.Amount, context.Target, context.CardSource?.Preview);
		}
		return dispatchResult.Value;
	}

	public static decimal InvokeGivenMultiplicative(AbstractModel listener, ModifyPowerAmountMirrorContext context)
	{
		if (!MultiplicativeRegistry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return listener.ModifyPowerAmountGivenMultiplicative(context.Power, context.Applier, context.Amount, context.Target, context.CardSource?.Preview);
		}
		return dispatchResult.Value;
	}

	public static PowerAmountModification InvokeReceived(AbstractModel listener, ModifyPowerAmountMirrorContext context)
	{
		if (ReceivedRegistry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return dispatchResult.Value;
		}
		decimal amount = default(decimal);
		return new PowerAmountModification(listener.TryModifyPowerAmountReceived(context.Power, context.Target, context.Amount, context.Applier, ref amount), amount);
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyPowerAmountMirrorContext, PowerAmountModification> CreateReceivedRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyPowerAmountMirrorContext, PowerAmountModification> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyPowerAmountMirrorContext, PowerAmountModification>(TryModifyReceived);
		methodMirrorRegistry.Register<ArtifactPower>((Func<ArtifactPower, ModifyPowerAmountMirrorContext, PowerAmountModification>)HandleArtifact);
		return methodMirrorRegistry;
	}

	private static PowerAmountModification HandleArtifact(ArtifactPower artifact, ModifyPowerAmountMirrorContext context)
	{
		if (!context.StateStore.GetPowerAmount((PowerModel)(object)artifact).IsActive)
		{
			return new PowerAmountModification(WasModified: false, context.Amount);
		}
		decimal amount = default(decimal);
		return new PowerAmountModification(((AbstractModel)artifact).TryModifyPowerAmountReceived(context.Power, context.Target, context.Amount, context.Applier, ref amount), amount);
	}
}
