using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Enchantments.OnPlay;

internal static class EnchantmentOnPlayMirrors
{
	private static readonly MirrorMethodSpec OnPlay = new MirrorMethodSpec(typeof(EnchantmentModel), "OnPlay", BindingFlags.Instance | BindingFlags.Public, new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(CardPlay)
	});

	private static readonly MethodMirrorRegistry<EnchantmentModel, EnchantmentOnPlayMirrorContext> Registry = CreateRegistry();

	public static MirrorDispatchResult Invoke(CombatPredictionSimulator simulator, PredictedCard card, CardPlay cardPlay, EnchantmentModel enchantment)
	{
		return Registry.Invoke(enchantment, new EnchantmentOnPlayMirrorContext
		{
			Simulator = simulator,
			Card = card,
			CardPlay = cardPlay
		});
	}

	private static MethodMirrorRegistry<EnchantmentModel, EnchantmentOnPlayMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<EnchantmentModel, EnchantmentOnPlayMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<EnchantmentModel, EnchantmentOnPlayMirrorContext>(OnPlay);
		methodMirrorRegistry.Register<Adroit>((Action<Adroit, EnchantmentOnPlayMirrorContext>)HandleAdroit);
		methodMirrorRegistry.Register<Corrupted>((Action<Corrupted, EnchantmentOnPlayMirrorContext>)HandleCorrupted);
		methodMirrorRegistry.Register<Inky>((Action<Inky, EnchantmentOnPlayMirrorContext>)HandleInky);
		methodMirrorRegistry.Register<Momentum>((Action<Momentum, EnchantmentOnPlayMirrorContext>)HandleMomentum);
		methodMirrorRegistry.Register<Sown>((Action<Sown, EnchantmentOnPlayMirrorContext>)HandleSown);
		methodMirrorRegistry.Register<Swift>((Action<Swift, EnchantmentOnPlayMirrorContext>)HandleSwift);
		return methodMirrorRegistry;
	}

	private static void HandleAdroit(Adroit enchantment, EnchantmentOnPlayMirrorContext context)
	{
		context.Simulator.GainBlock(context.PreviewCard.Owner.Creature, ((EnchantmentModel)enchantment).DynamicVars.Block, context.Card, context.CardPlay);
	}

	private static void HandleCorrupted(Corrupted enchantment, EnchantmentOnPlayMirrorContext context)
	{
		Creature creature = context.PreviewCard.Owner.Creature;
		context.Simulator.Damage(new _003C_003Ez__ReadOnlySingleElementList<Creature>(creature), 2m, (ValueProp)14, creature, context.Card, context.CardPlay);
	}

	private static void HandleInky(Inky enchantment, EnchantmentOnPlayMirrorContext context)
	{
		context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
	}

	private static void HandleMomentum(Momentum enchantment, EnchantmentOnPlayMirrorContext context)
	{
		enchantment._extraDamage += ((EnchantmentModel)enchantment).Amount;
	}

	private static void HandleSown(Sown enchantment, EnchantmentOnPlayMirrorContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((EnchantmentModel)enchantment).Status == 0)
		{
			((EnchantmentModel)enchantment)._status = (EnchantmentStatus)1;
			context.Simulator.GainEnergy(context.PreviewCard.Owner, ((EnchantmentModel)enchantment).Amount);
		}
	}

	private static void HandleSwift(Swift enchantment, EnchantmentOnPlayMirrorContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if ((int)((EnchantmentModel)enchantment).Status == 0)
		{
			((EnchantmentModel)enchantment)._status = (EnchantmentStatus)1;
			context.Simulator.Draw(context.PreviewCard.Owner, ((EnchantmentModel)enchantment).Amount);
		}
	}
}
