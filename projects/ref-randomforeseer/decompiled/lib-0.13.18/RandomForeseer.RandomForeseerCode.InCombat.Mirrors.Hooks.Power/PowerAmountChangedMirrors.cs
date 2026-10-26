using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Power;

internal static class PowerAmountChangedMirrors
{
	private static readonly MirrorMethodSpec BeforePowerAmountChanged = MirrorMethodSpec.Hook("BeforePowerAmountChanged", new Type[5]
	{
		typeof(PowerModel),
		typeof(decimal),
		typeof(Creature),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly MirrorMethodSpec AfterPowerAmountChanged = MirrorMethodSpec.Hook("AfterPowerAmountChanged", new Type[5]
	{
		typeof(PlayerChoiceContext),
		typeof(PowerModel),
		typeof(decimal),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly MirrorMethodSpec AfterModifyingGiven = MirrorMethodSpec.Hook("AfterModifyingPowerAmountGiven", new Type[1] { typeof(PowerModel) });

	private static readonly MirrorMethodSpec AfterModifyingReceived = MirrorMethodSpec.Hook("AfterModifyingPowerAmountReceived", new Type[1] { typeof(PowerModel) });

	private static readonly MethodMirrorRegistry<AbstractModel, PowerAmountChangedMirrorContext> BeforeRegistry = new MethodMirrorRegistry<AbstractModel, PowerAmountChangedMirrorContext>(BeforePowerAmountChanged);

	private static readonly MethodMirrorRegistry<AbstractModel, PowerAmountChangedMirrorContext> AfterRegistry = CreateAfterRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, AfterModifyingPowerAmountMirrorContext> AfterGivenRegistry = new MethodMirrorRegistry<AbstractModel, AfterModifyingPowerAmountMirrorContext>(AfterModifyingGiven);

	private static readonly MethodMirrorRegistry<AbstractModel, AfterModifyingPowerAmountMirrorContext> AfterReceivedRegistry = CreateAfterReceivedRegistry();

	public static void InvokeBefore(AbstractModel listener, PowerAmountChangedMirrorContext context)
	{
		BeforeRegistry.Invoke(listener, context);
	}

	public static void InvokeAfter(AbstractModel listener, PowerAmountChangedMirrorContext context)
	{
		AfterRegistry.Invoke(listener, context);
	}

	public static void InvokeAfterGiven(AbstractModel listener, AfterModifyingPowerAmountMirrorContext context)
	{
		AfterGivenRegistry.Invoke(listener, context);
	}

	public static void InvokeAfterReceived(AbstractModel listener, AfterModifyingPowerAmountMirrorContext context)
	{
		AfterReceivedRegistry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, PowerAmountChangedMirrorContext> CreateAfterRegistry()
	{
		MethodMirrorRegistry<AbstractModel, PowerAmountChangedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, PowerAmountChangedMirrorContext>(AfterPowerAmountChanged);
		methodMirrorRegistry.Register<ViciousPower>((Action<ViciousPower, PowerAmountChangedMirrorContext>)HandleVicious);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, AfterModifyingPowerAmountMirrorContext> CreateAfterReceivedRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterModifyingPowerAmountMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterModifyingPowerAmountMirrorContext>(AfterModifyingReceived);
		methodMirrorRegistry.Register<ArtifactPower>((Action<ArtifactPower, AfterModifyingPowerAmountMirrorContext>)HandleArtifactConsumed);
		return methodMirrorRegistry;
	}

	private static void HandleVicious(ViciousPower vicious, PowerAmountChangedMirrorContext context)
	{
		PowerAmountPredictionState powerAmount = context.StateStore.GetPowerAmount((PowerModel)(object)vicious);
		if (powerAmount.IsActive && context.Amount > 0m && context.Applier == ((PowerModel)vicious).Owner && context.Power is VulnerablePower)
		{
			Player player = ((PowerModel)vicious).Owner.Player;
			if (player != null)
			{
				context.Simulator.Draw(player, powerAmount.Amount);
			}
		}
	}

	private static void HandleArtifactConsumed(ArtifactPower artifact, AfterModifyingPowerAmountMirrorContext context)
	{
		context.StateStore.GetPowerAmount((PowerModel)(object)artifact).Decrement();
	}
}
