using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using IntentGraph2.Patches;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Runs;

namespace IntentGraph2.Utils.Variable;

public class VariableContext : IVariableContext
{
	public MonsterModel Monster { get; }

	public bool InBestiary { get; set; }

	public VariableContext(MonsterModel monster)
	{
		Monster = monster;
	}

	public int GetIntVariable(string variableName)
	{
		try
		{
			return Convert.ToInt32(GetObjectVariable(variableName) ?? ((object)0));
		}
		catch (Exception value)
		{
			IgLogger.Warn($"Error getting int variable '{variableName}': {value}. Return 0.");
			return 0;
		}
	}

	public bool GetBoolVariable(string variableName)
	{
		try
		{
			return Convert.ToBoolean(GetObjectVariable(variableName) ?? ((object)false));
		}
		catch (Exception value)
		{
			IgLogger.Warn($"Error getting bool variable '{variableName}': {value}. Return false.");
			return false;
		}
	}

	public string GetStringVariable(string variableName)
	{
		try
		{
			return Convert.ToString(GetObjectVariable(variableName) ?? string.Empty) ?? string.Empty;
		}
		catch (Exception value)
		{
			IgLogger.Warn($"Error getting string variable '{variableName}': {value}. Return empty string.");
			return string.Empty;
		}
	}

	public object? GetObjectVariable(string variableName)
	{
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ff: Expected I4, but got Unknown
		try
		{
			if (variableName.StartsWith("m."))
			{
				if (variableName.StartsWith("m.hasPower_"))
				{
					string powerId2 = variableName.Substring(11);
					return Monster.Creature.Powers.Any((PowerModel p) => ((AbstractModel)p).Id.Entry == powerId2);
				}
				if (variableName.StartsWith("m.powerAmount_"))
				{
					string powerId = variableName.Substring(14);
					PowerModel? obj = ((IEnumerable<PowerModel>)Monster.Creature.Powers).FirstOrDefault((Func<PowerModel, bool>)((PowerModel p) => ((AbstractModel)p).Id.Entry == powerId));
					return (obj != null) ? obj.Amount : 0;
				}
				string text = variableName.Substring(2);
				Traverse val = Traverse.Create((object)Monster);
				return val.Property(text, (object[])null).GetValue() ?? val.Field(text).GetValue();
			}
			if (variableName.StartsWith("mm.") && Monster.MoveStateMachine != null)
			{
				MonsterMoveStateMachine moveStateMachine = Monster.MoveStateMachine;
				string text2 = variableName.Substring(3);
				if (text2 == "count")
				{
					return moveStateMachine.States.Count;
				}
				if (text2.StartsWith("hasMove_"))
				{
					string moveName2 = text2.Substring(8);
					return moveStateMachine.States.Any((KeyValuePair<string, MonsterState> s) => s.Key == moveName2);
				}
				if (text2.StartsWith("startsWith_"))
				{
					string text3 = text2.Substring(11);
					return moveStateMachine.GetInitialState().Id == text3;
				}
				if (text2.StartsWith("nextMoveOf_") && text2.Contains("_is_"))
				{
					int num = text2.IndexOf("_is_");
					if (num == -1)
					{
						return false;
					}
					string moveName = text2.Substring(11, num - 11);
					string nextMoveName = text2.Substring(num + 4);
					MonsterState value = moveStateMachine.States.FirstOrDefault((KeyValuePair<string, MonsterState> s) => s.Key == moveName).Value;
					MonsterState value2 = moveStateMachine.States.FirstOrDefault((KeyValuePair<string, MonsterState> s) => s.Key == nextMoveName).Value;
					MoveState val2 = (MoveState)(object)((value is MoveState) ? value : null);
					if (val2 != null && value2 != null)
					{
						return val2.FollowUpStateId == nextMoveName || val2.FollowUpState == value2;
					}
					return false;
				}
				return null;
			}
			if (variableName.StartsWith("slotName_is_"))
			{
				string text4 = variableName.Substring(12);
				return Monster.Creature.SlotName == text4;
			}
			object result;
			switch (variableName)
			{
			case "act":
				result = Monster.CombatState.RunState.CurrentActIndex;
				break;
			case "slotIndex":
			{
				EncounterModel encounter = Monster.CombatState.Encounter;
				result = ((encounter != null) ? new int?(ListExtensions.IndexOf<string>(encounter.Slots, Monster.Creature.SlotName)) : null);
				break;
			}
			case "ascension":
				result = ((int?)HasAscensionLevelPatches.OverwriteAsensionLevel) ?? Traverse.Create((object)RunManager.Instance.AscensionManager).Field("_level").GetValue<int>();
				break;
			case "showMoveNames":
				result = IntentGraphMod.Config.ShowMonsterMoveNames;
				break;
			case "inBestiary":
				result = InBestiary;
				break;
			case "showMoveDetail":
				result = IntentGraphMod.Config.ShowMoveDetail;
				break;
			default:
				result = null;
				break;
			}
			return result;
		}
		catch (Exception value3)
		{
			IgLogger.Warn($"Error getting object variable '{variableName}': {value3}. Return null.");
			return null;
		}
	}

	public Type? GetVariableType(string variableName)
	{
		try
		{
			if (variableName.StartsWith("m."))
			{
				if (variableName.StartsWith("m.hasPower_"))
				{
					return typeof(bool);
				}
				if (variableName.StartsWith("m.powerAmount_"))
				{
					return typeof(int);
				}
				string text = variableName.Substring(2);
				Traverse val = Traverse.Create((object)Monster);
				return val.Property(text, (object[])null).GetValueType() ?? val.Field(text).GetValueType();
			}
			if (variableName.StartsWith("mm.") && Monster.MoveStateMachine != null)
			{
				_ = Monster.MoveStateMachine;
				string text2 = variableName.Substring(3);
				if (text2 == "count")
				{
					return typeof(int);
				}
				if (text2.StartsWith("hasMove_"))
				{
					return typeof(bool);
				}
				if (text2.StartsWith("startsWith_"))
				{
					return typeof(bool);
				}
				if (text2.StartsWith("nextMoveOf_") && text2.Contains("_is_"))
				{
					return typeof(bool);
				}
				return null;
			}
			if (variableName.StartsWith("slotName_is_"))
			{
				return typeof(bool);
			}
			return variableName switch
			{
				"act" => typeof(int), 
				"slotIndex" => typeof(int), 
				"ascension" => typeof(int), 
				"showMoveNames" => typeof(bool), 
				"inBestiary" => typeof(bool), 
				"showMoveDetail" => typeof(bool), 
				_ => null, 
			};
		}
		catch (Exception value)
		{
			IgLogger.Warn($"Error getting object variable '{variableName}': {value}. Return null.");
			return null;
		}
	}
}
