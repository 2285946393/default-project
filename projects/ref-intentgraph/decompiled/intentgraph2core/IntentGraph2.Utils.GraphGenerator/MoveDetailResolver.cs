using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using IntentGraph2.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntentGraph2.Utils.GraphGenerator;

internal static class MoveDetailResolver
{
	private sealed record RawMoveDetail(MoveDetailIconType Type, Type ModelType, int? Amount);

	private sealed record IlInstruction(int Offset, OpCode OpCode, object? Operand);

	private const string PowerCmdTypeName = "MegaCrit.Sts2.Core.Commands.PowerCmd";

	private const string CardPileCmdTypeName = "MegaCrit.Sts2.Core.Commands.CardPileCmd";

	private const string ModelDbTypeName = "MegaCrit.Sts2.Core.Models.ModelDb";

	private static readonly FieldInfo? OnPerformField;

	private static readonly ConditionalWeakTable<MoveState, List<RawMoveDetail>> RawMoveDetailCache;

	private static readonly OpCode[] SingleByteOpCodes;

	private static readonly OpCode[] MultiByteOpCodes;

	static MoveDetailResolver()
	{
		OnPerformField = typeof(MoveState).GetField("_onPerform", BindingFlags.Instance | BindingFlags.NonPublic);
		RawMoveDetailCache = new ConditionalWeakTable<MoveState, List<RawMoveDetail>>();
		SingleByteOpCodes = new OpCode[256];
		MultiByteOpCodes = new OpCode[256];
		FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
		for (int i = 0; i < fields.Length; i++)
		{
			if (fields[i].GetValue(null) is OpCode opCode)
			{
				ushort num = (ushort)opCode.Value;
				if (num < 256)
				{
					SingleByteOpCodes[num] = opCode;
				}
				else if ((num & 0xFF00) == 65024)
				{
					MultiByteOpCodes[num & 0xFF] = opCode;
				}
			}
		}
	}

	public static List<ResolvedIntent> ResolveIntentIcons(MoveState moveState, IntentOverride?[]? intentOverrides)
	{
		IReadOnlyList<AbstractIntent> intents = moveState.Intents;
		List<ResolvedIntent> list = intents.Select((AbstractIntent intent, int index) => new ResolvedIntent(intent, index)).ToList();
		if (!IntentGraphMod.Config.ShowMoveDetail || intents.Count == 0)
		{
			return list;
		}
		try
		{
			if (intents.All((AbstractIntent i) => !IsReplaceableIntent(i.IntentType)))
			{
				return GenerateResolvedIntents(intents, list, intentOverrides, null);
			}
			List<RawMoveDetail> value = RawMoveDetailCache.GetValue(moveState, ScanMove);
			if (value.Count == 0)
			{
				return GenerateResolvedIntents(intents, list, intentOverrides, null);
			}
			Dictionary<int, List<RawMoveDetail>> dictionary = new Dictionary<int, List<RawMoveDetail>>();
			List<int> intentIndices = (from i in Enumerable.Range(0, intents.Count)
				where IsPowerIntent(intents[i].IntentType)
				select i).ToList();
			AssignPowers(value.Where((RawMoveDetail content) => content.Type == MoveDetailIconType.Power).ToList(), intentIndices, intents, dictionary);
			AssignByPosition(value.Where((RawMoveDetail content) => content.Type == MoveDetailIconType.Card).ToList(), (from i in Enumerable.Range(0, intents.Count)
				where (int)intents[i].IntentType == 11
				select i).ToList(), dictionary);
			return GenerateResolvedIntents(intents, list, intentOverrides, dictionary);
		}
		catch (Exception value2)
		{
			IgLogger.Warn($"Unable to resolve action contents for move '{((MonsterState)moveState).Id}': {value2}");
			return list;
		}
	}

	private static List<ResolvedIntent> GenerateResolvedIntents(IReadOnlyList<AbstractIntent> intents, List<ResolvedIntent> originalIcons, IntentOverride?[]? intentOverrides, Dictionary<int, List<RawMoveDetail>>? contentByIntent)
	{
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Expected O, but got Unknown
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		IReadOnlyList<AbstractIntent> intents2 = intents;
		List<ResolvedIntent> list = new List<ResolvedIntent>();
		int i;
		for (i = 0; i < intents2.Count; i++)
		{
			IntentOverride intentOverride = ((i < intentOverrides?.Length) ? intentOverrides[i] : null);
			List<RawMoveDetail> value;
			if (intentOverride != null && intentOverride.Details != null)
			{
				if (intentOverride.Details.Length != 0 && intentOverride.Details.All((MoveDetailOverride d) => d.Type == MoveDetailIconType.None))
				{
					list.Add(originalIcons[i]);
					continue;
				}
				MoveDetailOverride[] details = intentOverride.Details;
				foreach (MoveDetailOverride moveDetailOverride in details)
				{
					if (moveDetailOverride.Id != null)
					{
						ModelId val = (ModelId)(moveDetailOverride.Type switch
						{
							MoveDetailIconType.Card => (object)new ModelId("CARD", moveDetailOverride.Id), 
							MoveDetailIconType.Power => (object)new ModelId("POWER", moveDetailOverride.Id), 
							_ => null, 
						});
						if (val != (ModelId)null && ModelDb.GetByIdOrNull<AbstractModel>(val) != null)
						{
							list.Add(new ResolvedIntent(intents2[i], i, val, moveDetailOverride.Value, moveDetailOverride.ValueText));
						}
					}
				}
			}
			else if (contentByIntent == null || !IsReplaceableIntent(intents2[i].IntentType) || !contentByIntent.TryGetValue(i, out value) || value.Count == 0)
			{
				list.Add(originalIcons[i]);
			}
			else
			{
				list.AddRange(value.Select((RawMoveDetail content) => CreateResolvedIcon(intents2[i], i, content)));
			}
		}
		return list;
	}

	private static bool IsReplaceableIntent(IntentType intentType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		if (!IsPowerIntent(intentType))
		{
			return (int)intentType == 11;
		}
		return true;
	}

	private static bool IsPowerIntent(IntentType intentType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Invalid comparison between Unknown and I4
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		if (intentType - 1 <= 2 || (int)intentType == 12)
		{
			return true;
		}
		return false;
	}

	private static void AssignPowers(List<RawMoveDetail> powers, List<int> intentIndices, IReadOnlyList<AbstractIntent> intents, Dictionary<int, List<RawMoveDetail>> destination)
	{
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		IReadOnlyList<AbstractIntent> intents2 = intents;
		if (powers.Count == 0 || intentIndices.Count == 0)
		{
			return;
		}
		if (intentIndices.Count == 1)
		{
			AddRange(destination, intentIndices[0], powers);
			return;
		}
		if (powers.Count == intentIndices.Count)
		{
			for (int i = 0; i < powers.Count; i++)
			{
				Add(destination, intentIndices[i], powers[i]);
			}
			return;
		}
		Dictionary<IntentType, int> dictionary = new Dictionary<IntentType, int>();
		foreach (RawMoveDetail power in powers)
		{
			HashSet<IntentType> expectedIntentTypes = GetCompatibleIntentTypes(power);
			List<int> list = intentIndices.Where((int index) => expectedIntentTypes.Contains(intents2[index].IntentType)).ToList();
			if (list.Count == 0)
			{
				list = intentIndices.Where((int index) => (int)intents2[index].IntentType == 12).ToList();
			}
			if (list.Count == 0)
			{
				list = intentIndices;
			}
			IntentType intentType = intents2[list[0]].IntentType;
			int valueOrDefault = dictionary.GetValueOrDefault(intentType);
			int intentIndex = list[valueOrDefault % list.Count];
			dictionary[intentType] = valueOrDefault + 1;
			Add(destination, intentIndex, power);
		}
	}

	private static HashSet<IntentType> GetCompatibleIntentTypes(RawMoveDetail power)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Invalid comparison between Unknown and I4
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		PowerModel? powerModel = GetPowerModel(power.ModelType);
		PowerType val = (PowerType)((powerModel != null) ? ((int)powerModel.Type) : 0);
		if (power.Amount < 0)
		{
			PowerType val2 = (((int)val == 1) ? ((PowerType)2) : (((int)val != 2) ? val : ((PowerType)1)));
			val = val2;
		}
		if ((int)val != 1)
		{
			if ((int)val == 2)
			{
				return new HashSet<IntentType>
				{
					(IntentType)2,
					(IntentType)3
				};
			}
			return new HashSet<IntentType>();
		}
		return new HashSet<IntentType> { (IntentType)1 };
	}

	private static void AssignByPosition(List<RawMoveDetail> contents, List<int> intentIndices, Dictionary<int, List<RawMoveDetail>> destination)
	{
		if (contents.Count == 0 || intentIndices.Count == 0)
		{
			return;
		}
		if (intentIndices.Count == 1)
		{
			AddRange(destination, intentIndices[0], contents);
		}
		else if (contents.Count == intentIndices.Count)
		{
			for (int i = 0; i < contents.Count; i++)
			{
				Add(destination, intentIndices[i], contents[i]);
			}
		}
		else
		{
			for (int j = 0; j < contents.Count; j++)
			{
				Add(destination, intentIndices[Math.Min(j, intentIndices.Count - 1)], contents[j]);
			}
		}
	}

	private static void AddRange(Dictionary<int, List<RawMoveDetail>> destination, int intentIndex, IEnumerable<RawMoveDetail> contents)
	{
		foreach (RawMoveDetail content in contents)
		{
			Add(destination, intentIndex, content);
		}
	}

	private static void Add(Dictionary<int, List<RawMoveDetail>> destination, int intentIndex, RawMoveDetail content)
	{
		if (!destination.TryGetValue(intentIndex, out List<RawMoveDetail> value))
		{
			value = (destination[intentIndex] = new List<RawMoveDetail>());
		}
		value.Add(content);
	}

	private static ResolvedIntent CreateResolvedIcon(AbstractIntent intent, int intentIndex, RawMoveDetail content)
	{
		return new ResolvedIntent(intent, intentIndex, ModelDb.GetId(content.ModelType), content.Amount);
	}

	private static PowerModel? GetPowerModel(Type powerType)
	{
		return ModelDb.GetByIdOrNull<PowerModel>(ModelDb.GetId(powerType));
	}

	private static List<RawMoveDetail> ScanMove(MoveState moveState)
	{
		List<RawMoveDetail> list = new List<RawMoveDetail>();
		try
		{
			if (!(OnPerformField?.GetValue(moveState) is Delegate @delegate))
			{
				return list;
			}
			ScanMethod(@delegate.Method, @delegate.Target, list, new HashSet<MethodInfo>());
		}
		catch (Exception ex)
		{
			IgLogger.Debug("Unable to resolve action contents for move '" + ((MonsterState)moveState).Id + "': " + ex.Message);
		}
		return list;
	}

	private static void ScanMethod(MethodInfo method, object? evaluationTarget, List<RawMoveDetail> destination, HashSet<MethodInfo> visited)
	{
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Invalid comparison between Unknown and I4
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Invalid comparison between Unknown and I4
		if (!visited.Add(method))
		{
			return;
		}
		Type type = method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
		if (type != null)
		{
			MethodInfo method2 = type.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method2 != null && method2 != method)
			{
				ScanMethod(method2, evaluationTarget, destination, visited);
				return;
			}
		}
		List<IlInstruction> list = ReadInstructions(method);
		List<(Type, bool)> list2 = new List<(Type, bool)>();
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (!(list[i].Operand is MethodInfo methodInfo))
			{
				continue;
			}
			string text = methodInfo.DeclaringType?.FullName;
			if (text == "MegaCrit.Sts2.Core.Models.ModelDb" && methodInfo.Name == "Power" && methodInfo.IsGenericMethod)
			{
				Type[] genericArguments = methodInfo.GetGenericArguments();
				if (genericArguments.Length != 0)
				{
					list2.Add((genericArguments[0], false));
				}
			}
			else if (text == "MegaCrit.Sts2.Core.Commands.PowerCmd" && methodInfo.Name == "Apply")
			{
				if (methodInfo.IsGenericMethod)
				{
					Type[] genericArguments2 = methodInfo.GetGenericArguments();
					if (genericArguments2.Length != 0)
					{
						PowerModel powerModel = GetPowerModel(genericArguments2[0]);
						destination.Add(new RawMoveDetail(MoveDetailIconType.Power, genericArguments2[0], (powerModel != null && (int)powerModel.StackType == 1) ? ScanDecimalAmountBefore(list, i, evaluationTarget) : null));
					}
					continue;
				}
				flag = true;
				for (int num = list2.Count - 1; num >= 0; num--)
				{
					(Type, bool) tuple = list2[num];
					if (!tuple.Item2)
					{
						PowerModel powerModel2 = GetPowerModel(tuple.Item1);
						destination.Add(new RawMoveDetail(MoveDetailIconType.Power, tuple.Item1, (powerModel2 != null && (int)powerModel2.StackType == 1) ? ScanDecimalAmountBefore(list, i, evaluationTarget) : null));
						list2[num] = (tuple.Item1, true);
						break;
					}
				}
			}
			else if (text == "MegaCrit.Sts2.Core.Commands.CardPileCmd" && methodInfo.Name == "AddToCombatAndPreview" && methodInfo.IsGenericMethod)
			{
				Type[] genericArguments3 = methodInfo.GetGenericArguments();
				if (genericArguments3.Length != 0)
				{
					destination.Add(new RawMoveDetail(MoveDetailIconType.Card, genericArguments3[0], null));
				}
			}
		}
		if (!flag)
		{
			return;
		}
		foreach (var item in list2.Where<(Type, bool)>(((Type type, bool used) candidate) => !candidate.used))
		{
			destination.Add(new RawMoveDetail(MoveDetailIconType.Power, item.Item1, null));
		}
	}

	private static int? ScanDecimalAmountBefore(IReadOnlyList<IlInstruction> instructions, int callIndex, object? evaluationTarget)
	{
		int num = Math.Max(0, callIndex - 40);
		for (int num2 = callIndex - 1; num2 >= num; num2--)
		{
			if (instructions[num2].Operand is FieldInfo fieldInfo && fieldInfo.DeclaringType == typeof(decimal) && fieldInfo.IsStatic)
			{
				try
				{
					if (TryConvertToInt(fieldInfo.GetValue(null), out var value))
					{
						return value;
					}
				}
				catch
				{
				}
			}
			if (instructions[num2].Operand is MethodBase methodBase && !(methodBase.DeclaringType != typeof(decimal)) && methodBase.GetParameters().Length == 1 && !(methodBase.GetParameters()[0].ParameterType != typeof(int)) && (!(instructions[num2].OpCode != OpCodes.Newobj) || !(methodBase.Name != "op_Implicit")))
			{
				int index = num2 - 1;
				if (TryEvaluateIntExpression(instructions, ref index, evaluationTarget, out var value2))
				{
					return value2;
				}
			}
		}
		return null;
	}

	private static bool TryEvaluateIntExpression(IReadOnlyList<IlInstruction> instructions, ref int index, object? evaluationTarget, out int value)
	{
		while (index >= 0 && IsTransparentNumericInstruction(instructions[index].OpCode))
		{
			index--;
		}
		if (index < 0)
		{
			value = 0;
			return false;
		}
		IlInstruction ilInstruction = instructions[index];
		if (TryGetIntConstant(ilInstruction, out value))
		{
			index--;
			return true;
		}
		if (ilInstruction.OpCode == OpCodes.Neg)
		{
			index--;
			if (!TryEvaluateIntExpression(instructions, ref index, evaluationTarget, out var value2))
			{
				value = 0;
				return false;
			}
			try
			{
				value = checked(-value2);
				return true;
			}
			catch (OverflowException)
			{
				value = 0;
				return false;
			}
		}
		if (IsSupportedBinaryNumericInstruction(ilInstruction.OpCode))
		{
			index--;
			if (!TryEvaluateIntExpression(instructions, ref index, evaluationTarget, out var value3) || !TryEvaluateIntExpression(instructions, ref index, evaluationTarget, out var value4))
			{
				value = 0;
				return false;
			}
			return TryEvaluateBinary(ilInstruction.OpCode, value4, value3, out value);
		}
		object obj;
		if (ilInstruction.Operand is MethodInfo methodInfo)
		{
			OpCode opCode = ilInstruction.OpCode;
			if ((opCode == OpCodes.Call || opCode == OpCodes.Callvirt) && methodInfo.IsSpecialName && methodInfo.Name.StartsWith("get_", StringComparison.Ordinal) && methodInfo.GetParameters().Length == 0)
			{
				index--;
				obj = null;
				if (!methodInfo.IsStatic)
				{
					if (evaluationTarget != null)
					{
						Type? declaringType = methodInfo.DeclaringType;
						if ((object)declaringType != null && declaringType.IsInstanceOfType(evaluationTarget))
						{
							obj = evaluationTarget;
							SkipEvaluationTargetReceiver(instructions, ref index, evaluationTarget);
							goto IL_015e;
						}
					}
					value = 0;
					return false;
				}
				goto IL_015e;
			}
		}
		object obj2;
		if (ilInstruction.Operand is FieldInfo fieldInfo)
		{
			index--;
			obj2 = null;
			if (!fieldInfo.IsStatic)
			{
				if (evaluationTarget != null)
				{
					Type? declaringType2 = fieldInfo.DeclaringType;
					if ((object)declaringType2 != null && declaringType2.IsInstanceOfType(evaluationTarget))
					{
						obj2 = evaluationTarget;
						SkipEvaluationTargetReceiver(instructions, ref index, evaluationTarget);
						goto IL_01c6;
					}
				}
				value = 0;
				return false;
			}
			goto IL_01c6;
		}
		value = 0;
		return false;
		IL_01c6:
		try
		{
			return TryConvertToInt(fieldInfo.GetValue(obj2), out value);
		}
		catch
		{
			value = 0;
			return false;
		}
		IL_015e:
		try
		{
			return TryConvertToInt(methodInfo.Invoke(obj, null), out value);
		}
		catch
		{
			value = 0;
			return false;
		}
	}

	private static void SkipEvaluationTargetReceiver(IReadOnlyList<IlInstruction> instructions, ref int index, object evaluationTarget)
	{
		if (index >= 0 && instructions[index].OpCode == OpCodes.Ldfld && instructions[index].Operand is FieldInfo fieldInfo && fieldInfo.FieldType.IsInstanceOfType(evaluationTarget))
		{
			index--;
		}
		if (index >= 0 && instructions[index].OpCode == OpCodes.Ldarg_0)
		{
			index--;
		}
	}

	private static bool IsTransparentNumericInstruction(OpCode opCode)
	{
		if (!(opCode == OpCodes.Nop) && !(opCode == OpCodes.Conv_I) && !(opCode == OpCodes.Conv_I1) && !(opCode == OpCodes.Conv_I2) && !(opCode == OpCodes.Conv_I4) && !(opCode == OpCodes.Conv_I8) && !(opCode == OpCodes.Conv_U) && !(opCode == OpCodes.Conv_U1) && !(opCode == OpCodes.Conv_U2) && !(opCode == OpCodes.Conv_U4))
		{
			return opCode == OpCodes.Conv_U8;
		}
		return true;
	}

	private static bool IsSupportedBinaryNumericInstruction(OpCode opCode)
	{
		if (!(opCode == OpCodes.Add) && !(opCode == OpCodes.Add_Ovf) && !(opCode == OpCodes.Add_Ovf_Un) && !(opCode == OpCodes.Sub) && !(opCode == OpCodes.Sub_Ovf) && !(opCode == OpCodes.Sub_Ovf_Un) && !(opCode == OpCodes.Mul) && !(opCode == OpCodes.Mul_Ovf) && !(opCode == OpCodes.Mul_Ovf_Un) && !(opCode == OpCodes.Div) && !(opCode == OpCodes.Div_Un) && !(opCode == OpCodes.Rem) && !(opCode == OpCodes.Rem_Un) && !(opCode == OpCodes.And) && !(opCode == OpCodes.Or) && !(opCode == OpCodes.Xor) && !(opCode == OpCodes.Shl) && !(opCode == OpCodes.Shr))
		{
			return opCode == OpCodes.Shr_Un;
		}
		return true;
	}

	private static bool TryEvaluateBinary(OpCode opCode, int left, int right, out int value)
	{
		checked
		{
			try
			{
				short value2 = opCode.Value;
				int num;
				if (value2 == OpCodes.Add.Value || value2 == OpCodes.Add_Ovf.Value || value2 == OpCodes.Add_Ovf_Un.Value)
				{
					num = left + right;
				}
				else
				{
					short num2 = value2;
					if (num2 == OpCodes.Sub.Value || num2 == OpCodes.Sub_Ovf.Value || num2 == OpCodes.Sub_Ovf_Un.Value)
					{
						num = left - right;
					}
					else
					{
						short num3 = value2;
						if (num3 == OpCodes.Mul.Value || num3 == OpCodes.Mul_Ovf.Value || num3 == OpCodes.Mul_Ovf_Un.Value)
						{
							num = left * right;
						}
						else
						{
							short num4 = value2;
							unchecked
							{
								if (num4 == OpCodes.Div.Value || num4 == OpCodes.Div_Un.Value)
								{
									num = left / right;
								}
								else
								{
									short num5 = value2;
									if (num5 == OpCodes.Rem.Value || num5 == OpCodes.Rem_Un.Value)
									{
										num = left % right;
									}
									else if (value2 == OpCodes.And.Value)
									{
										num = left & right;
									}
									else if (value2 == OpCodes.Or.Value)
									{
										num = left | right;
									}
									else if (value2 == OpCodes.Xor.Value)
									{
										num = left ^ right;
									}
									else if (value2 == OpCodes.Shl.Value)
									{
										num = left << right;
									}
									else
									{
										short num6 = value2;
										if (num6 != OpCodes.Shr.Value && num6 != OpCodes.Shr_Un.Value)
										{
											throw new InvalidOperationException();
										}
										num = left >> right;
									}
								}
							}
						}
					}
				}
				value = num;
				return true;
			}
			catch (Exception ex) when (((ex is OverflowException || ex is DivideByZeroException || ex is InvalidOperationException) ? 1 : 0) != 0)
			{
				value = 0;
				return false;
			}
		}
	}

	private static bool TryConvertToInt(object? input, out int value)
	{
		if (!(input is int num))
		{
			if (!(input is uint num2))
			{
				if (!(input is long num3))
				{
					if (!(input is decimal num4))
					{
						if (input is Enum value2)
						{
							value = Convert.ToInt32(value2);
							return true;
						}
					}
					else if (num4 == decimal.Truncate(num4) && num4 >= -2147483648m && num4 <= 2147483647m)
					{
						value = (int)num4;
						return true;
					}
				}
				else if (num3 >= int.MinValue && num3 <= int.MaxValue)
				{
					value = (int)num3;
					return true;
				}
			}
			else if (num2 <= int.MaxValue)
			{
				value = (int)num2;
				return true;
			}
			value = 0;
			return false;
		}
		value = num;
		return true;
	}

	private static int? ScanIntBefore(IReadOnlyList<IlInstruction> instructions, int callIndex)
	{
		int num = Math.Max(0, callIndex - 20);
		for (int num2 = callIndex - 1; num2 >= num; num2--)
		{
			if (TryGetIntConstant(instructions[num2], out var value))
			{
				return value;
			}
		}
		return null;
	}

	private static bool TryGetIntConstant(IlInstruction instruction, out int value)
	{
		if (instruction.OpCode == OpCodes.Ldc_I4_M1)
		{
			value = -1;
			return true;
		}
		if (instruction.OpCode == OpCodes.Ldc_I4_0)
		{
			value = 0;
			return true;
		}
		if (instruction.OpCode == OpCodes.Ldc_I4_1)
		{
			value = 1;
			return true;
		}
		if (instruction.OpCode == OpCodes.Ldc_I4_2)
		{
			value = 2;
			return true;
		}
		if (instruction.OpCode == OpCodes.Ldc_I4_3)
		{
			value = 3;
			return true;
		}
		if (instruction.OpCode == OpCodes.Ldc_I4_4)
		{
			value = 4;
			return true;
		}
		if (instruction.OpCode == OpCodes.Ldc_I4_5)
		{
			value = 5;
			return true;
		}
		if (instruction.OpCode == OpCodes.Ldc_I4_6)
		{
			value = 6;
			return true;
		}
		if (instruction.OpCode == OpCodes.Ldc_I4_7)
		{
			value = 7;
			return true;
		}
		if (instruction.OpCode == OpCodes.Ldc_I4_8)
		{
			value = 8;
			return true;
		}
		OpCode opCode = instruction.OpCode;
		if ((opCode == OpCodes.Ldc_I4 || opCode == OpCodes.Ldc_I4_S) && instruction.Operand is int num)
		{
			value = num;
			return true;
		}
		value = 0;
		return false;
	}

	private static List<IlInstruction> ReadInstructions(MethodInfo method)
	{
		byte[] array = method.GetMethodBody()?.GetILAsByteArray();
		if (array == null)
		{
			return new List<IlInstruction>();
		}
		Type declaringType = method.DeclaringType;
		Type[] typeArguments = (((object)declaringType != null && declaringType.IsGenericType) ? declaringType.GetGenericArguments() : Type.EmptyTypes);
		Type[] methodArguments = (method.IsGenericMethod ? method.GetGenericArguments() : Type.EmptyTypes);
		List<IlInstruction> list = new List<IlInstruction>();
		int offset = 0;
		while (offset < array.Length)
		{
			int offset2 = offset;
			byte b = array[offset++];
			OpCode opCode;
			if (b == 254)
			{
				if (offset >= array.Length)
				{
					break;
				}
				opCode = MultiByteOpCodes[array[offset++]];
			}
			else
			{
				opCode = SingleByteOpCodes[b];
			}
			object obj = null;
			try
			{
				obj = ReadOperand(method.Module, array, ref offset, opCode.OperandType, typeArguments, methodArguments);
			}
			catch
			{
				break;
			}
			list.Add(new IlInstruction(offset2, opCode, obj));
		}
		return list;
	}

	private static object? ReadOperand(Module module, byte[] il, ref int offset, OperandType operandType, Type[] typeArguments, Type[] methodArguments)
	{
		switch (operandType)
		{
		case OperandType.InlineNone:
			return null;
		case OperandType.ShortInlineI:
			return (int)(sbyte)il[offset++];
		case OperandType.InlineI:
		{
			int num2 = BitConverter.ToInt32(il, offset);
			offset += 4;
			return num2;
		}
		case OperandType.InlineI8:
		case OperandType.InlineR:
			offset += 8;
			return null;
		case OperandType.ShortInlineR:
			offset += 4;
			return null;
		case OperandType.ShortInlineBrTarget:
		case OperandType.ShortInlineVar:
			offset++;
			return null;
		case OperandType.InlineVar:
			offset += 2;
			return null;
		case OperandType.InlineBrTarget:
		case OperandType.InlineSig:
		case OperandType.InlineString:
		case OperandType.InlineTok:
		case OperandType.InlineType:
			offset += 4;
			return null;
		case OperandType.InlineField:
		{
			int metadataToken2 = BitConverter.ToInt32(il, offset);
			offset += 4;
			return module.ResolveField(metadataToken2, typeArguments, methodArguments);
		}
		case OperandType.InlineMethod:
		{
			int metadataToken = BitConverter.ToInt32(il, offset);
			offset += 4;
			return module.ResolveMethod(metadataToken, typeArguments, methodArguments);
		}
		case OperandType.InlineSwitch:
		{
			int num = BitConverter.ToInt32(il, offset);
			offset += 4 + num * 4;
			return null;
		}
		default:
			throw new InvalidOperationException($"Unsupported IL operand type: {operandType}");
		}
	}
}
