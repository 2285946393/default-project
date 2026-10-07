using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RegentFX.Scripts.Vfx.Cards;

[HarmonyPatch]
public static class CometPatch
{
	private static readonly MethodInfo? FromCard108 = AccessTools.Method(typeof(AttackCommand), "FromCard", new Type[2]
	{
		typeof(CardModel),
		typeof(CardPlay)
	}, (Type[])null);

	private static readonly MethodInfo? FromCard107 = AccessTools.Method(typeof(AttackCommand), "FromCard", new Type[1] { typeof(CardModel) }, (Type[])null);

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Comet), "OnPlay")]
	public static bool OnPlay(Comet __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
	{
		if (!CardFX.IsTypeEnabled<Comet>())
		{
			return true;
		}
		if (!LocalContext.IsMe(((CardModel)__instance).Owner))
		{
			return true;
		}
		__result = MyOnPlay(__instance, choiceContext, cardPlay);
		return false;
	}

	private static async Task MyOnPlay(Comet card, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await CreatureCmd.TriggerAnim(((CardModel)card).Owner.Creature, "Cast", ((CardModel)card).Owner.Character.CastAnimDelay);
		await FromCardCompat(DamageCmd.Attack(((DynamicVar)((CardModel)card).DynamicVars.Damage).BaseValue), (CardModel)(object)card, cardPlay).Targeting(cardPlay.Target).WithNoAttackerAnim().Execute(choiceContext);
		await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, ((DynamicVar)((CardModel)card).DynamicVars.Weak).BaseValue, ((CardModel)card).Owner.Creature, (CardModel)(object)card, false);
		await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, ((DynamicVar)((CardModel)card).DynamicVars.Vulnerable).BaseValue, ((CardModel)card).Owner.Creature, (CardModel)(object)card, false);
	}

	private static AttackCommand FromCardCompat(AttackCommand command, CardModel card, CardPlay? cardPlay)
	{
		MethodInfo methodInfo = FromCard108 ?? FromCard107 ?? throw new MissingMethodException(typeof(AttackCommand).FullName, "FromCard");
		object[] parameters = ((methodInfo.GetParameters().Length != 2) ? new object[1] { card } : new object[2] { card, cardPlay });
		object? obj = methodInfo.Invoke(command, parameters);
		return (AttackCommand)(((obj is AttackCommand) ? obj : null) ?? throw new InvalidOperationException("AttackCommand.FromCard returned an unexpected result type."));
	}
}
