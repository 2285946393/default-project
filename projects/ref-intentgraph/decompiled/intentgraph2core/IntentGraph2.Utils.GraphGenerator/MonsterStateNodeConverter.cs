using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;
using IntentGraph2.Models;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntentGraph2.Utils.GraphGenerator;

internal class MonsterStateNodeConverter
{
	private const string Otherwise = "otherwise";

	private const string OtherwiseMark = "{otherwise}";

	private readonly IntentGraphLocalizer localizer;

	private readonly IntentDefinition? intentDefinition;

	private readonly Func<string, int, Vector2> getStringSize;

	private readonly Dictionary<MoveState, List<ResolvedIntent>> resolvedIntentsByMoveState = new Dictionary<MoveState, List<ResolvedIntent>>();

	public bool EvaluateInitialState { get; set; } = true;


	public MonsterStateNodeConverter(IntentGraphLocalizer localizer, IntentDefinition? intentDefinition, Func<string, int, Vector2> getStringSize)
	{
		this.localizer = localizer;
		this.intentDefinition = intentDefinition;
		this.getStringSize = getStringSize;
	}

	public List<MonsterStateNode> ToMonsterStateNodes(MonsterModel monster, MonsterMoveStateMachine stateMachine, ref string? warning)
	{
		if (intentDefinition?.StateMachine != null)
		{
			return FromStateMachineNodes(stateMachine, intentDefinition.StateMachine);
		}
		return FromMonsterMoveStateMachine(((object)monster).GetType().FullName ?? "_unknownMonster", stateMachine, intentDefinition, ref warning);
	}

	private List<MonsterStateNode> FromStateMachineNodes(MonsterMoveStateMachine stateMachine, StateMachineNode[] overwriteStateMachine)
	{
		Dictionary<string, MonsterStateNode> existingNodes = new Dictionary<string, MonsterStateNode>();
		List<(int, MonsterStateNode)> list = new List<(int, MonsterStateNode)>();
		foreach (StateMachineNode stateMachineNode in overwriteStateMachine)
		{
			if (stateMachineNode.IsInitialState)
			{
				MonsterStateNode monsterStateNode = StateMachineNodeToMonsterStateNode(stateMachine, overwriteStateMachine, stateMachineNode, existingNodes, null);
				if (monsterStateNode != null)
				{
					list.Add((stateMachineNode.InitialStatePriority, monsterStateNode));
				}
			}
		}
		List<MonsterStateNode> list2 = (from t in list
			orderby t.Item1
			select t.Item2).ToList();
		MonsterStateNodeSimplifier.FindAndSetSimpleLoops(list2);
		MonsterStateNode? monsterStateNode2 = list2.FirstOrDefault();
		if (monsterStateNode2 != null)
		{
			monsterStateNode2.SetIsInitialState(value: true);
			return list2;
		}
		return list2;
	}

	private List<MonsterStateNode> FromMonsterMoveStateMachine(string monsterName, MonsterMoveStateMachine stateMachine, IntentDefinition? intentDefinition, ref string? warning)
	{
		Dictionary<MonsterState, MonsterStateNode> dictionary = new Dictionary<MonsterState, MonsterStateNode>();
		List<MonsterStateNode> list = new List<MonsterStateNode>();
		MonsterStateNode monsterStateNode = null;
		MonsterState initialState = stateMachine.GetInitialState();
		if (EvaluateInitialState)
		{
			ConditionalBranchState val = (ConditionalBranchState)(object)((initialState is ConditionalBranchState) ? initialState : null);
			if (val != null)
			{
				string stateName = val.EvaluateStates();
				MonsterState val2 = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState s) => s.Id == stateName));
				if (val2 != null)
				{
					monsterStateNode = MonsterStateToMonsterStateNode(monsterName, stateMachine, val2, dictionary, null, ref warning);
					monsterStateNode.SetIsInitialState(value: true);
					if (monsterStateNode.NextState?.State == initialState)
					{
						monsterStateNode = null;
					}
				}
			}
		}
		if (monsterStateNode == null)
		{
			monsterStateNode = MonsterStateToMonsterStateNode(monsterName, stateMachine, initialState, dictionary, null, ref warning);
			monsterStateNode.SetIsInitialState(value: true);
		}
		HashSet<MonsterStateNode> allNodes = monsterStateNode.GetAllNodes();
		MonsterStateNodeSimplifier.SimplifyStateNodes(monsterStateNode, allNodes, this);
		list.Add(monsterStateNode);
		SecondaryInitialState[] array = intentDefinition?.SecondaryInitialStates;
		if (array != null)
		{
			SecondaryInitialState[] array2 = array;
			foreach (SecondaryInitialState secondaryState in array2)
			{
				MonsterState val3 = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState s) => s.Id == secondaryState.Id));
				if (val3 == null || dictionary.ContainsKey(val3))
				{
					continue;
				}
				MonsterStateNode monsterStateNode2 = MonsterStateToMonsterStateNode(monsterName, stateMachine, val3, dictionary, null, ref warning);
				HashSet<MonsterStateNode> allNodes2 = monsterStateNode2.GetAllNodes();
				MonsterStateNodeSimplifier.SimplifyStateNodes(monsterStateNode2, allNodes2, this);
				foreach (MonsterStateNode item in allNodes2)
				{
					allNodes.Add(item);
				}
				monsterStateNode2.Offset = secondaryState.Offset;
				list.Add(monsterStateNode2);
			}
		}
		foreach (string stateId in stateMachine.States.Keys)
		{
			if (stateId != "INIT_MOVE" && !allNodes.Any(delegate(MonsterStateNode n)
			{
				MonsterState? state = n.State;
				return ((state != null) ? state.Id : null) == stateId;
			}))
			{
				IgLogger.Warn($"State '{stateId}' is not included in the graph for monster '{monsterName}'.");
				break;
			}
		}
		if (warning == null && allNodes.Any((MonsterStateNode n) => n.UnrecognizedStateType))
		{
			warning = localizer.GetOrElse("ui.Incomplete", "Incomplete");
		}
		MonsterStateNodeSimplifier.FindAndSetSimpleLoops(list);
		return list;
	}

	public string MakeText(MonsterStateNodeLabel label, float sumWeight)
	{
		float weight = label.Weight;
		int cooldown = label.Cooldown;
		bool useOnlyOnce = label.UseOnlyOnce;
		int maxRepeat = label.MaxRepeat;
		return (int)(weight / sumWeight * 100f) + "%" + ((cooldown > 0) ? (", ⏱" + cooldown) : (useOnlyOnce ? (", " + localizer.GetOrElse("ui.UseOnlyOnce", "one use")) : ((maxRepeat > 0) ? (", ≤" + maxRepeat) : "")));
	}

	private MonsterStateNode? StateMachineNodeToMonsterStateNode(MonsterMoveStateMachine stateMachine, StateMachineNode[] overwriteStateMachine, StateMachineNode? node, Dictionary<string, MonsterStateNode> existingNodes, MonsterStateNode? parent)
	{
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		StateMachineNode node2 = node;
		if (node2 == null)
		{
			return null;
		}
		string name = node2.Name;
		if (parent == null && existingNodes.TryGetValue(name, out MonsterStateNode value))
		{
			return value;
		}
		if (node2.Children == null || node2.Children.Length == 0)
		{
			MonsterState? obj = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState s) => s.Id == (node2.MoveName ?? node2.Name)));
			MoveState val = (MoveState)(object)((obj is MoveState) ? obj : null);
			MonsterStateNode monsterStateNode;
			if (val != null)
			{
				List<ResolvedIntent> list = MoveDetailResolver.ResolveIntentIcons(val, (intentDefinition?.MoveReplacements?.GetMoveReplacementOrNull(((MonsterState)val).Id, string.IsNullOrEmpty(node2.Name) ? null : ((parent == null) ? ("/" + node2.Name) : (parent.FullId + "/" + node2.Name))))?.IntentOverrides);
				monsterStateNode = new MonsterStateNode
				{
					Id = node2.Name,
					State = (MonsterState?)(object)val,
					ResolvedIntentIcons = list,
					Width = IntentGraphGenerator.GetMoveWidth(list.Count),
					Height = 1f,
					NextStateCount = 1,
					Parent = parent,
					ForceNotSimpleLoop = node2.NotSimpleLoopStart,
					Offset = node2.Offset
				};
				monsterStateNode.MoveStateIds.Add(((MonsterState)val).Id);
				if (node2.AlternativeMoveNames != null)
				{
					monsterStateNode.MoveStateIds.AddRange(node2.AlternativeMoveNames);
				}
			}
			else
			{
				if (node2.PlaceholderIntentCount <= 0)
				{
					return null;
				}
				monsterStateNode = new MonsterStateNode
				{
					Id = node2.Name,
					State = null,
					Width = IntentGraphGenerator.GetMoveWidth(node2.PlaceholderIntentCount),
					Height = 1f,
					NextStateCount = 1,
					Parent = parent,
					ForceNotSimpleLoop = node2.NotSimpleLoopStart,
					Offset = node2.Offset
				};
				if (node2.AlternativeMoveNames != null)
				{
					monsterStateNode.MoveStateIds.AddRange(node2.AlternativeMoveNames);
				}
			}
			if (parent == null)
			{
				existingNodes[name] = monsterStateNode;
			}
			if (node2.FollowUpState != null)
			{
				monsterStateNode.NextState = StateMachineNodeToMonsterStateNode(stateMachine, overwriteStateMachine, overwriteStateMachine.FirstOrDefault((StateMachineNode n) => n.Name == node2.FollowUpState), existingNodes, null);
			}
			return monsterStateNode;
		}
		MonsterStateNode monsterStateNode2 = new MonsterStateNode
		{
			Id = node2.Name,
			State = null,
			Parent = parent,
			ForceNotSimpleLoop = node2.NotSimpleLoopStart,
			Offset = node2.Offset
		};
		if (parent == null)
		{
			existingNodes[name] = monsterStateNode2;
		}
		List<MonsterStateNode> list2 = new List<MonsterStateNode>();
		for (int i = 0; i < node2.Children.Length; i++)
		{
			StateMachineNode node3 = node2.Children[i].Node;
			string label = node2.Children[i].Label;
			label = localizer.GetOrElse(label, label);
			label = localizer.FormatWithVariables(label);
			MonsterStateNode monsterStateNode3 = StateMachineNodeToMonsterStateNode(stateMachine, overwriteStateMachine, node3, existingNodes, monsterStateNode2);
			if (monsterStateNode3 != null)
			{
				monsterStateNode3.Label = new MonsterStateNodeLabel
				{
					Text = label
				};
				monsterStateNode3.Width = Math.Max(monsterStateNode3.Width, getStringSize(label, 18).X / 80f);
				list2.Add(monsterStateNode3);
				monsterStateNode2.MoveStateIds.AddRange(monsterStateNode3.MoveStateIds);
			}
		}
		if (node2.AlternativeMoveNames != null)
		{
			monsterStateNode2.MoveStateIds.AddRange(node2.AlternativeMoveNames);
		}
		monsterStateNode2.Children = list2;
		monsterStateNode2.HorizontalLayout = node2.HorizontalLayout;
		monsterStateNode2.CalculateNodeSize();
		if (node2.FollowUpState != null)
		{
			monsterStateNode2.NextState = StateMachineNodeToMonsterStateNode(stateMachine, overwriteStateMachine, overwriteStateMachine.FirstOrDefault((StateMachineNode n) => n.Name == node2.FollowUpState), existingNodes, null);
		}
		monsterStateNode2.NextStateCount = ((monsterStateNode2.NextState != null) ? 1 : 0) + list2.Select((MonsterStateNode c) => c.NextStateCount).DefaultIfEmpty(0).Max();
		return monsterStateNode2;
	}

	[return: NotNullIfNotNull("state")]
	private MonsterStateNode? MonsterStateToMonsterStateNode(string monsterName, MonsterMoveStateMachine stateMachine, MonsterState? state, Dictionary<MonsterState, MonsterStateNode> existingNodes, MonsterStateNode? parent, ref string? warning)
	{
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Expected I4, but got Unknown
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Invalid comparison between Unknown and I4
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_064a: Unknown result type (might be due to invalid IL or missing references)
		if (state == null)
		{
			return null;
		}
		if (parent == null && existingNodes.TryGetValue(state, out MonsterStateNode value))
		{
			return value;
		}
		MoveState val = (MoveState)(object)((state is MoveState) ? state : null);
		if (val != null)
		{
			List<ResolvedIntent> list = MoveDetailResolver.ResolveIntentIcons(val, (intentDefinition?.MoveReplacements?.GetMoveReplacementOrNull(state.Id, (parent == null) ? ("/" + state.Id) : (parent.FullId + "/" + state.Id)))?.IntentOverrides);
			MonsterStateNode monsterStateNode = new MonsterStateNode
			{
				Id = state.Id,
				State = state,
				ResolvedIntentIcons = list,
				Width = IntentGraphGenerator.GetMoveWidth(list.Count),
				Height = 1f,
				NextStateCount = 1,
				Parent = parent
			};
			monsterStateNode.MoveStateIds.Add(((MonsterState)val).Id);
			if (parent == null)
			{
				existingNodes[state] = monsterStateNode;
			}
			monsterStateNode.NextState = MonsterStateToMonsterStateNode(monsterName, stateMachine, val.FollowUpState, existingNodes, null, ref warning);
			return monsterStateNode;
		}
		IntentGraphLocalizer.OnFoundVariableDelegate onFoundVariable = (string var, string? type) => (!(var == "otherwise")) ? null : "{otherwise}";
		bool unrecognizedStateType = false;
		List<(string, MonsterStateNodeLabel)> list2 = new List<(string, MonsterStateNodeLabel)>();
		RandomBranchState val2 = (RandomBranchState)(object)((state is RandomBranchState) ? state : null);
		if (val2 != null)
		{
			float sumWeight = val2.States.Sum((StateWeight s) => ((StateWeight)(ref s)).GetWeight());
			foreach (StateWeight state2 in val2.States)
			{
				StateWeight current = state2;
				MonsterStateNodeLabel monsterStateNodeLabel = new MonsterStateNodeLabel();
				monsterStateNodeLabel.Type = MonsterStateNodeLabel.LabelType.Random;
				monsterStateNodeLabel.Text = string.Empty;
				monsterStateNodeLabel.IsTextGenerated = true;
				monsterStateNodeLabel.Weight = ((StateWeight)(ref current)).GetWeight();
				MonsterStateNodeLabel monsterStateNodeLabel2 = monsterStateNodeLabel;
				MoveRepeatType repeatType = current.repeatType;
				int maxRepeat;
				switch ((int)repeatType)
				{
				case 0:
					maxRepeat = 0;
					break;
				case 1:
					maxRepeat = current.maxTimes;
					break;
				case 2:
				case 3:
					maxRepeat = 1;
					break;
				default:
					maxRepeat = 0;
					break;
				}
				monsterStateNodeLabel2.MaxRepeat = maxRepeat;
				monsterStateNodeLabel.Cooldown = current.cooldown;
				monsterStateNodeLabel.UseOnlyOnce = (int)current.repeatType == 3;
				MonsterStateNodeLabel monsterStateNodeLabel3 = monsterStateNodeLabel;
				if (localizer.TryGet($"branch.{monsterName}.{state.Id}.{current.stateId}", out string value2))
				{
					monsterStateNodeLabel3.Text = localizer.FormatWithVariables(value2, onFoundVariable);
					monsterStateNodeLabel3.IsTextGenerated = false;
				}
				else
				{
					monsterStateNodeLabel3.Text = MakeText(monsterStateNodeLabel3, sumWeight);
				}
				list2.Add((current.stateId, monsterStateNodeLabel3));
			}
		}
		else
		{
			ConditionalBranchState val3 = (ConditionalBranchState)(object)((state is ConditionalBranchState) ? state : null);
			if (val3 != null)
			{
				if (EvaluateInitialState && state.Id == "INIT_MOVE")
				{
					string evaluatedSstateName = val3.EvaluateStates();
					MonsterState val4 = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState s) => s.Id == evaluatedSstateName));
					if (val4 != null)
					{
						return MonsterStateToMonsterStateNode(monsterName, stateMachine, val4, existingNodes, parent, ref warning);
					}
				}
				foreach (string s2 in val3.GetStates())
				{
					if (list2.Any<(string, MonsterStateNodeLabel)>(((string state, MonsterStateNodeLabel label) c) => c.state == s2))
					{
						continue;
					}
					if (localizer.TryGet($"branch.{monsterName}.{state.Id}.{s2}", out string value3))
					{
						list2.Add((s2, new MonsterStateNodeLabel
						{
							Type = MonsterStateNodeLabel.LabelType.Condition,
							Text = localizer.FormatWithVariables(value3, onFoundVariable)
						}));
						continue;
					}
					if (warning == null)
					{
						warning = localizer.GetOrElse("ui.UnknownConditions", "Unknown conditions");
					}
					list2.Add((s2, new MonsterStateNodeLabel
					{
						Type = MonsterStateNodeLabel.LabelType.Condition,
						Text = localizer.GetOrElse("ui.UnknownCondition", "condition?")
					}));
				}
			}
			else
			{
				unrecognizedStateType = true;
			}
		}
		int num = list2.Count;
		for (int i = 0; i < num; i++)
		{
			(string, MonsterStateNodeLabel) tuple = list2[i];
			if (tuple.Item2.Text == "{otherwise}")
			{
				list2.RemoveAt(i);
				list2.Add((tuple.Item1, tuple.Item2));
				tuple.Item2.Text = localizer.GetOrElse("ui.Otherwise", "Otherwise");
				i--;
				num--;
			}
		}
		MonsterStateNode monsterStateNode2 = new MonsterStateNode
		{
			Id = state.Id,
			State = state,
			Parent = parent,
			UnrecognizedStateType = unrecognizedStateType
		};
		if (parent == null)
		{
			existingNodes[state] = monsterStateNode2;
		}
		List<MonsterStateNode> list3 = new List<MonsterStateNode>();
		for (int j = 0; j < list2.Count; j++)
		{
			(string, MonsterStateNodeLabel) tuple2 = list2[j];
			string childStateId = tuple2.Item1;
			MonsterStateNodeLabel item = tuple2.Item2;
			MonsterState val5 = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState s) => s.Id == childStateId));
			if (val5 == null)
			{
				continue;
			}
			if (IsStateInParentChain(monsterStateNode2, val5))
			{
				if (warning == null)
				{
					warning = localizer.GetOrElse("ui.Incomplete", "Incomplete");
				}
				continue;
			}
			MonsterStateNode monsterStateNode3 = MonsterStateToMonsterStateNode(monsterName, stateMachine, val5, existingNodes, monsterStateNode2, ref warning);
			if (monsterStateNode3 != null)
			{
				monsterStateNode3.Label = item;
				monsterStateNode3.Width = Math.Max(monsterStateNode3.Width, getStringSize(item.Text, 18).X / 80f);
				list3.Add(monsterStateNode3);
				monsterStateNode2.MoveStateIds.AddRange(monsterStateNode3.MoveStateIds);
			}
		}
		List<MonsterStateNode> list4 = list3.Select((MonsterStateNode c) => c.NextState).Distinct().ToList();
		if (list4.Count == 1)
		{
			foreach (MonsterStateNode item2 in list3)
			{
				item2.NextState = null;
				item2.NextStateCount = 0;
			}
		}
		monsterStateNode2.Children = list3;
		monsterStateNode2.CalculateNodeSize();
		monsterStateNode2.NextState = ((list4.Count == 1) ? list4[0] : null);
		monsterStateNode2.NextStateCount = ((monsterStateNode2.NextState != null) ? 1 : 0) + list3.Select((MonsterStateNode c) => c.NextStateCount).DefaultIfEmpty(0).Max();
		return monsterStateNode2;
	}

	private bool IsStateInParentChain(MonsterStateNode parent, MonsterState state)
	{
		for (MonsterStateNode monsterStateNode = parent; monsterStateNode != null; monsterStateNode = monsterStateNode.Parent)
		{
			if (monsterStateNode.State == state)
			{
				return true;
			}
		}
		return false;
	}
}
