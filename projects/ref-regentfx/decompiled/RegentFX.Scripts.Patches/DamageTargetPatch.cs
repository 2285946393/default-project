using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RegentFX.Scripts.Vfx;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class DamageTargetPatch
{
	private static int _depth;

	private static readonly MethodInfo? Damage108 = AccessTools.Method(typeof(CreatureCmd), "Damage", new Type[7]
	{
		typeof(PlayerChoiceContext),
		typeof(IEnumerable<Creature>),
		typeof(decimal),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel),
		typeof(CardPlay)
	}, (Type[])null);

	private static readonly MethodInfo? Damage107 = AccessTools.Method(typeof(CreatureCmd), "Damage", new Type[6]
	{
		typeof(PlayerChoiceContext),
		typeof(IEnumerable<Creature>),
		typeof(decimal),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel)
	}, (Type[])null);

	public static MethodBase TargetMethod()
	{
		return Damage108 ?? Damage107 ?? throw new MissingMethodException(typeof(CreatureCmd).FullName, "Damage");
	}

	[HarmonyPrefix]
	public static bool DamagePrefix(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, object?[] __args, ref Task<IEnumerable<DamageResult>> __result)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		if (cardSource == null)
		{
			return true;
		}
		CardFX cardFX = CardFX.FromCard(cardSource);
		if (cardFX == null)
		{
			return true;
		}
		if (!LocalContext.IsMe(cardSource.Owner))
		{
			return true;
		}
		if (!cardFX.HasOnBeforeDamage && !cardFX.HasOnBeforeDamageTargeted)
		{
			return true;
		}
		if (Interlocked.Increment(ref _depth) > 1)
		{
			Interlocked.Decrement(ref _depth);
			return true;
		}
		AttackCommand value = AttackVfxContext.CurrentAttackCommand.Value;
		CardPlay cardPlay = (CardPlay)((__args.Length > 6) ? /*isinst with value type is only supported in some contexts*/: null);
		__result = RunBeforeAndDamage(cardFX, value, targets, choiceContext, amount, props, dealer, cardSource, cardPlay);
		return false;
	}

	private static async Task<IEnumerable<DamageResult>> RunBeforeAndDamage(CardFX cardFX, AttackCommand? command, IEnumerable<Creature> targets, PlayerChoiceContext choiceContext, decimal amount, ValueProp props, Creature? dealer, CardModel cardSource, CardPlay? cardPlay)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		_ = 2;
		try
		{
			List<Creature> targetList = targets.ToList();
			if (cardFX.HasOnBeforeDamageTargeted && command != null)
			{
				await cardFX.OnBeforeDamage(command, targetList);
			}
			else if (cardFX.HasOnBeforeDamage && command != null)
			{
				await cardFX.OnBeforeDamage(command);
			}
			return await InvokeDamage(choiceContext, targetList, amount, props, dealer, cardSource, cardPlay);
		}
		finally
		{
			Interlocked.Decrement(ref _depth);
		}
	}

	private static Task<IEnumerable<DamageResult>> InvokeDamage(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, decimal amount, ValueProp props, Creature? dealer, CardModel cardSource, CardPlay? cardPlay)
	{
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		MethodInfo methodInfo = Damage108 ?? Damage107 ?? throw new MissingMethodException(typeof(CreatureCmd).FullName, "Damage");
		object[] parameters = ((methodInfo.GetParameters().Length != 7) ? new object[6] { choiceContext, targets, amount, props, dealer, cardSource } : new object[7] { choiceContext, targets, amount, props, dealer, cardSource, cardPlay });
		return (methodInfo.Invoke(null, parameters) as Task<IEnumerable<DamageResult>>) ?? throw new InvalidOperationException("CreatureCmd.Damage returned an unexpected result type.");
	}
}
