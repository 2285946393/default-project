using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Block;

internal static class BeforeBlockGainedMirrors
{
	private static readonly MirrorMethodSpec BeforeBlockGained = MirrorMethodSpec.Hook("BeforeBlockGained", new Type[4]
	{
		typeof(Creature),
		typeof(decimal),
		typeof(ValueProp),
		typeof(CardModel)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, BeforeBlockGainedMirrorContext> Registry = new MethodMirrorRegistry<AbstractModel, BeforeBlockGainedMirrorContext>(BeforeBlockGained);

	public static void Invoke(AbstractModel listener, BeforeBlockGainedMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}
}
