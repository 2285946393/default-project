using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Telemetry;
using STS2RitsuLib.Utils.HarmonyIl;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class CardOnPlayInferrer
{
	private enum EffectKind
	{
		Attack,
		Block,
		OwnerDraw,
		VulnerableApplication,
		WeakApplication
	}

	public static Action<CardModel, CardOnPlayMirrorContext>? Infer(Type runtimeType, MethodInfo overrideMethod)
	{
		if (!CompatibilityUtils.IsPredictionAllowedForType(runtimeType))
		{
			return null;
		}
		HarmonyIlMethodBody originalIl;
		try
		{
			originalIl = HarmonyIlInspectionExtensions.GetOriginalIl((MethodBase)overrideMethod, true);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Could not inspect original OnPlay IL for inferred card mirror {runtimeType.FullName}: {ex}", 1);
			ModTelemetry.CaptureException(ex, "card_on_play_inferrer", "read_original_il", TelemetryContext.ForModel(runtimeType));
			return null;
		}
		HashSet<EffectKind> hashSet = new HashSet<EffectKind>();
		List<Action<CardModel, CardOnPlayMirrorContext>> actions = new List<Action<CardModel, CardOnPlayMirrorContext>>();
		MethodInfo method = default(MethodInfo);
		for (int i = 0; i < originalIl.Instructions.Count; i++)
		{
			if (!HarmonyIl.TryGetCalledMethod(originalIl.Instructions[i], ref method))
			{
				continue;
			}
			Action<CardModel, CardOnPlayMirrorContext> action;
			if (IsAttackExecution(method))
			{
				if (hashSet.Add(EffectKind.Attack))
				{
					actions.Add(GeneralCardMirrors.GeneralAttackOnPlay);
				}
			}
			else if (IsBlockGain(method))
			{
				if (hashSet.Add(EffectKind.Block))
				{
					actions.Add(GeneralCardMirrors.GeneralBlockOnPlay);
				}
			}
			else if (TryInferOwnerDraw(originalIl.Instructions, i, method, out action))
			{
				if (hashSet.Add(EffectKind.OwnerDraw))
				{
					actions.Add(action);
				}
			}
			else if (IsPowerApplication<VulnerablePower>(originalIl.Instructions, i, method))
			{
				if (hashSet.Add(EffectKind.VulnerableApplication))
				{
					actions.Add(GeneralCardMirrors.GeneralPowerApplicationOnPlay<VulnerablePower>);
				}
			}
			else if (IsPowerApplication<WeakPower>(originalIl.Instructions, i, method) && hashSet.Add(EffectKind.WeakApplication))
			{
				actions.Add(GeneralCardMirrors.GeneralPowerApplicationOnPlay<WeakPower>);
			}
		}
		if (actions.Count == 0)
		{
			return null;
		}
		return delegate(CardModel card, CardOnPlayMirrorContext context)
		{
			try
			{
				foreach (Action<CardModel, CardOnPlayMirrorContext> item in actions)
				{
					item(card, context);
				}
			}
			catch (Exception ex2)
			{
				Entry.Logger.Warn($"Inferred card mirror failed for {((AbstractModel)card).Id}: {ex2}", 1);
				ModTelemetry.CaptureException(ex2, "card_on_play_inferrer", "execute_inferred_mirror", TelemetryContext.ForModel((AbstractModel)(object)card));
			}
		};
	}

	private static bool IsPowerApplication<T>(IReadOnlyList<CodeInstruction> instructions, int callIndex, MethodInfo method) where T : PowerModel
	{
		if (method.DeclaringType == typeof(PowerCmd) && method.Name == "Apply" && method.IsGenericMethod)
		{
			Type[] genericArguments = method.GetGenericArguments();
			if (genericArguments != null && genericArguments.Length == 1)
			{
				Type type = genericArguments[0];
				if (type == typeof(T))
				{
					return !IsConditionallyGuarded(instructions, callIndex);
				}
			}
		}
		return false;
	}

	private static bool TryInferOwnerDraw(IReadOnlyList<CodeInstruction> instructions, int callIndex, MethodInfo method, [NotNullWhen(true)] out Action<CardModel, CardOnPlayMirrorContext>? action)
	{
		action = null;
		if (method.DeclaringType != typeof(CardPileCmd) || method.Name != "Draw" || IsConditionallyGuarded(instructions, callIndex))
		{
			return false;
		}
		ParameterInfo[] parameters = method.GetParameters();
		if (parameters.Length == 2 && parameters[0].ParameterType == typeof(PlayerChoiceContext) && IsCardModelOwnerGetter(instructions, callIndex - 1))
		{
			action = GeneralCardMirrors.GeneralOwnerDrawOneOnPlay;
			return true;
		}
		if (parameters.Length != 4 || parameters[0].ParameterType != typeof(PlayerChoiceContext) || parameters[1].ParameterType != typeof(decimal) || !IsCardModelOwnerGetter(instructions, callIndex - 2) || !LoadsFalse(instructions, callIndex - 1))
		{
			return false;
		}
		if (LoadsDecimalOne(instructions, callIndex - 4))
		{
			action = GeneralCardMirrors.GeneralOwnerDrawOneOnPlay;
			return true;
		}
		if (LoadsCardsValue(instructions, callIndex - 4) || LoadsStoredCardsValue(instructions, callIndex - 4))
		{
			action = GeneralCardMirrors.GeneralOwnerDrawOnPlay;
			return true;
		}
		return false;
	}

	private static bool IsConditionallyGuarded(IReadOnlyList<CodeInstruction> instructions, int callIndex)
	{
		int num = -1;
		for (int num2 = callIndex - 1; num2 >= Math.Max(0, callIndex - 16); num2--)
		{
			if (instructions[num2].operand is FieldInfo fieldInfo)
			{
				Type fieldType = fieldInfo.FieldType;
				if (fieldType == typeof(PlayerChoiceContext))
				{
					num = num2;
					break;
				}
			}
		}
		int num3 = num - 2;
		if (num3 < 0 || instructions[num3].opcode.FlowControl != FlowControl.Cond_Branch)
		{
			return false;
		}
		if (num3 < 14)
		{
			int num4 = default(int);
			for (int i = Math.Max(0, num3 - 2); i < num3; i++)
			{
				if (HarmonyIl.TryGetLocalLoadIndex(instructions[i], ref num4) && num4 == 0)
				{
					return false;
				}
			}
		}
		return true;
	}

	private static bool IsCardModelOwnerGetter(IReadOnlyList<CodeInstruction> instructions, int index)
	{
		MethodInfo methodInfo = default(MethodInfo);
		if (index >= 0 && HarmonyIl.TryGetCalledMethod(instructions[index], ref methodInfo) && methodInfo.DeclaringType == typeof(CardModel))
		{
			return methodInfo.Name == "get_Owner";
		}
		return false;
	}

	private static bool LoadsFalse(IReadOnlyList<CodeInstruction> instructions, int index)
	{
		if (index >= 0)
		{
			return HarmonyIl.LoadsInt32(instructions[index], 0);
		}
		return false;
	}

	private static bool LoadsDecimalOne(IReadOnlyList<CodeInstruction> instructions, int index)
	{
		if (index >= 0 && instructions[index].opcode == OpCodes.Ldsfld && instructions[index].operand is FieldInfo fieldInfo)
		{
			Type declaringType = fieldInfo.DeclaringType;
			if (fieldInfo.Name == "One")
			{
				return declaringType == typeof(decimal);
			}
		}
		return false;
	}

	private static bool LoadsCardsValue(IReadOnlyList<CodeInstruction> instructions, int valueEndIndex)
	{
		if (IsDynamicVarGetter(instructions, valueEndIndex, "BaseValue"))
		{
			return IsCardsGetter(instructions, valueEndIndex - 1);
		}
		if (IsDecimalFromInt(instructions, valueEndIndex) && IsDynamicVarGetter(instructions, valueEndIndex - 1, "IntValue"))
		{
			return IsCardsGetter(instructions, valueEndIndex - 2);
		}
		return false;
	}

	private static bool LoadsStoredCardsValue(IReadOnlyList<CodeInstruction> instructions, int valueEndIndex)
	{
		if (!IsDecimalFromInt(instructions, valueEndIndex) || valueEndIndex < 1 || !(instructions[valueEndIndex - 1].operand is FieldInfo objB))
		{
			return false;
		}
		for (int num = valueEndIndex - 2; num >= Math.Max(2, valueEndIndex - 32); num--)
		{
			if (instructions[num].opcode == OpCodes.Stfld && object.Equals(instructions[num].operand, objB))
			{
				if (IsDynamicVarGetter(instructions, num - 1, "IntValue"))
				{
					return IsCardsGetter(instructions, num - 2);
				}
				return false;
			}
		}
		return false;
	}

	private static bool IsCardsGetter(IReadOnlyList<CodeInstruction> instructions, int index)
	{
		MethodInfo methodInfo = default(MethodInfo);
		if (index >= 0 && HarmonyIl.TryGetCalledMethod(instructions[index], ref methodInfo) && methodInfo.DeclaringType == typeof(DynamicVarSet))
		{
			return methodInfo.Name == "get_Cards";
		}
		return false;
	}

	private static bool IsDynamicVarGetter(IReadOnlyList<CodeInstruction> instructions, int index, string propertyName)
	{
		MethodInfo methodInfo = default(MethodInfo);
		if (index >= 0 && HarmonyIl.TryGetCalledMethod(instructions[index], ref methodInfo) && typeof(DynamicVar).IsAssignableFrom(methodInfo.DeclaringType))
		{
			return methodInfo.Name == "get_" + propertyName;
		}
		return false;
	}

	private static bool IsDecimalFromInt(IReadOnlyList<CodeInstruction> instructions, int index)
	{
		MethodInfo methodInfo = default(MethodInfo);
		if (index >= 0 && HarmonyIl.TryGetCalledMethod(instructions[index], ref methodInfo) && methodInfo.DeclaringType == typeof(decimal) && methodInfo.Name == "op_Implicit")
		{
			ParameterInfo[] parameters = methodInfo.GetParameters();
			if (parameters != null && parameters.Length == 1)
			{
				ParameterInfo parameterInfo = parameters[0];
				return parameterInfo.ParameterType == typeof(int);
			}
		}
		return false;
	}

	private static bool IsAttackExecution(MethodInfo method)
	{
		if (method.DeclaringType == typeof(AttackCommand))
		{
			return method.Name == "Execute";
		}
		return false;
	}

	private static bool IsBlockGain(MethodInfo method)
	{
		if (method.DeclaringType == typeof(CreatureCmd))
		{
			return method.Name == "GainBlock";
		}
		return false;
	}
}
