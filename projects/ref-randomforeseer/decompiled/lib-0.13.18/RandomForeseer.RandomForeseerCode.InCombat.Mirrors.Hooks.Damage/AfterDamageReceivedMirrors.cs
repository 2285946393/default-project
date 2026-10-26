using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal static class AfterDamageReceivedMirrors
{
	private static readonly MirrorMethodSpec AfterDamageReceived = MirrorMethodSpec.Hook("AfterDamageReceived", new Type[6]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature),
		typeof(DamageResult),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly MirrorMethodSpec AfterDamageReceivedLate = MirrorMethodSpec.Hook("AfterDamageReceivedLate", new Type[6]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature),
		typeof(DamageResult),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterDamageReceivedMirrorContext> Registry = CreateRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, AfterDamageReceivedMirrorContext> LateRegistry = new MethodMirrorRegistry<AbstractModel, AfterDamageReceivedMirrorContext>(AfterDamageReceivedLate);

	public static void Invoke(AbstractModel listener, AfterDamageReceivedMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	public static void InvokeLate(AbstractModel listener, AfterDamageReceivedMirrorContext context)
	{
		LateRegistry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterDamageReceivedMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterDamageReceivedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterDamageReceivedMirrorContext>(AfterDamageReceived);
		methodMirrorRegistry.RegisterIgnored<AsleepPower>();
		methodMirrorRegistry.Register<BeatingRemnant>((Action<BeatingRemnant, AfterDamageReceivedMirrorContext>)HandleBeatingRemnant);
		methodMirrorRegistry.Register<CentennialPuzzle>((Action<CentennialPuzzle, AfterDamageReceivedMirrorContext>)HandleCentennialPuzzle);
		methodMirrorRegistry.Register<CurlUpPower>((Action<CurlUpPower, AfterDamageReceivedMirrorContext>)HandleCurlUpPower);
		methodMirrorRegistry.Register<DemonTongue>((Action<DemonTongue, AfterDamageReceivedMirrorContext>)HandleDemonTongue);
		methodMirrorRegistry.RegisterIgnored<EmotionChip>();
		methodMirrorRegistry.Register<FlameBarrierPower>((Action<FlameBarrierPower, AfterDamageReceivedMirrorContext>)HandleFlameBarrierPower);
		methodMirrorRegistry.Register<FlutterPower>((Action<FlutterPower, AfterDamageReceivedMirrorContext>)HandleFlutterPower);
		methodMirrorRegistry.Register<HardenedShellPower>((Action<HardenedShellPower, AfterDamageReceivedMirrorContext>)HandleHardenedShellPower);
		methodMirrorRegistry.RegisterIgnored<LagavulinMatriarch>();
		methodMirrorRegistry.RegisterIgnored<LavaLamp>();
		methodMirrorRegistry.Register<InfernoPower>((Action<InfernoPower, AfterDamageReceivedMirrorContext>)HandleInfernoPower);
		methodMirrorRegistry.Register<PersonalHivePower>((Action<PersonalHivePower, AfterDamageReceivedMirrorContext>)HandlePersonalHivePower);
		methodMirrorRegistry.RegisterIgnored<PlowPower>();
		methodMirrorRegistry.Register<ReflectPower>((Action<ReflectPower, AfterDamageReceivedMirrorContext>)HandleReflectPower);
		methodMirrorRegistry.Register<RupturePower>((Action<RupturePower, AfterDamageReceivedMirrorContext>)HandleRupturePower);
		methodMirrorRegistry.Register<SelfFormingClay>((Action<SelfFormingClay, AfterDamageReceivedMirrorContext>)HandleSelfFormingClay);
		methodMirrorRegistry.RegisterIgnored<ShriekPower>();
		methodMirrorRegistry.Register<SlipperyPower>((Action<SlipperyPower, AfterDamageReceivedMirrorContext>)HandleSlipperyPower);
		methodMirrorRegistry.RegisterIgnored<SlumberPower>();
		methodMirrorRegistry.Register<TheGambitPower>((Action<TheGambitPower, AfterDamageReceivedMirrorContext>)HandleTheGambitPower);
		return methodMirrorRegistry;
	}

	private static void HandleBeatingRemnant(BeatingRemnant relic, AfterDamageReceivedMirrorContext context)
	{
		BeatingRemnant relic2 = relic;
		if (context.Target == ((RelicModel)relic2).Owner.Creature)
		{
			context.StateStore.Get((AbstractModel)(object)relic2, () => new BeatingRemnantPredictionState(relic2)).DamageReceivedThisTurn += (decimal)context.Result.UnblockedDamage;
		}
	}

	private static void HandleCentennialPuzzle(CentennialPuzzle relic, AfterDamageReceivedMirrorContext context)
	{
		CentennialPuzzle relic2 = relic;
		if (context.Target == ((RelicModel)relic2).Owner.Creature && context.Result.UnblockedDamage > 0)
		{
			CentennialPuzzlePredictionState centennialPuzzlePredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new CentennialPuzzlePredictionState(relic2));
			if (!centennialPuzzlePredictionState.UsedThisCombat)
			{
				centennialPuzzlePredictionState.UsedThisCombat = true;
				context.Simulator.Draw(((RelicModel)relic2).Owner, ((DynamicVar)((RelicModel)relic2).DynamicVars.Cards).BaseValue);
			}
		}
	}

	private static void HandleCurlUpPower(CurlUpPower power, AfterDamageReceivedMirrorContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (context.Target == ((PowerModel)power).Owner && ValuePropExtensions.IsPoweredAttack(context.Props) && context.Source != null)
		{
			CurlUpPredictionState curlUpPredictionState = context.StateStore.Get<CurlUpPredictionState>((AbstractModel)(object)power);
			if (curlUpPredictionState != null && !curlUpPredictionState.Consumed && curlUpPredictionState.PlayedCard == null)
			{
				curlUpPredictionState.PlayedCard = context.Source.Original;
			}
		}
	}

	private static void HandleDemonTongue(DemonTongue relic, AfterDamageReceivedMirrorContext context)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		DemonTongue relic2 = relic;
		if (context.CombatState.CurrentSide == ((RelicModel)relic2).Owner.Creature.Side && context.Target == ((RelicModel)relic2).Owner.Creature && context.Result.UnblockedDamage > 0)
		{
			DemonTonguePredictionState demonTonguePredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new DemonTonguePredictionState(relic2));
			if (!demonTonguePredictionState.TriggeredThisTurn)
			{
				demonTonguePredictionState.TriggeredThisTurn = true;
				context.Simulator.Heal(((RelicModel)relic2).Owner.Creature, context.Result.UnblockedDamage);
			}
		}
	}

	private static void HandleFlameBarrierPower(FlameBarrierPower power, AfterDamageReceivedMirrorContext context)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (context.Target == ((PowerModel)power).Owner && context.Dealer != null && ValuePropExtensions.IsPoweredAttack(context.Props))
		{
			context.Simulator.Damage(context.Dealer, ((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
		}
	}

	private static void HandleFlutterPower(FlutterPower power, AfterDamageReceivedMirrorContext context)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (context.Target == ((PowerModel)power).Owner && context.Result.UnblockedDamage != 0 && ValuePropExtensions.IsPoweredAttack(context.Props))
		{
			context.StateStore.GetPowerAmount((PowerModel)(object)power).Decrement();
		}
	}

	private static void HandleHardenedShellPower(HardenedShellPower power, AfterDamageReceivedMirrorContext context)
	{
		HardenedShellPower power2 = power;
		if (context.Target == ((PowerModel)power2).Owner && !context.Result.WasFullyBlocked)
		{
			HardenedShellPredictionState hardenedShellPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new HardenedShellPredictionState(power2));
			hardenedShellPredictionState.DamageReceivedThisTurn += (decimal)context.Result.UnblockedDamage;
			if (hardenedShellPredictionState.DamageReceivedThisTurn >= (decimal)((PowerModel)power2).Amount)
			{
				context.State.GetCreature(((PowerModel)power2).Owner).HpDisplay = (HpDisplay)1;
			}
		}
	}

	private static void HandleInfernoPower(InfernoPower power, AfterDamageReceivedMirrorContext context)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (context.Target == ((PowerModel)power).Owner && context.Result.UnblockedDamage > 0 && context.CombatState.CurrentSide == ((PowerModel)power).Owner.Side)
		{
			context.Simulator.Damage(context.State.HittableEnemies, ((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
		}
	}

	private static void HandlePersonalHivePower(PersonalHivePower power, AfterDamageReceivedMirrorContext context)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (context.Target == ((PowerModel)power).Owner && context.Dealer != null && ValuePropExtensions.IsPoweredAttack(context.Props))
		{
			Creature val = context.Dealer;
			if (val.Monster is Osty)
			{
				Player petOwner = val.PetOwner;
				val = ((petOwner != null) ? petOwner.Creature : null);
			}
			Player val2 = ((val != null) ? val.Player : null);
			if (val2 != null)
			{
				context.Simulator.CreateAndAddGeneratedCardsToCombat<Dazed>(val2, (PileType)1, ((PowerModel)power).Amount, (Player?)null, (CardPilePosition)3);
			}
		}
	}

	private static void HandleReflectPower(ReflectPower power, AfterDamageReceivedMirrorContext context)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		if (context.Target == ((PowerModel)power).Owner && context.Result.BlockedDamage > 0 && ValuePropExtensions.IsPoweredAttack(context.Props) && context.Dealer != null)
		{
			context.Simulator.Damage(context.Dealer, context.Result.BlockedDamage, (ValueProp)4, ((PowerModel)power).Owner);
		}
	}

	private static void HandleRupturePower(RupturePower power, AfterDamageReceivedMirrorContext context)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (context.Target == ((PowerModel)power).Owner && context.Result.UnblockedDamage > 0 && context.CombatState.CurrentSide == ((PowerModel)power).Owner.Side)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleSelfFormingClay(SelfFormingClay relic, AfterDamageReceivedMirrorContext context)
	{
		if (context.Target == ((RelicModel)relic).Owner.Creature)
		{
			_ = context.Result.UnblockedDamage;
			_ = 0;
		}
	}

	private static void HandleSlipperyPower(SlipperyPower power, AfterDamageReceivedMirrorContext context)
	{
		if (context.Target == ((PowerModel)power).Owner && context.Result.UnblockedDamage >= 1)
		{
			context.StateStore.GetPowerAmount((PowerModel)(object)power).Decrement();
		}
	}

	private static void HandleTheGambitPower(TheGambitPower power, AfterDamageReceivedMirrorContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (context.Target == ((PowerModel)power).Owner && ValuePropExtensions.IsPoweredAttack(context.Props) && context.Result.UnblockedDamage > 0)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
			context.Simulator.Kill(((PowerModel)power).Owner);
		}
	}
}
