using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class CardOnPlayMirrorContextExtensions
{
	public static void AttackSingle(this CardOnPlayMirrorContext context, int hitCount = 1)
	{
		DamageCmd.Attack(((DynamicVar)context.PreviewCard.DynamicVars.Damage).BaseValue).FromCard(context.PreviewCard, context.CardPlay).WithHitCount(hitCount)
			.Targeting(context.Target)
			.Simulate(context.Simulator);
	}

	public static void AttackAllOpponents(this CardOnPlayMirrorContext context, int hitCount = 1)
	{
		DamageCmd.Attack(((DynamicVar)context.PreviewCard.DynamicVars.Damage).BaseValue).FromCard(context.PreviewCard, context.CardPlay).WithHitCount(hitCount)
			.TargetingAllOpponents(context.CombatState)
			.Simulate(context.Simulator);
	}

	public static void AttackRandomOpponents(this CardOnPlayMirrorContext context, int hitCount = 1)
	{
		DamageCmd.Attack(((DynamicVar)context.PreviewCard.DynamicVars.Damage).BaseValue).FromCard(context.PreviewCard, context.CardPlay).WithHitCount(hitCount)
			.TargetingRandomOpponents(context.CombatState, true)
			.Simulate(context.Simulator);
	}

	public static decimal GainBlock(this CardOnPlayMirrorContext context, Creature target)
	{
		return context.Simulator.GainBlock(target, context.PreviewCard.DynamicVars.Block, context.Card, context.CardPlay);
	}

	public static decimal GainBlock(this CardOnPlayMirrorContext context, Creature target, decimal amount, ValueProp props)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return context.Simulator.GainBlock(target, amount, props, context.Card, context.CardPlay);
	}

	public static PowerModel? ApplyPower<T>(this CardOnPlayMirrorContext context, Creature target) where T : PowerModel
	{
		decimal baseValue = context.PreviewCard.DynamicVars[typeof(T).Name].BaseValue;
		return (PowerModel?)(object)context.Simulator.ApplyPower<T>(target, baseValue, context.PreviewCard.Owner.Creature, context.Card);
	}

	public static PowerModel? ApplyPower<T>(this CardOnPlayMirrorContext context, Creature target, decimal amount) where T : PowerModel
	{
		return (PowerModel?)(object)context.Simulator.ApplyPower<T>(target, amount, context.PreviewCard.Owner.Creature, context.Card);
	}

	public static IReadOnlyList<PowerModel> ApplyPower<T>(this CardOnPlayMirrorContext context, IReadOnlyList<Creature> targets) where T : PowerModel
	{
		decimal baseValue = context.PreviewCard.DynamicVars[typeof(T).Name].BaseValue;
		return (IReadOnlyList<PowerModel>)context.Simulator.ApplyPower<T>(targets, baseValue, context.PreviewCard.Owner.Creature, context.Card);
	}

	public static IReadOnlyList<PowerModel> ApplyPower<T>(this CardOnPlayMirrorContext context, IReadOnlyList<Creature> targets, decimal amount) where T : PowerModel
	{
		return (IReadOnlyList<PowerModel>)context.Simulator.ApplyPower<T>(targets, amount, context.PreviewCard.Owner.Creature, context.Card);
	}

	public static decimal Calculate(this CardOnPlayMirrorContext context, DynamicVar dynamicVar)
	{
		return dynamicVar.InvokeCalculate(context.Simulator, context.Card, context.CardPlay.Target);
	}

	public static int ResolveEnergyXValue(this CardOnPlayMirrorContext context)
	{
		return HookMirrors.ModifyXValue(context.Simulator, context.PreviewCard, context.PreviewCard.EnergyCost.CapturedXValue);
	}

	public static int ResolveStarXValue(this CardOnPlayMirrorContext context)
	{
		return HookMirrors.ModifyXValue(context.Simulator, context.PreviewCard, context.PreviewCard.LastStarsSpent);
	}
}
