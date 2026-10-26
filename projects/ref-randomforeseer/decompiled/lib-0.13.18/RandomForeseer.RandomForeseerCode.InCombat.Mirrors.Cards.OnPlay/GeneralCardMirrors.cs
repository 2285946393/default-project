using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class GeneralCardMirrors
{
	public static void GeneralPowerApplicationOnPlay<T>(CardModel card, CardOnPlayMirrorContext context) where T : PowerModel
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Invalid comparison between Unknown and I4
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between Unknown and I4
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		DynamicVar dynamicVar;
		decimal amount = (TryGetDynamicVar(card, new _003C_003Ez__ReadOnlyArray<string>(new string[2]
		{
			typeof(T).Name,
			"Power"
		}), out dynamicVar) ? context.Calculate(dynamicVar) : 1m);
		TargetType targetType = card.TargetType;
		IReadOnlyList<Creature> targets;
		if ((int)targetType != 2)
		{
			if ((int)targetType != 3)
			{
				Entry.Logger.Warn($"{typeof(T).Name} application {((AbstractModel)card).Id} has an unsupported target type: {card.TargetType}", 1);
				context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
				return;
			}
			targets = context.State.HittableEnemies;
		}
		else
		{
			targets = new _003C_003Ez__ReadOnlySingleElementList<Creature>(context.Target);
		}
		context.Simulator.ApplyPower<T>(targets, amount, card.Owner.Creature, context.Card);
	}

	public static void GeneralOwnerDrawOneOnPlay(CardModel card, CardOnPlayMirrorContext context)
	{
		context.Simulator.Draw(card.Owner, 1);
	}

	public static void GeneralOwnerDrawOnPlay(CardModel card, CardOnPlayMirrorContext context)
	{
		DynamicVar dynamicVar = default(DynamicVar);
		if (!card.DynamicVars.TryGetValue("Cards", ref dynamicVar))
		{
			Entry.Logger.Warn($"Card {((AbstractModel)card).Id} has no cards var to simulate a draw.", 1);
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
		else
		{
			context.Simulator.Draw(card.Owner, context.Calculate(dynamicVar));
		}
	}

	public static void GeneralAttackOnPlay(CardModel card, CardOnPlayMirrorContext context)
	{
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Expected I4, but got Unknown
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		if (!TryGetDynamicVar(card, new _003C_003Ez__ReadOnlyArray<string>(new string[3] { "CalculatedDamage", "Damage", "OstyDamage" }), out DynamicVar dynamicVar))
		{
			Entry.Logger.Warn($"Card {((AbstractModel)card).Id} has no damage var to simulate an attack command.", 1);
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
			return;
		}
		CalculatedDamageVar val = (CalculatedDamageVar)(object)((dynamicVar is CalculatedDamageVar) ? dynamicVar : null);
		AttackCommand val2 = ((val != null) ? DamageCmd.Attack(val) : DamageCmd.Attack(context.Calculate(dynamicVar)));
		if (card.Tags.Contains((CardTag)4))
		{
			Creature osty = card.Owner.Osty;
			if (osty == null || !context.State.GetCreature(osty).IsAlive)
			{
				return;
			}
			val2.FromOsty(osty, card, context.CardPlay);
		}
		else
		{
			val2.FromCard(card, context.CardPlay);
		}
		if (TryGetDynamicVar(card, new _003C_003Ez__ReadOnlyArray<string>(new string[2] { "Repeat", "CalculatedHits" }), out DynamicVar dynamicVar2))
		{
			val2.WithHitCount((int)context.Calculate(dynamicVar2));
		}
		else if (card.EnergyCost.CostsX)
		{
			val2.WithHitCount(context.ResolveEnergyXValue());
		}
		else if (card.HasStarCostX)
		{
			val2.WithHitCount(context.ResolveStarXValue());
		}
		TargetType targetType = card.TargetType;
		switch (targetType - 2)
		{
		case 0:
			val2.Targeting(context.Target);
			break;
		case 1:
			val2.TargetingAllOpponents(context.CombatState);
			break;
		case 2:
			val2.TargetingRandomOpponents(context.CombatState, true);
			break;
		default:
			Entry.Logger.Warn($"Attack {((AbstractModel)card).Id} has an unsupported target type: {card.TargetType}", 1);
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
			return;
		}
		val2.Simulate(context.Simulator);
	}

	public static void GeneralBlockOnPlay(CardModel card, CardOnPlayMirrorContext context)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Expected I4, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		CardOnPlayMirrorContext context2 = context;
		if (TryGetDynamicVar(card, new _003C_003Ez__ReadOnlyArray<string>(new string[2] { "CalculatedBlock", "Block" }), out DynamicVar dynamicVar))
		{
			decimal amount = context2.Calculate(dynamicVar);
			CalculatedBlockVar val = (CalculatedBlockVar)(object)((dynamicVar is CalculatedBlockVar) ? dynamicVar : null);
			ValueProp val3;
			if (val == null)
			{
				BlockVar val2 = (BlockVar)(object)((dynamicVar is BlockVar) ? dynamicVar : null);
				val3 = ((val2 == null) ? ((ValueProp)8) : val2.Props);
			}
			else
			{
				val3 = val.Props;
			}
			ValueProp props = val3;
			Action<Creature> action = delegate(Creature target)
			{
				//IL_0013: Unknown result type (might be due to invalid IL or missing references)
				context2.GainBlock(target, amount, props);
			};
			TargetType targetType = card.TargetType;
			switch (targetType - 1)
			{
			case 0:
			case 1:
			case 2:
			case 3:
				action(card.Owner.Creature);
				break;
			case 5:
				action(context2.Target);
				break;
			case 6:
			{
				foreach (Creature item in from creature in context2.CombatState.GetTeammatesOf(card.Owner.Creature)
					where creature.IsPlayer && context2.State.GetCreature(creature).IsAlive
					select creature)
				{
					action(item);
				}
				break;
			}
			default:
				Entry.Logger.Warn($"Block {((AbstractModel)card).Id} has an unsupported target type: {card.TargetType}", 1);
				context2.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
				break;
			}
		}
		else
		{
			Entry.Logger.Warn($"Card {((AbstractModel)card).Id} has no block var to simulate a block gain.", 1);
			context2.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static bool TryGetDynamicVar(CardModel card, IEnumerable<string> candidateKeys, [NotNullWhen(true)] out DynamicVar? dynamicVar)
	{
		foreach (string candidateKey in candidateKeys)
		{
			if (card.DynamicVars.TryGetValue(candidateKey, ref dynamicVar))
			{
				return true;
			}
		}
		dynamicVar = null;
		return false;
	}
}
