using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;

internal static class AfterAttackMirrors
{
	private static readonly MirrorMethodSpec AfterAttack = MirrorMethodSpec.Hook("AfterAttack", new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(AttackCommand)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterAttackMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterAttackMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterAttackMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterAttackMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterAttackMirrorContext>(AfterAttack);
		methodMirrorRegistry.Register<BoneFlute>((Action<BoneFlute, AfterAttackMirrorContext>)HandleBoneFlute);
		methodMirrorRegistry.Register<Flatten>((Action<Flatten, AfterAttackMirrorContext>)HandleFlatten);
		methodMirrorRegistry.Register<GigantificationPower>((Action<GigantificationPower, AfterAttackMirrorContext>)GigantificationPowerMirrors.AfterAttack);
		methodMirrorRegistry.Register<PainfulStabsPower>((Action<PainfulStabsPower, AfterAttackMirrorContext>)HandlePainfulStabsPower);
		methodMirrorRegistry.Register<SkittishPower>((Action<SkittishPower, AfterAttackMirrorContext>)HandleSkittishPower);
		methodMirrorRegistry.Register<SuckPower>((Action<SuckPower, AfterAttackMirrorContext>)HandleSuckPower);
		methodMirrorRegistry.Register<VigorPower>((Action<VigorPower, AfterAttackMirrorContext>)VigorPowerMirrors.AfterAttack);
		return methodMirrorRegistry;
	}

	private static void HandleBoneFlute(BoneFlute relic, AfterAttackMirrorContext context)
	{
		Creature attacker = context.Command.Attacker;
		if (((attacker != null) ? attacker.Monster : null) is Osty && context.Command.Attacker.PetOwner == ((RelicModel)relic).Owner)
		{
			context.Simulator.GainBlock(((RelicModel)relic).Owner.Creature, ((RelicModel)relic).DynamicVars.Block);
		}
	}

	private static void HandleFlatten(Flatten card, AfterAttackMirrorContext context)
	{
		if (context.Command.Attacker != null && context.Command.Attacker == ((CardModel)card).Owner.Osty)
		{
			PredictedCard? predictedCard = context.State.FindCard((CardModel)(object)card);
			if (predictedCard != null)
			{
				predictedCard.MutablePreview.EnergyCost.SetThisTurn(0, false);
			}
		}
	}

	private static void HandlePainfulStabsPower(PainfulStabsPower power, AfterAttackMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (context.Command.Attacker != ((PowerModel)power).Owner || context.Command.TargetSide == ((PowerModel)power).Owner.Side || !ValuePropExtensions.IsPoweredAttack(context.Command.DamageProps))
		{
			return;
		}
		foreach (IGrouping<Creature, DamageResult> item in from result in context.Command.Results.SelectMany((List<DamageResult> results) => results)
			where result.Receiver.IsPlayer
			group result by result.Receiver)
		{
			int count = item.Count((DamageResult result) => result.UnblockedDamage > 0) * ((PowerModel)power).Amount;
			context.Simulator.AddToCombat<Wound>(item.Key, (PileType)3, count, (Player?)null, (CardPilePosition)1);
		}
	}

	private static void HandleSkittishPower(SkittishPower power, AfterAttackMirrorContext context)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		SkittishPower power2 = power;
		SkittishPredictionState skittishPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new SkittishPredictionState(power2));
		if (!skittishPredictionState.HasGainedBlockThisTurn && ((Enum)context.Command.DamageProps).HasFlag((Enum)(object)(ValueProp)8) && context.Command.ModelSource is CardModel)
		{
			DamageResult? obj = context.Command.Results.SelectMany((List<DamageResult> results) => results).FirstOrDefault((Func<DamageResult, bool>)((DamageResult result) => result.Receiver == ((PowerModel)power2).Owner));
			if (obj != null && obj.UnblockedDamage > 0)
			{
				skittishPredictionState.HasGainedBlockThisTurn = true;
				context.Simulator.GainBlock(((PowerModel)power2).Owner, ((PowerModel)power2).Amount, (ValueProp)4);
			}
		}
	}

	private static void HandleSuckPower(SuckPower power, AfterAttackMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (context.Command.Attacker != ((PowerModel)power).Owner || context.Command.TargetSide == ((PowerModel)power).Owner.Side || !ValuePropExtensions.IsPoweredAttack(context.Command.DamageProps))
		{
			return;
		}
		int num = 0;
		foreach (List<DamageResult> result in context.Command.Results)
		{
			HashSet<Creature> petOwners = result.Where((DamageResult result) => result.Receiver.IsPet).Select(delegate(DamageResult result)
			{
				Player petOwner = result.Receiver.PetOwner;
				return (petOwner == null) ? null : petOwner.Creature;
			}).OfType<Creature>()
				.ToHashSet();
			if (result.Any((DamageResult result) => result.UnblockedDamage > 0 && !petOwners.Contains(result.Receiver)))
			{
				num++;
			}
		}
		if (num > 0)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}
}
