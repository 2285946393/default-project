using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntentGraph2.Patches;

public class StateLogPatches
{
	[HarmonyPatch(typeof(MonsterMoveStateMachine), "SetCurrentState")]
	public static class SetCurrentStatePatch
	{
		public static void Prefix(MonsterMoveStateMachine __instance, MonsterState state)
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Invalid comparison between Unknown and I4
			if (!state.ShouldAppearInLogs)
			{
				return;
			}
			MonsterState val = state;
			MoveState val2 = (MoveState)(object)((state is MoveState) ? state : null);
			if (val2 != null && val2.Intents.Count == 1 && (int)val2.Intents[0].IntentType == 10)
			{
				StackFrame stackFrame = new StackTrace().GetFrames().Where(delegate(StackFrame t)
				{
					Type type = t.GetMethod()?.DeclaringType;
					return (object)type != null && type.IsAssignableTo(typeof(IAsyncStateMachine)) && (object)type != null && (type.DeclaringType?.IsAssignableTo(typeof(AbstractModel))).GetValueOrDefault();
				}).FirstOrDefault();
				int num;
				if (stackFrame != null)
				{
					MethodBase? method = stackFrame.GetMethod();
					num = (((object)method != null && (method.DeclaringType?.DeclaringType?.IsAssignableTo(typeof(CardModel))).GetValueOrDefault()) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				bool flag = false;
				if (num == 0)
				{
					List<MonsterState> list = __instance.States.Values.Where(delegate(MonsterState s)
					{
						//IL_0024: Unknown result type (might be due to invalid IL or missing references)
						//IL_002b: Invalid comparison between Unknown and I4
						MoveState val3 = (MoveState)(object)((s is MoveState) ? s : null);
						return val3 != null && val3.Intents.Count == 1 && (int)val3.Intents[0].IntentType == 10;
					}).ToList();
					if (list.Count == 1)
					{
						val = list[0];
						flag = true;
					}
				}
				if (!flag)
				{
					val = (MonsterState)(object)NormalStun;
				}
			}
			List<MonsterState> orCreateValue = FullStateLog.GetOrCreateValue(__instance);
			if (orCreateValue.Count >= 2)
			{
				if (orCreateValue[orCreateValue.Count - 1] == NormalStun)
				{
					if (orCreateValue[orCreateValue.Count - 2] == val)
					{
						orCreateValue.RemoveAt(orCreateValue.Count - 1);
						return;
					}
				}
			}
			if (orCreateValue.Count >= 1)
			{
				if (orCreateValue[orCreateValue.Count - 1] == NormalStun)
				{
					orCreateValue[orCreateValue.Count - 1] = val;
					return;
				}
			}
			orCreateValue.Add(val);
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	public static class StateMachineConstructorPatch
	{
		public static void Postfix(MonsterMoveStateMachine __instance, IEnumerable<MonsterState> states, MonsterState initialState)
		{
			List<MonsterState> list = new List<MonsterState>();
			FullStateLog.Add(__instance, list);
			if (initialState.ShouldAppearInLogs)
			{
				list.Add(initialState);
			}
		}
	}

	public static readonly ConditionalWeakTable<MonsterMoveStateMachine, List<MonsterState>> FullStateLog = new ConditionalWeakTable<MonsterMoveStateMachine, List<MonsterState>>();

	private static readonly MoveState NormalStun = new MoveState("NormalStun", (Func<IReadOnlyList<Creature>, Task>)((IReadOnlyList<Creature> c) => Task.CompletedTask), (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new StunIntent() });
}
