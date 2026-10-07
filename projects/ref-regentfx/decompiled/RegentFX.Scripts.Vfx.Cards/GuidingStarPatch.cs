using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace RegentFX.Scripts.Vfx.Cards;

[HarmonyPatch]
public static class GuidingStarPatch
{
	private static MethodBase TargetMethod()
	{
		MethodInfo methodInfo = AccessTools.DeclaredMethod(typeof(GuidingStar), "OnPlay", new Type[2]
		{
			typeof(PlayerChoiceContext),
			typeof(CardPlay)
		}, (Type[])null);
		if (!(methodInfo == null))
		{
			return AccessTools.AsyncMoveNext((MethodBase)methodInfo);
		}
		throw new MissingMethodException(typeof(GuidingStar).FullName, "OnPlay");
	}

	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> RemoveOriginalVfx(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
	{
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Expected O, but got Unknown
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Expected O, but got Unknown
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Expected O, but got Unknown
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Expected O, but got Unknown
		List<CodeInstruction> list = instructions.ToList();
		MethodInfo method = AccessTools.PropertyGetter(typeof(NCombatRoom), "Instance");
		MethodInfo method2 = AccessTools.Method(typeof(NCombatRoom), "GetCreatureNode", (Type[])null, (Type[])null);
		MethodInfo method3 = AccessTools.Method(typeof(NSmallMagicMissileVfx), "Create", (Type[])null, (Type[])null);
		MethodInfo method4 = AccessTools.Method(typeof(Cmd), "Wait", new Type[2]
		{
			typeof(float),
			typeof(bool)
		}, (Type[])null);
		MethodInfo method5 = AccessTools.PropertyGetter(typeof(CardModel), "DynamicVars");
		MethodInfo method6 = AccessTools.Method(typeof(DamageCmd), "Attack", new Type[1] { typeof(decimal) }, (Type[])null);
		int num = FindCall(list, method2);
		int num2 = FindLastCall(list, method, num);
		int num3 = FindCall(list, method3, num + 1);
		int num4 = FindCall(list, method4, num3 + 1);
		int num5 = FindCall(list, method5, num4 + 1);
		int num6 = FindCall(list, method6, num4 + 1);
		int num7 = num5 - 1;
		FieldInfo fieldInfo = __originalMethod.DeclaringType?.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SingleOrDefault((FieldInfo field) => field.FieldType == typeof(GuidingStar));
		MethodInfo methodInfo = AccessTools.Method(typeof(GuidingStarPatch), "ShouldSkipOriginalVfx", (Type[])null, (Type[])null);
		if (num2 < 0 || num < num2 || num3 < num || num4 < num3 || num5 < num4 || num6 < num5 || num7 < 0 || !LoadsLocal(list[num7]) || fieldInfo == null || methodInfo == null)
		{
			Entry.Logger.Warn("GuidingStar Transpiler未找到预期的原版特效IL，保留原逻辑", 1);
			return list;
		}
		Label label = generator.DefineLabel();
		list[num7].labels.Add(label);
		List<CodeInstruction> list2 = new List<CodeInstruction>
		{
			new CodeInstruction(OpCodes.Ldarg_0, (object)null),
			new CodeInstruction(OpCodes.Ldfld, (object)fieldInfo),
			new CodeInstruction(OpCodes.Call, (object)methodInfo),
			new CodeInstruction(OpCodes.Brtrue, (object)label)
		};
		CodeInstructionExtensions.MoveLabelsTo(list[num2], list2[0]);
		list.InsertRange(num2, list2);
		return list;
	}

	private static bool ShouldSkipOriginalVfx(GuidingStar card)
	{
		if (CardFX.IsTypeEnabled<GuidingStar>())
		{
			return LocalContext.IsMe(((CardModel)card).Owner);
		}
		return false;
	}

	private static int FindCall(IReadOnlyList<CodeInstruction> codes, MethodInfo? method, int startIndex = 0)
	{
		if (method == null || startIndex < 0)
		{
			return -1;
		}
		for (int i = startIndex; i < codes.Count; i++)
		{
			if (CodeInstructionExtensions.Calls(codes[i], method))
			{
				return i;
			}
		}
		return -1;
	}

	private static int FindLastCall(IReadOnlyList<CodeInstruction> codes, MethodInfo? method, int beforeIndex)
	{
		if (method == null || beforeIndex < 0)
		{
			return -1;
		}
		for (int num = beforeIndex - 1; num >= 0; num--)
		{
			if (CodeInstructionExtensions.Calls(codes[num], method))
			{
				return num;
			}
		}
		return -1;
	}

	private static bool LoadsLocal(CodeInstruction instruction)
	{
		if (!(instruction.opcode == OpCodes.Ldloc) && !(instruction.opcode == OpCodes.Ldloc_S) && !(instruction.opcode == OpCodes.Ldloc_0) && !(instruction.opcode == OpCodes.Ldloc_1) && !(instruction.opcode == OpCodes.Ldloc_2))
		{
			return instruction.opcode == OpCodes.Ldloc_3;
		}
		return true;
	}
}
