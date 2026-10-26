using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntentGraph2.Utils.GraphGenerator;

internal class MonsterStateNodeSimplifier
{
	public static void SimplifyStateNodes(MonsterStateNode stateNode, HashSet<MonsterStateNode> allNodes, MonsterStateNodeConverter converter)
	{
		RemoveUnreachableNoRepeat(allNodes, converter);
		List<MonsterStateNode> rootNodes = new List<MonsterStateNode>(allNodes.Where((MonsterStateNode n) => n.Parent == null));
		MergeSameNodes(allNodes, rootNodes);
		ChangeToHorizontalLayout(allNodes, rootNodes);
	}

	public static void FindAndSetSimpleLoops(List<MonsterStateNode> initNodes)
	{
		Dictionary<MonsterStateNode, int> precessorCount = new Dictionary<MonsterStateNode, int>();
		foreach (MonsterStateNode initNode in initNodes)
		{
			precessorCount[initNode] = 1;
		}
		HashSet<MonsterStateNode> allNodes = initNodes.GetAllNodes();
		foreach (MonsterStateNode item in allNodes)
		{
			if (item.NextState != null)
			{
				precessorCount[item.NextState] = precessorCount.GetValueOrDefault(item.NextState) + 1;
			}
		}
		foreach (MonsterStateNode item2 in allNodes.Where((MonsterStateNode n) => n.Parent == null && n.Children == null && !n.ForceNotSimpleLoop && precessorCount.GetValueOrDefault(n) > 1).ToList())
		{
			HashSet<MonsterStateNode> hashSet = new HashSet<MonsterStateNode>();
			MonsterStateNode monsterStateNode = item2;
			while (true)
			{
				if (monsterStateNode != null && !hashSet.Contains(monsterStateNode))
				{
					if (monsterStateNode != item2 && (monsterStateNode.Children != null || precessorCount.GetValueOrDefault(monsterStateNode) > 1))
					{
						break;
					}
					hashSet.Add(monsterStateNode);
					monsterStateNode = monsterStateNode.NextState;
					continue;
				}
				if (monsterStateNode == item2 && hashSet.Count > 1)
				{
					item2.SimpleLoopStart = true;
					item2.SimpleLoopLength = hashSet.Count;
					item2.SimpleLoopPrecessorCount = precessorCount.GetValueOrDefault(item2) - 1;
				}
				break;
			}
		}
	}

	private static void MergeSameNodes(HashSet<MonsterStateNode> allNodes, List<MonsterStateNode> rootNodes)
	{
		List<(MonsterStateNode, MonsterStateNode)> list = new List<(MonsterStateNode, MonsterStateNode)>();
		for (int i = 0; i < rootNodes.Count; i++)
		{
			MonsterStateNode monsterStateNode = rootNodes[i];
			for (int j = i + 1; j < rootNodes.Count; j++)
			{
				MonsterStateNode monsterStateNode2 = rootNodes[j];
				if (AreSameNode(monsterStateNode, monsterStateNode2, list))
				{
					list.Add((monsterStateNode, monsterStateNode2));
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 2);
					defaultInterpolatedStringHandler.AppendLiteral("MergeSameNodes detected same node ");
					MonsterState? state = monsterStateNode.State;
					defaultInterpolatedStringHandler.AppendFormatted((state != null) ? state.Id : null);
					defaultInterpolatedStringHandler.AppendLiteral(" and ");
					MonsterState? state2 = monsterStateNode2.State;
					defaultInterpolatedStringHandler.AppendFormatted((state2 != null) ? state2.Id : null);
					defaultInterpolatedStringHandler.AppendLiteral(".");
					IgLogger.Info(defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
		}
		Dictionary<MonsterStateNode, MonsterStateNode> dictionary = new Dictionary<MonsterStateNode, MonsterStateNode>();
		foreach (var (monsterStateNode3, monsterStateNode4) in list)
		{
			if (!dictionary.ContainsKey(monsterStateNode3) && !dictionary.ContainsKey(monsterStateNode4))
			{
				dictionary[monsterStateNode4] = monsterStateNode3;
				monsterStateNode3.AddMoveStateIdsFrom(monsterStateNode4);
			}
		}
		foreach (MonsterStateNode allNode in allNodes)
		{
			if (allNode.NextState != null && dictionary.TryGetValue(allNode.NextState, out var value))
			{
				allNode.NextState = value;
			}
		}
	}

	private static bool AreSameNode(MonsterStateNode a, MonsterStateNode b, List<(MonsterStateNode, MonsterStateNode)> exisitingSameNodes)
	{
		if (exisitingSameNodes.Contains((a, b)) || exisitingSameNodes.Contains((b, a)))
		{
			return true;
		}
		if (a == b)
		{
			return true;
		}
		if (a.Children != null != (b.Children != null))
		{
			return false;
		}
		if (a.NextState != null != (b.NextState != null))
		{
			return false;
		}
		if (a.Label != b.Label)
		{
			return false;
		}
		exisitingSameNodes.Add((a, b));
		try
		{
			if (a.Children != null)
			{
				if (a.Children.Count != b.Children.Count)
				{
					return false;
				}
				for (int i = 0; i < a.Children.Count; i++)
				{
					if (!AreSameNode(a.Children[i], b.Children[i], exisitingSameNodes))
					{
						return false;
					}
				}
			}
			else
			{
				if (a.State != b.State)
				{
					return false;
				}
				if (a.ResolvedIntentIcons == null != (b.ResolvedIntentIcons == null))
				{
					return false;
				}
				if (a.ResolvedIntentIcons != null && b.ResolvedIntentIcons != null)
				{
					if (a.ResolvedIntentIcons.Count != b.ResolvedIntentIcons.Count)
					{
						return false;
					}
					for (int j = 0; j < a.ResolvedIntentIcons.Count; j++)
					{
						ResolvedIntent resolvedIntent = a.ResolvedIntentIcons[j];
						ResolvedIntent resolvedIntent2 = b.ResolvedIntentIcons[j];
						if (resolvedIntent.OriginalIntentIndex != resolvedIntent2.OriginalIntentIndex)
						{
							return false;
						}
						if (resolvedIntent.Value != resolvedIntent2.Value || resolvedIntent.ModelId != resolvedIntent2.ModelId)
						{
							return false;
						}
					}
				}
			}
			if (a.NextState != null && !AreSameNode(a.NextState, b.NextState, exisitingSameNodes))
			{
				return false;
			}
			return true;
		}
		finally
		{
			exisitingSameNodes.Remove((a, b));
		}
	}

	private static void RemoveUnreachableNoRepeat(HashSet<MonsterStateNode> allNodes, MonsterStateNodeConverter converter)
	{
		bool flag;
		do
		{
			flag = false;
			Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> precessorDict = GetPrecessorDict(allNodes);
			List<MonsterStateNode> list = new List<MonsterStateNode>();
			foreach (MonsterStateNode allNode in allNodes)
			{
				MonsterStateNode parent = allNode.Parent;
				if (parent != null && allNode.State is MoveState && allNode.Children == null && parent.State is RandomBranchState && parent.Children != null && !parent.Children.Any((MonsterStateNode c) => c.Label == null || !c.Label.IsTextGenerated))
				{
					if (TryRemoveNoRepeatNodePrecededBySame(allNode, precessorDict, list, converter))
					{
						flag = true;
					}
					else if (TryRemoveNoRepeatTextIfPrecededByOther(allNode, precessorDict, converter))
					{
						flag = true;
					}
				}
			}
			foreach (MonsterStateNode item in list)
			{
				allNodes.Remove(item);
			}
		}
		while (flag);
	}

	public static Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> GetPrecessorDict(HashSet<MonsterStateNode> allNodes)
	{
		Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> dictionary = new Dictionary<MonsterStateNode, HashSet<MonsterStateNode>>();
		foreach (MonsterStateNode allNode in allNodes)
		{
			if (allNode.NextState != null)
			{
				if (!dictionary.ContainsKey(allNode.NextState))
				{
					dictionary[allNode.NextState] = new HashSet<MonsterStateNode>();
				}
				dictionary[allNode.NextState].Add(allNode);
			}
		}
		foreach (MonsterStateNode allNode2 in allNodes)
		{
			MonsterStateNode monsterStateNode = allNode2;
			while (monsterStateNode.Parent != null)
			{
				monsterStateNode = monsterStateNode.Parent;
			}
			if (monsterStateNode != allNode2 && dictionary.TryGetValue(monsterStateNode, out var value))
			{
				dictionary[allNode2] = value;
			}
		}
		return dictionary;
	}

	private static bool TryRemoveNoRepeatNodePrecededBySame(MonsterStateNode node, Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> allPrecessors, List<MonsterStateNode> nodesToRemove, MonsterStateNodeConverter converter)
	{
		MonsterStateNode node2 = node;
		MonsterStateNode parent = node2.Parent;
		if (node2.Label == null || node2.Label.MaxRepeat != 1)
		{
			return false;
		}
		if (!allPrecessors.ContainsKey(node2))
		{
			return false;
		}
		HashSet<MonsterStateNode> source = allPrecessors[node2];
		if (source.Any((MonsterStateNode n) => n.Children != null || !(n.State is MoveState)))
		{
			return false;
		}
		List<string> list = source.Select((MonsterStateNode n) => n.State.Id).Distinct().ToList();
		if (list.Count == 1)
		{
			string text = list[0];
			MonsterState? state = node2.State;
			if (!(text != ((state != null) ? state.Id : null)))
			{
				nodesToRemove.Add(node2);
				parent.Children.Remove(node2);
				foreach (KeyValuePair<MonsterStateNode, HashSet<MonsterStateNode>> allPrecessor in allPrecessors)
				{
					allPrecessor.Deconstruct(out var _, out var value);
					value.Remove(node2);
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 2);
				defaultInterpolatedStringHandler.AppendLiteral("TryRemoveNoRepeatNodePrecededBySame removed node ");
				MonsterState? state2 = node2.State;
				defaultInterpolatedStringHandler.AppendFormatted((state2 != null) ? state2.Id : null);
				defaultInterpolatedStringHandler.AppendLiteral(" in ");
				MonsterState? state3 = parent.State;
				defaultInterpolatedStringHandler.AppendFormatted((state3 != null) ? state3.Id : null);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				IgLogger.Info(defaultInterpolatedStringHandler.ToStringAndClear());
				if (parent.Children.Count == 1)
				{
					MonsterStateNode monsterStateNode = parent.Children[0];
					foreach (MonsterStateNode item in allPrecessors[parent])
					{
						if (item.NextState == parent)
						{
							item.NextState = monsterStateNode;
						}
					}
					nodesToRemove.Add(parent);
					monsterStateNode.Parent = parent.Parent;
					monsterStateNode.Label = parent.Label;
					if (monsterStateNode.NextState == null)
					{
						monsterStateNode.NextState = parent.NextState;
						monsterStateNode.NextStateCount = ((monsterStateNode.NextState != null) ? 1 : 0);
						if (monsterStateNode.NextState != null)
						{
							HashSet<MonsterStateNode> hashSet = allPrecessors[monsterStateNode.NextState];
							hashSet.Add(monsterStateNode);
							hashSet.Remove(parent);
						}
					}
				}
				else
				{
					float sumWeight = parent.Children.Where((MonsterStateNode c) => c != node2).Sum((MonsterStateNode c) => c.Label?.Weight ?? 0f);
					foreach (MonsterStateNode child in parent.Children)
					{
						if (child.Label != null)
						{
							child.Label.Text = converter.MakeText(child.Label, sumWeight);
						}
					}
					List<MonsterStateNode> list2 = parent.Children.Select((MonsterStateNode c) => c.NextState).Distinct().ToList();
					if (list2.Count == 1)
					{
						foreach (MonsterStateNode child2 in parent.Children)
						{
							child2.NextState = null;
							child2.NextStateCount = 0;
						}
						parent.NextState = list2[0];
					}
					parent.NextStateCount = ((parent.NextState != null) ? 1 : 0) + parent.Children.Select((MonsterStateNode c) => c.NextStateCount).DefaultIfEmpty(0).Max();
					parent.CalculateNodeSize();
				}
				return true;
			}
		}
		return false;
	}

	private static bool TryRemoveNoRepeatTextIfPrecededByOther(MonsterStateNode node, Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> allPrecessors, MonsterStateNodeConverter converter)
	{
		MonsterStateNode node2 = node;
		_ = node2.Parent;
		if (node2.Label == null || node2.Label.MaxRepeat != 1 || node2.Label.UseOnlyOnce)
		{
			return false;
		}
		if (allPrecessors.TryGetValue(node2, out HashSet<MonsterStateNode> value) && value.Any(delegate(MonsterStateNode n)
		{
			if (n.Children == null && n.State is MoveState)
			{
				string id = n.State.Id;
				MonsterState? state2 = node2.State;
				return id == ((state2 != null) ? state2.Id : null);
			}
			return true;
		}))
		{
			return false;
		}
		if (!node2.Label.Text.Contains("≤1"))
		{
			return false;
		}
		MonsterState? state = node2.State;
		IgLogger.Info("TryRemoveNoRepeatTextIfPrecededByOther removed repeat restriction on " + ((state != null) ? state.Id : null) + ".");
		node2.Label.Text = node2.Label.Text.Replace(", ≤1", string.Empty);
		node2.Label.MaxRepeat = 0;
		return true;
	}

	private static void ChangeToHorizontalLayout(HashSet<MonsterStateNode> allNodes, List<MonsterStateNode> rootNodes)
	{
		foreach (MonsterStateNode allNode in allNodes)
		{
			List<MonsterStateNode> children = allNode.Children;
			if (children == null || children.Count <= 2)
			{
				continue;
			}
			MonsterStateNode? parent = allNode.Parent;
			if ((parent == null || !parent.HorizontalLayout) && !children.Any((MonsterStateNode c) => c.HorizontalLayout || c.NextState != null || c.Width > 1.5f) && ((allNode.Parent != null && !(allNode.Parent.Width < children.Sum((MonsterStateNode c) => c.Width))) || rootNodes.Count <= 4))
			{
				allNode.HorizontalLayout = true;
				allNode.CalculateNodeSize();
				for (MonsterStateNode parent2 = allNode.Parent; parent2 != null; parent2 = parent2.Parent)
				{
					parent2.CalculateNodeSize();
				}
			}
		}
	}
}
