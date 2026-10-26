using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal static class ModifyUnblockedDamageTargetMirrors
{
	private static readonly MirrorMethodSpec Method = MirrorMethodSpec.Hook("ModifyUnblockedDamageTarget", new Type[4]
	{
		typeof(Creature),
		typeof(decimal),
		typeof(ValueProp),
		typeof(Creature)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyUnblockedDamageTargetMirrorContext, Creature> Registry = CreateRegistry();

	public static Creature Invoke(AbstractModel listener, ModifyUnblockedDamageTargetMirrorContext context)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (!Registry.TryInvokeRegistered(listener, context, out MirrorDispatchResult<Creature> dispatchResult))
		{
			return listener.ModifyUnblockedDamageTarget(context.Target, context.Amount, context.Props, context.Dealer);
		}
		return dispatchResult.Value;
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyUnblockedDamageTargetMirrorContext, Creature> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyUnblockedDamageTargetMirrorContext, Creature> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyUnblockedDamageTargetMirrorContext, Creature>(Method);
		methodMirrorRegistry.Register<DieForYouPower>((Func<DieForYouPower, ModifyUnblockedDamageTargetMirrorContext, Creature>)HandleDieForYouPower);
		return methodMirrorRegistry;
	}

	private static Creature HandleDieForYouPower(DieForYouPower power, ModifyUnblockedDamageTargetMirrorContext context)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Creature target = context.Target;
		Player petOwner = ((PowerModel)power).Owner.PetOwner;
		if (target != ((petOwner != null) ? petOwner.Creature : null) || !context.State.GetCreature(((PowerModel)power).Owner).IsAlive || !ValuePropExtensions.IsPoweredAttack(context.Props))
		{
			return context.Target;
		}
		return ((PowerModel)power).Owner;
	}
}
