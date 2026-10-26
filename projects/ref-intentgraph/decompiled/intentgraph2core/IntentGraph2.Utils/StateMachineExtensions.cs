using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntentGraph2.Utils;

public static class StateMachineExtensions
{
	public static MonsterState GetInitialState(this MonsterMoveStateMachine stateMachine)
	{
		object? obj = ((object)stateMachine).GetType().GetField("_initialState", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(stateMachine);
		MonsterState val = (MonsterState)((obj is MonsterState) ? obj : null);
		if (val == null)
		{
			throw new Exception("Failed to get initial state from state machine.");
		}
		return val;
	}

	public static MonsterState GetCurrentState(this MonsterMoveStateMachine stateMachine)
	{
		object? obj = ((object)stateMachine).GetType().GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(stateMachine);
		MonsterState val = (MonsterState)((obj is MonsterState) ? obj : null);
		if (val == null)
		{
			throw new Exception("Failed to get current state from state machine.");
		}
		return val;
	}

	public static List<string> GetStates(this ConditionalBranchState conditionalBranchState)
	{
		IList list = ((object)conditionalBranchState).GetType().GetProperty("States", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(conditionalBranchState) as IList;
		List<string> list2 = new List<string>();
		if (list != null)
		{
			foreach (object item2 in list)
			{
				if (item2.GetType().GetField("id", BindingFlags.Instance | BindingFlags.Public)?.GetValue(item2) is string item)
				{
					list2.Add(item);
				}
			}
		}
		return list2;
	}

	public static string? EvaluateStates(this ConditionalBranchState conditionalBranchState)
	{
		if (((object)conditionalBranchState).GetType().GetProperty("States", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(conditionalBranchState) is IList list)
		{
			foreach (object item in list)
			{
				float? num = item.GetType().GetMethod("Evaluate", BindingFlags.Instance | BindingFlags.Public)?.Invoke(item, null) as float?;
				if (num.HasValue && num > 0f)
				{
					return item.GetType().GetField("id", BindingFlags.Instance | BindingFlags.Public)?.GetValue(item) as string;
				}
			}
		}
		return null;
	}
}
