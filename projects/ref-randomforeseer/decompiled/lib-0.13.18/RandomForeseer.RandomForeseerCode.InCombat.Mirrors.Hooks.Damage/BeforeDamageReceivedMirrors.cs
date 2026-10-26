using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal static class BeforeDamageReceivedMirrors
{
	private static readonly MirrorMethodSpec BeforeDamageReceived = MirrorMethodSpec.Hook("BeforeDamageReceived", new Type[6]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature),
		typeof(decimal),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, BeforeDamageReceivedMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, BeforeDamageReceivedMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, BeforeDamageReceivedMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, BeforeDamageReceivedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, BeforeDamageReceivedMirrorContext>(BeforeDamageReceived);
		methodMirrorRegistry.Register<ThornsPower>((Action<ThornsPower, BeforeDamageReceivedMirrorContext>)HandleThornsPower);
		return methodMirrorRegistry;
	}

	private static void HandleThornsPower(ThornsPower power, BeforeDamageReceivedMirrorContext context)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (context.Target == ((PowerModel)power).Owner && context.Dealer != null && (ValuePropExtensions.IsPoweredAttack(context.Props) || context.Source?.Original is Omnislice))
		{
			context.Simulator.Damage(context.Dealer, ((PowerModel)power).Amount, (ValueProp)20, ((PowerModel)power).Owner);
		}
	}
}
